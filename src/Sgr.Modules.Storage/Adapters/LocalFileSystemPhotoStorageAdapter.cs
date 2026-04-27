using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sgr.Modules.Storage.Adapters;

public sealed class LocalStorageOptions
{
    public const string SectionName = "Storage:Local";

    /// <summary>Carpeta raíz donde se guardan las fotos. Default: ./storage-local</summary>
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "storage-local");
}

public sealed class LocalFileSystemPhotoStorageAdapter : IPhotoStorageAdapter
{
    private readonly LocalStorageOptions _options;
    private readonly ILogger<LocalFileSystemPhotoStorageAdapter> _logger;

    public LocalFileSystemPhotoStorageAdapter(
        IOptions<LocalStorageOptions> options,
        ILogger<LocalFileSystemPhotoStorageAdapter> logger)
    {
        _options = options.Value;
        _logger = logger;
        Directory.CreateDirectory(_options.RootPath);
    }

    public string AdapterName => Sgr.Domain.Photos.StorageAdapterNames.Local;

    public async Task<StoredPhotoRef> StoreAsync(Stream content, StorageHints hints, CancellationToken ct = default)
    {
        // Layout: <root>/<folder>/<guid>_<filename>
        var folder = string.IsNullOrWhiteSpace(hints.SuggestedFolder) ? "default" : hints.SuggestedFolder;
        var safeFolder = SanitizeFolder(folder);
        var fullFolder = Path.Combine(_options.RootPath, safeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(fullFolder);

        var fileName = $"{Guid.NewGuid():N}_{SanitizeFileName(hints.FileName)}";
        var fullPath = Path.Combine(fullFolder, fileName);
        var adapterRef = $"{safeFolder}/{fileName}";

        using var sha = SHA256.Create();
        long size;

        // Stream a disco + calcular hash en una sola pasada.
        await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        await using (var hashStream = new CryptoStream(output, sha, CryptoStreamMode.Write))
        {
            await content.CopyToAsync(hashStream, ct);
            await hashStream.FlushFinalBlockAsync(ct);
            size = output.Length;
        }

        var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        _logger.LogDebug("Stored photo at {Path} ({Size} bytes, sha256={Hash})", fullPath, size, hash);
        return new StoredPhotoRef(AdapterName, adapterRef, size, hash);
    }

    public Task<Stream> ReadAsync(string adapterRef, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_options.RootPath, adapterRef);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Photo not found in local storage.", fullPath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string adapterRef, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_options.RootPath, adapterRef);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_options.RootPath);
            // Round-trip: escribir + leer + borrar un archivo dummy.
            var probe = Path.Combine(_options.RootPath, $".probe-{Guid.NewGuid():N}.tmp");
            File.WriteAllBytes(probe, new byte[] { 0x01, 0x02, 0x03 });
            var read = File.ReadAllBytes(probe);
            File.Delete(probe);
            return Task.FromResult(read.Length == 3);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local storage TestConnection failed.");
            return Task.FromResult(false);
        }
    }

    private static string SanitizeFileName(string input)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var clean = new string(input.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "untitled" : clean;
    }

    /// <summary>Sanitiza una jerarquía de carpetas conservando los <c>/</c>: cada segmento se
    /// sanitiza individualmente. <c>..</c> se elimina para evitar path traversal.</summary>
    private static string SanitizeFolder(string input)
    {
        var segments = input.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => SanitizeFileName(s).Replace("..", string.Empty))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToArray();
        return segments.Length == 0 ? "default" : string.Join("/", segments);
    }
}
