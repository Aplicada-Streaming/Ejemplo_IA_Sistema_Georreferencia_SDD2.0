using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Sgr.Modules.Storage;
using Sgr.Modules.Surveys.Application;
using Sgr.Persistence;

namespace Sgr.Backend.Api.Startup;

/// <summary>
/// E.6.a — Empaqueta una exportación de relevamiento como ZIP con CSV/XLSX/GeoJSON +
/// fotos. Vive en la capa API (no en módulos) porque combina dos módulos
/// (<c>Surveys</c> para los datos y <c>Storage</c> para los binarios).
///
/// Layout dentro del ZIP:
/// <code>
///   data.csv
///   data.xlsx
///   data.geojson
///   photos/{pointId}/{contentHashShort}_{originalName}.{ext}
/// </code>
///
/// Streaming (DT-export-zip-streaming resuelta): escribimos directo al stream de
/// salida (usualmente <c>Response.Body</c>), sin buffer intermedio. Las fotos van
/// con <c>CompressionLevel.NoCompression</c> porque ya son JPEG (recomprimir gasta
/// CPU sin reducir tamaño). El central-directory del ZIP se ajusta al stream
/// no-seekable porque <see cref="ZipArchiveMode.Create"/> sólo escribe forward.
///
/// Si la lectura de una foto falla (storage caído), se omite del ZIP y se loggea —
/// no rompemos la exportación entera por una foto rota.
/// </summary>
public sealed class SurveyZipBundler
{
    private readonly SgrDbContext _db;
    private readonly IExportSurveyService _export;
    private readonly IPhotoStorageAdapterFactory _adapters;
    private readonly ILogger<SurveyZipBundler> _logger;

    public SurveyZipBundler(
        SgrDbContext db,
        IExportSurveyService export,
        IPhotoStorageAdapterFactory adapters,
        ILogger<SurveyZipBundler> logger)
    {
        _db = db;
        _export = export;
        _adapters = adapters;
        _logger = logger;
    }

    /// <summary>
    /// Escribe el ZIP directamente al <paramref name="output"/> (no buffer).
    /// El caller setea Content-Type/Content-Disposition antes de llamar.
    /// </summary>
    public async Task BuildAsync(Stream output, Guid surveyId, CurrentUserContext currentUser, CancellationToken ct)
    {
        // Los formatos chicos (CSV/XLSX/GeoJSON) los generamos en bytes — son
        // fracciones de MB y simplifica el código. Las fotos sí van streaming.
        var csv = await _export.GenerateCsvAsync(surveyId, currentUser, ct);
        var xlsx = await _export.GenerateXlsxAsync(surveyId, currentUser, ct);
        var geo = await _export.GenerateGeoJsonAsync(surveyId, currentUser, ct);

        var photos = await _db.Photos.AsNoTracking()
            .Where(p => !p.IsDeleted &&
                _db.Points.Any(pt => pt.Id == p.PointId && pt.SurveyId == surveyId))
            .OrderBy(p => p.PointId).ThenBy(p => p.CreatedAt)
            .Select(p => new { p.Id, p.PointId, p.AdapterName, p.AdapterRef, p.ContentHash })
            .ToListAsync(ct);

        // leaveOpen:true para no cerrar Response.Body cuando el ZipArchive se dispone.
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteEntryAsync(zip, "data.csv", csv, CompressionLevel.Optimal, ct);
            await WriteEntryAsync(zip, "data.xlsx", xlsx, CompressionLevel.NoCompression, ct);
            await WriteEntryAsync(zip, "data.geojson", geo, CompressionLevel.Optimal, ct);

            foreach (var ph in photos)
            {
                try
                {
                    var adapter = _adapters.GetByName(ph.AdapterName);
                    await using var src = await adapter.ReadAsync(ph.AdapterRef, ct);

                    var originalName = ph.AdapterRef.Split('/').LastOrDefault() ?? "photo.jpg";
                    var entryPath = $"photos/{ph.PointId}/{ph.ContentHash[..8]}_{originalName}";

                    // NoCompression para JPEG: ya está comprimido, recomprimir
                    // sólo gasta CPU sin reducir tamaño (a veces incluso lo aumenta).
                    var entry = zip.CreateEntry(entryPath, CompressionLevel.NoCompression);
                    await using var dst = entry.Open();
                    await src.CopyToAsync(dst, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No pude incluir foto {PhotoId} ({AdapterRef}) en el ZIP — se omite.",
                        ph.Id, ph.AdapterRef);
                }
            }
        }
    }

    private static async Task WriteEntryAsync(ZipArchive zip, string name, byte[] bytes,
        CompressionLevel level, CancellationToken ct)
    {
        var entry = zip.CreateEntry(name, level);
        await using var dst = entry.Open();
        await dst.WriteAsync(bytes, ct);
    }
}
