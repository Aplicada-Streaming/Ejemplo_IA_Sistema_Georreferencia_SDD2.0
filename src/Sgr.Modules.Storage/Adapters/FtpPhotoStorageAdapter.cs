using System.Security.Cryptography;
using FluentFTP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sgr.Modules.Storage.Adapters;

public sealed class FtpStorageOptions
{
    public const string SectionName = "Storage:Ftp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 21;
    public string Username { get; set; } = "anonymous";
    public string Password { get; set; } = string.Empty;
    public string RemoteRoot { get; set; } = "/sgr-photos";
    public bool UseTls { get; set; } = false;
}

public sealed class FtpPhotoStorageAdapter : IPhotoStorageAdapter
{
    private readonly FtpStorageOptions _options;
    private readonly ILogger<FtpPhotoStorageAdapter> _logger;

    public FtpPhotoStorageAdapter(IOptions<FtpStorageOptions> options, ILogger<FtpPhotoStorageAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string AdapterName => Sgr.Domain.Photos.StorageAdapterNames.Ftp;

    public async Task<StoredPhotoRef> StoreAsync(Stream content, StorageHints hints, CancellationToken ct = default)
    {
        var folder = string.IsNullOrWhiteSpace(hints.SuggestedFolder) ? "default" : hints.SuggestedFolder;
        var fileName = $"{Guid.NewGuid():N}_{hints.FileName}";
        var remotePath = $"{_options.RemoteRoot.TrimEnd('/')}/{folder.Trim('/')}/{fileName}";

        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;
        var hash = Convert.ToHexString(SHA256.HashData(ms.ToArray())).ToLowerInvariant();
        ms.Position = 0;

        using var client = CreateClient();
        await client.Connect(ct);
        var status = await client.UploadStream(ms, remotePath, FtpRemoteExists.Overwrite,
            createRemoteDir: true, token: ct);
        if (status != FtpStatus.Success)
            throw new InvalidOperationException($"FTP upload failed: {status}");

        return new StoredPhotoRef(AdapterName, remotePath, ms.Length, hash);
    }

    public async Task<Stream> ReadAsync(string adapterRef, CancellationToken ct = default)
    {
        // Caller dispone el cliente vía wrapper.
        var client = CreateClient();
        await client.Connect(ct);
        var stream = await client.OpenRead(adapterRef, FtpDataType.Binary, token: ct);
        return new FtpReadStream(client, stream);
    }

    public async Task DeleteAsync(string adapterRef, CancellationToken ct = default)
    {
        using var client = CreateClient();
        await client.Connect(ct);
        await client.DeleteFile(adapterRef, ct);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient();
            await client.Connect(ct);
            return client.IsConnected;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FTP storage TestConnection failed.");
            return false;
        }
    }

    private AsyncFtpClient CreateClient()
    {
        var client = new AsyncFtpClient(_options.Host, _options.Username, _options.Password, _options.Port);
        if (_options.UseTls)
            client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
        return client;
    }

    /// <summary>Wrap del Stream para mantener vivo el cliente FTP hasta que se cierre el stream.</summary>
    private sealed class FtpReadStream : Stream
    {
        private readonly AsyncFtpClient _client;
        private readonly Stream _inner;

        public FtpReadStream(AsyncFtpClient client, Stream inner)
        {
            _client = client;
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _client.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
