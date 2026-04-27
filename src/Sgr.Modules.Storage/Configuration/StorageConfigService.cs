using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Sgr.Domain.Common;
using Sgr.Domain.Photos;
using Sgr.Domain.SystemConfig;
using Sgr.Persistence;

namespace Sgr.Modules.Storage.Configuration;

/// <summary>
/// Persistencia + test de la configuración de storage. Las credenciales sensibles
/// (S3 SecretKey, FTP/SFTP Password, SFTP PrivateKeyPassphrase) se cifran con
/// <see cref="IDataProtector"/> antes de tocar la DB y se descifran al leer.
/// </summary>
public sealed class StorageConfigService : IStorageConfigService
{
    private const string ProtectorPurpose = "Sgr.SystemConfig.StorageCredentials.v1";

    private readonly SgrDbContext _db;
    private readonly IDataProtectionProvider _protectionProvider;
    private readonly IDateTimeProvider _clock;
    private readonly IStorageAdapterBuilder _adapterBuilder;

    public StorageConfigService(
        SgrDbContext db,
        IDataProtectionProvider protectionProvider,
        IDateTimeProvider clock,
        IStorageAdapterBuilder adapterBuilder)
    {
        _db = db;
        _protectionProvider = protectionProvider;
        _clock = clock;
        _adapterBuilder = adapterBuilder;
    }

    public async Task<StorageConfigDto?> GetActiveAsync(CancellationToken ct = default)
    {
        var entry = await _db.SystemConfig.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Key == SystemConfigKeys.StorageActive, ct);
        if (entry is null) return null;

        var stored = JsonSerializer.Deserialize<StorageConfigDto>(entry.ValueJson, StorageConfigJson.Options);
        if (stored is null) return null;
        return Decrypt(stored);
    }

    public async Task SaveAsync(StorageConfigDto config, Guid actorUserId, CancellationToken ct = default)
    {
        Validate(config);
        var encrypted = Encrypt(config);
        var json = JsonSerializer.Serialize(encrypted, StorageConfigJson.Options);
        var now = _clock.UtcNow;

        var entry = await _db.SystemConfig
            .FirstOrDefaultAsync(e => e.Key == SystemConfigKeys.StorageActive, ct);
        if (entry is null)
        {
            entry = SystemConfigEntry.Create(Guid.NewGuid(),
                SystemConfigKeys.StorageActive, json, actorUserId, now);
            _db.SystemConfig.Add(entry);
        }
        else
        {
            entry.UpdateValue(json, actorUserId, now);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<StorageTestResult> TestAsync(StorageConfigDto config, CancellationToken ct = default)
    {
        Validate(config);
        var adapter = _adapterBuilder.Build(config);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var ok = await adapter.TestConnectionAsync(ct);
            sw.Stop();
            return ok
                ? new StorageTestResult(true, "Conexión OK.", sw.ElapsedMilliseconds)
                : new StorageTestResult(false, "El adapter reportó conexión fallida.", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new StorageTestResult(false, $"{ex.GetType().Name}: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }

    private static void Validate(StorageConfigDto config)
    {
        if (!StorageAdapterNames.IsValid(config.Adapter))
            throw new ArgumentException($"Adapter '{config.Adapter}' inválido.");
        switch (config.Adapter)
        {
            case "local" when config.Config.Local is null:
                throw new ArgumentException("Falta config 'local'.");
            case "s3" when config.Config.S3 is null:
                throw new ArgumentException("Falta config 's3'.");
            case "ftp" when config.Config.Ftp is null:
                throw new ArgumentException("Falta config 'ftp'.");
            case "sftp" when config.Config.Sftp is null:
                throw new ArgumentException("Falta config 'sftp'.");
        }
    }

    private StorageConfigDto Encrypt(StorageConfigDto plain)
    {
        var protector = _protectionProvider.CreateProtector(ProtectorPurpose);
        return plain with
        {
            Config = plain.Config with
            {
                S3 = plain.Config.S3 is null ? null : plain.Config.S3 with
                {
                    SecretKey = protector.Protect(plain.Config.S3.SecretKey),
                },
                Ftp = plain.Config.Ftp is null ? null : plain.Config.Ftp with
                {
                    Password = protector.Protect(plain.Config.Ftp.Password),
                },
                Sftp = plain.Config.Sftp is null ? null : plain.Config.Sftp with
                {
                    Password = plain.Config.Sftp.Password is null
                        ? null
                        : protector.Protect(plain.Config.Sftp.Password),
                    PrivateKeyPassphrase = plain.Config.Sftp.PrivateKeyPassphrase is null
                        ? null
                        : protector.Protect(plain.Config.Sftp.PrivateKeyPassphrase),
                },
            },
        };
    }

    private StorageConfigDto Decrypt(StorageConfigDto cipher)
    {
        var protector = _protectionProvider.CreateProtector(ProtectorPurpose);
        return cipher with
        {
            Config = cipher.Config with
            {
                S3 = cipher.Config.S3 is null ? null : cipher.Config.S3 with
                {
                    SecretKey = protector.Unprotect(cipher.Config.S3.SecretKey),
                },
                Ftp = cipher.Config.Ftp is null ? null : cipher.Config.Ftp with
                {
                    Password = protector.Unprotect(cipher.Config.Ftp.Password),
                },
                Sftp = cipher.Config.Sftp is null ? null : cipher.Config.Sftp with
                {
                    Password = cipher.Config.Sftp.Password is null
                        ? null
                        : protector.Unprotect(cipher.Config.Sftp.Password),
                    PrivateKeyPassphrase = cipher.Config.Sftp.PrivateKeyPassphrase is null
                        ? null
                        : protector.Unprotect(cipher.Config.Sftp.PrivateKeyPassphrase),
                },
            },
        };
    }
}
