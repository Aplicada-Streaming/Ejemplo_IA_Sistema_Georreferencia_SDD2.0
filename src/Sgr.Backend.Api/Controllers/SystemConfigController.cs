using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sgr.Domain.Identity;
using Sgr.Modules.Storage.Configuration;

namespace Sgr.Backend.Api.Controllers;

/// <summary>
/// Configuración de sistema (US-17 / CU-02). Sólo accesible al admin raíz.
/// El endpoint <c>storage/test</c> NO persiste — sirve para validar credenciales
/// antes de guardar (CA-17.3).
/// </summary>
[ApiController]
[Authorize(Roles = UserRole.AdminRaiz)]
[Route("api/v1/system-config")]
[Produces("application/json")]
public sealed class SystemConfigController : ControllerBase
{
    private readonly IStorageConfigService _storageConfig;

    public SystemConfigController(IStorageConfigService storageConfig)
    {
        _storageConfig = storageConfig;
    }

    /// <summary>
    /// Devuelve la config de storage activa, sin credenciales (las masquerea para no
    /// filtrarlas a la UI). Si nunca se configuró, devuelve 204.
    /// </summary>
    [HttpGet("storage")]
    [ProducesResponseType(typeof(StorageConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<StorageConfigDto>> GetStorage(CancellationToken ct)
    {
        var current = await _storageConfig.GetActiveAsync(ct);
        if (current is null) return NoContent();
        return Ok(MaskSecrets(current));
    }

    /// <summary>
    /// Guarda la config de storage activa. Si las credenciales ya existían y la UI
    /// mandó la masked-form (e.g. "***"), preservamos la del DB; si manda nuevas,
    /// las cifra y reemplaza.
    /// </summary>
    [HttpPost("storage")]
    [ProducesResponseType(typeof(StorageConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorageConfigDto>> SaveStorage(
        [FromBody] StorageConfigDto body,
        CancellationToken ct)
    {
        var current = CurrentUserAccessor.FromPrincipal(User);
        var merged = await MergeWithExistingAsync(body, ct);
        await _storageConfig.SaveAsync(merged, current.UserId, ct);
        return Ok(MaskSecrets(merged));
    }

    /// <summary>
    /// Prueba la config sin persistir (CA-17.3). Crea un adapter ephemeral con la
    /// config provista y ejecuta <c>TestConnectionAsync</c> (escribe/lee/borra blob dummy).
    /// </summary>
    [HttpPost("storage/test")]
    [ProducesResponseType(typeof(StorageTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageTestResult>> TestStorage(
        [FromBody] StorageConfigDto body,
        CancellationToken ct)
    {
        var merged = await MergeWithExistingAsync(body, ct);
        var result = await _storageConfig.TestAsync(merged, ct);
        return Ok(result);
    }

    /// <summary>
    /// Si la UI manda credenciales masked (`SecretsMaskValue`), las reemplaza por las
    /// del DB. Permite editar otros campos sin tener que re-tipear las credenciales.
    /// </summary>
    private async Task<StorageConfigDto> MergeWithExistingAsync(StorageConfigDto incoming, CancellationToken ct)
    {
        var existing = await _storageConfig.GetActiveAsync(ct);
        if (existing is null) return incoming;

        return incoming with
        {
            Config = incoming.Config with
            {
                S3 = incoming.Config.S3 is null ? null : incoming.Config.S3 with
                {
                    SecretKey = IsMasked(incoming.Config.S3.SecretKey) && existing.Config.S3 is not null
                        ? existing.Config.S3.SecretKey
                        : incoming.Config.S3.SecretKey,
                },
                Ftp = incoming.Config.Ftp is null ? null : incoming.Config.Ftp with
                {
                    Password = IsMasked(incoming.Config.Ftp.Password) && existing.Config.Ftp is not null
                        ? existing.Config.Ftp.Password
                        : incoming.Config.Ftp.Password,
                },
                Sftp = incoming.Config.Sftp is null ? null : incoming.Config.Sftp with
                {
                    Password = IsMasked(incoming.Config.Sftp.Password ?? "") && existing.Config.Sftp is not null
                        ? existing.Config.Sftp.Password
                        : incoming.Config.Sftp.Password,
                    PrivateKeyPassphrase = IsMasked(incoming.Config.Sftp.PrivateKeyPassphrase ?? "")
                                                && existing.Config.Sftp is not null
                        ? existing.Config.Sftp.PrivateKeyPassphrase
                        : incoming.Config.Sftp.PrivateKeyPassphrase,
                },
            },
        };
    }

    private const string SecretsMaskValue = "***";
    private static bool IsMasked(string s) => s == SecretsMaskValue;

    private static StorageConfigDto MaskSecrets(StorageConfigDto dto) => dto with
    {
        Config = dto.Config with
        {
            S3 = dto.Config.S3 is null ? null : dto.Config.S3 with { SecretKey = SecretsMaskValue },
            Ftp = dto.Config.Ftp is null ? null : dto.Config.Ftp with { Password = SecretsMaskValue },
            Sftp = dto.Config.Sftp is null ? null : dto.Config.Sftp with
            {
                Password = dto.Config.Sftp.Password is null ? null : SecretsMaskValue,
                PrivateKeyPassphrase = dto.Config.Sftp.PrivateKeyPassphrase is null ? null : SecretsMaskValue,
            },
        },
    };
}
