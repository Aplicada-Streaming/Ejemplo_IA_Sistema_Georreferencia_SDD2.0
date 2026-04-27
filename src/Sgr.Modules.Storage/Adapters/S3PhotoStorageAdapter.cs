using System.Security.Cryptography;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sgr.Modules.Storage.Adapters;

public sealed class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    public string BucketName { get; set; } = "sgr-photos";
    public string Region { get; set; } = "us-east-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    /// <summary>Para MinIO u otros proveedores compatibles. Null = AWS.</summary>
    public string? ServiceUrl { get; set; }
    /// <summary>true cuando se usa MinIO o similar (forzar path-style).</summary>
    public bool ForcePathStyle { get; set; }
}

public sealed class S3PhotoStorageAdapter : IPhotoStorageAdapter, IDisposable
{
    private readonly S3StorageOptions _options;
    private readonly ILogger<S3PhotoStorageAdapter> _logger;
    private readonly Lazy<IAmazonS3> _client;

    public S3PhotoStorageAdapter(IOptions<S3StorageOptions> options, ILogger<S3PhotoStorageAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<IAmazonS3>(CreateClient);
    }

    public string AdapterName => Sgr.Domain.Photos.StorageAdapterNames.S3;

    public async Task<StoredPhotoRef> StoreAsync(Stream content, StorageHints hints, CancellationToken ct = default)
    {
        var folder = string.IsNullOrWhiteSpace(hints.SuggestedFolder) ? "default" : hints.SuggestedFolder;
        var key = $"{folder.Trim('/')}/{Guid.NewGuid():N}_{hints.FileName}";

        // Stream → MemoryStream para conocer Length + computar hash en una pasada.
        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;
        var hash = Convert.ToHexString(SHA256.HashData(ms.ToArray())).ToLowerInvariant();
        ms.Position = 0;

        await _client.Value.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = ms,
            ContentType = hints.ContentType,
            DisablePayloadSigning = _options.ForcePathStyle,
        }, ct);

        return new StoredPhotoRef(AdapterName, key, ms.Length, hash);
    }

    public async Task<Stream> ReadAsync(string adapterRef, CancellationToken ct = default)
    {
        var resp = await _client.Value.GetObjectAsync(_options.BucketName, adapterRef, ct);
        return resp.ResponseStream;
    }

    public async Task DeleteAsync(string adapterRef, CancellationToken ct = default)
    {
        await _client.Value.DeleteObjectAsync(_options.BucketName, adapterRef, ct);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            // Probe: PUT + GET + DELETE de un objeto pequeño.
            var key = $".probe-{Guid.NewGuid():N}";
            await using var ms = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
            await _client.Value.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _options.BucketName, Key = key, InputStream = ms,
            }, ct);
            await _client.Value.DeleteObjectAsync(_options.BucketName, key, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "S3 storage TestConnection failed.");
            return false;
        }
    }

    private IAmazonS3 CreateClient()
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region),
            ForcePathStyle = _options.ForcePathStyle,
        };
        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
            config.ServiceURL = _options.ServiceUrl;

        if (!string.IsNullOrWhiteSpace(_options.AccessKey) && !string.IsNullOrWhiteSpace(_options.SecretKey))
            return new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);

        // Usa el chain default (env vars / IAM roles / etc.)
        return new AmazonS3Client(config);
    }

    public void Dispose()
    {
        if (_client.IsValueCreated) _client.Value.Dispose();
    }
}
