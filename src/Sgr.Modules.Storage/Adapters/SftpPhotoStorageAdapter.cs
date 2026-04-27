using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace Sgr.Modules.Storage.Adapters;

public sealed class SftpStorageOptions
{
    public const string SectionName = "Storage:Sftp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? PrivateKeyPath { get; set; }
    public string? PrivateKeyPassphrase { get; set; }
    public string RemoteRoot { get; set; } = "/sgr-photos";
}

public sealed class SftpPhotoStorageAdapter : IPhotoStorageAdapter
{
    private readonly SftpStorageOptions _options;
    private readonly ILogger<SftpPhotoStorageAdapter> _logger;

    public SftpPhotoStorageAdapter(IOptions<SftpStorageOptions> options, ILogger<SftpPhotoStorageAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string AdapterName => Sgr.Domain.Photos.StorageAdapterNames.Sftp;

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
        await client.ConnectAsync(ct);
        EnsureRemoteDirectory(client, Path.GetDirectoryName(remotePath)!.Replace('\\', '/'));
        await Task.Run(() => client.UploadFile(ms, remotePath, canOverride: true), ct);

        return new StoredPhotoRef(AdapterName, remotePath, ms.Length, hash);
    }

    public async Task<Stream> ReadAsync(string adapterRef, CancellationToken ct = default)
    {
        var client = CreateClient();
        await client.ConnectAsync(ct);

        // Descarga a memoria para evitar mantener la conexión SFTP abierta indefinidamente.
        // Para fotos grandes, considerar streaming directo (más complejo con SSH.NET).
        var ms = new MemoryStream();
        await Task.Run(() => client.DownloadFile(adapterRef, ms), ct);
        client.Dispose();
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string adapterRef, CancellationToken ct = default)
    {
        using var client = CreateClient();
        await client.ConnectAsync(ct);
        await Task.Run(() => client.DeleteFile(adapterRef), ct);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient();
            await client.ConnectAsync(ct);
            return client.IsConnected;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SFTP storage TestConnection failed.");
            return false;
        }
    }

    private SftpClient CreateClient()
    {
        if (!string.IsNullOrWhiteSpace(_options.PrivateKeyPath))
        {
            var keyFile = string.IsNullOrEmpty(_options.PrivateKeyPassphrase)
                ? new PrivateKeyFile(_options.PrivateKeyPath)
                : new PrivateKeyFile(_options.PrivateKeyPath, _options.PrivateKeyPassphrase);
            return new SftpClient(_options.Host, _options.Port, _options.Username, keyFile);
        }

        return new SftpClient(_options.Host, _options.Port, _options.Username, _options.Password ?? string.Empty);
    }

    private static void EnsureRemoteDirectory(SftpClient client, string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/" || client.Exists(path)) return;

        var parts = path.Trim('/').Split('/');
        var current = string.Empty;
        foreach (var part in parts)
        {
            current = string.IsNullOrEmpty(current) ? "/" + part : current + "/" + part;
            if (!client.Exists(current))
                client.CreateDirectory(current);
        }
    }
}
