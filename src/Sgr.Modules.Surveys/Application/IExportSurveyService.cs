using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Sgr.Modules.Templates.Application;
using Sgr.Persistence;

namespace Sgr.Modules.Surveys.Application;

public interface IExportSurveyService
{
    /// <summary>
    /// Genera CSV con un row por punto. Columnas fijas
    /// (id, lat, lng, accuracyM, captureMode, createdAt, updatedAt, photoCount)
    /// + una columna por cada campo de la plantilla (en el orden definido).
    /// El archivo lleva BOM UTF-8 para que Excel detecte la codificación.
    /// </summary>
    Task<byte[]> GenerateCsvAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default);

    /// <summary>
    /// Genera GeoJSON FeatureCollection. Cada Feature es Point (lng/lat) con
    /// todos los metadata + field values en `properties`.
    /// </summary>
    Task<byte[]> GenerateGeoJsonAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default);

    /// <summary>
    /// Genera XLSX (Excel nativo). Mismas columnas que CSV pero con tipos preservados
    /// (números como números, fechas como fechas) — el usuario abre y filtra/ordena
    /// sin importar manualmente. Header en negrita y panel congelado.
    /// </summary>
    Task<byte[]> GenerateXlsxAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default);
}

public sealed class ExportSurveyService : IExportSurveyService
{
    private readonly SgrDbContext _db;
    private readonly IGetSurveyService _getSurvey;
    private readonly IGetTemplateVersionService _getTemplate;

    public ExportSurveyService(
        SgrDbContext db,
        IGetSurveyService getSurvey,
        IGetTemplateVersionService getTemplate)
    {
        _db = db;
        _getSurvey = getSurvey;
        _getTemplate = getTemplate;
    }

    public async Task<byte[]> GenerateCsvAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default)
    {
        var data = await LoadAsync(surveyId, currentUser, ct);

        // Separador `;` (no `,`): Excel en es-AR/es-ES usa `;` como list separator.
        // Con BOM UTF-8 y `;` los acentos y los decimales en formato regional funcionan
        // sin que el usuario tenga que importar manualmente desde "Datos → Texto a columnas".
        const string Sep = ";";

        var sb = new StringBuilder();
        var fixedCols = new[] { "id", "latitude", "longitude", "accuracyM", "captureMode",
                                "createdAt", "updatedAt", "photoCount" };
        var fieldKeys = data.Template.Fields.Select(f => f.Key).ToArray();

        // Header
        sb.AppendLine(string.Join(Sep, fixedCols.Concat(fieldKeys).Select(EscapeCsv)));

        // Rows
        foreach (var p in data.Points)
        {
            data.PhotoCounts.TryGetValue(p.Id, out var photoCount);
            var fixedValues = new[]
            {
                p.Id.ToString(),
                p.Latitude.ToString("F6", CultureInfo.InvariantCulture),
                p.Longitude.ToString("F6", CultureInfo.InvariantCulture),
                p.AccuracyM?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty,
                p.CaptureMode,
                p.CreatedAt.ToString("o"),
                p.UpdatedAt.ToString("o"),
                photoCount.ToString(CultureInfo.InvariantCulture),
            };
            var fieldValues = fieldKeys.Select(k =>
                data.FieldValues.TryGetValue((p.Id, k), out var raw) ? UnwrapJsonValue(raw) : string.Empty);

            sb.AppendLine(string.Join(Sep, fixedValues.Concat(fieldValues).Select(EscapeCsv)));
        }

        // BOM UTF-8 para Excel.
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    public async Task<byte[]> GenerateGeoJsonAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default)
    {
        var data = await LoadAsync(surveyId, currentUser, ct);

        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("type", "FeatureCollection");
        writer.WritePropertyName("metadata");
        writer.WriteStartObject();
        writer.WriteString("surveyId", data.Survey.Id.ToString());
        writer.WriteString("surveyName", data.Survey.Name);
        writer.WriteString("templateVersionId", data.Template.VersionId.ToString());
        writer.WriteString("exportedAtUtc", DateTime.UtcNow.ToString("o"));
        writer.WriteNumber("pointCount", data.Points.Count);
        writer.WriteEndObject();

        writer.WritePropertyName("features");
        writer.WriteStartArray();
        foreach (var p in data.Points)
        {
            data.PhotoCounts.TryGetValue(p.Id, out var photoCount);
            writer.WriteStartObject();
            writer.WriteString("type", "Feature");
            // Geometry
            writer.WritePropertyName("geometry");
            writer.WriteStartObject();
            writer.WriteString("type", "Point");
            writer.WritePropertyName("coordinates");
            writer.WriteStartArray();
            writer.WriteNumberValue((double)p.Longitude);
            writer.WriteNumberValue((double)p.Latitude);
            writer.WriteEndArray();
            writer.WriteEndObject();
            // Properties
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteString("id", p.Id.ToString());
            if (p.AccuracyM is not null)
                writer.WriteNumber("accuracyM", (double)p.AccuracyM.Value);
            writer.WriteString("captureMode", p.CaptureMode);
            writer.WriteString("createdAt", p.CreatedAt.ToString("o"));
            writer.WriteString("updatedAt", p.UpdatedAt.ToString("o"));
            writer.WriteNumber("photoCount", photoCount);
            foreach (var f in data.Template.Fields)
            {
                if (data.FieldValues.TryGetValue((p.Id, f.Key), out var raw) && !string.IsNullOrEmpty(raw))
                {
                    writer.WritePropertyName(f.Key);
                    // Escribimos el JSON crudo del valor (ya está bien tipado en BD).
                    using var doc = JsonDocument.Parse(raw);
                    doc.RootElement.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();

        await writer.FlushAsync(ct);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateXlsxAsync(Guid surveyId, CurrentUserContext currentUser, CancellationToken ct = default)
    {
        var data = await LoadAsync(surveyId, currentUser, ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Puntos");

        var fixedCols = new[] { "id", "latitude", "longitude", "accuracyM", "captureMode",
                                "createdAt", "updatedAt", "photoCount" };
        var fieldKeys = data.Template.Fields.Select(f => f.Key).ToArray();
        var fieldLabels = data.Template.Fields.ToDictionary(f => f.Key, f => f.Label);
        var fieldTypes = data.Template.Fields.ToDictionary(f => f.Key, f => f.Type);

        // Header — usamos label legible para campos del template (ej. "Fecha de inspección"
        // en vez de "fecha_inspeccion"); los fixed cols quedan con el nombre técnico para
        // que coincidan con CSV/GeoJSON y sean útiles para análisis.
        var headers = fixedCols.Concat(fieldKeys.Select(k => fieldLabels.GetValueOrDefault(k, k))).ToArray();
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }

        // Data rows
        for (int row = 0; row < data.Points.Count; row++)
        {
            var p = data.Points[row];
            data.PhotoCounts.TryGetValue(p.Id, out var photoCount);

            ws.Cell(row + 2, 1).Value = p.Id.ToString();
            ws.Cell(row + 2, 2).Value = (double)p.Latitude;
            ws.Cell(row + 2, 3).Value = (double)p.Longitude;
            if (p.AccuracyM is not null)
                ws.Cell(row + 2, 4).Value = (double)p.AccuracyM.Value;
            ws.Cell(row + 2, 5).Value = p.CaptureMode;
            ws.Cell(row + 2, 6).Value = p.CreatedAt;
            ws.Cell(row + 2, 6).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            ws.Cell(row + 2, 7).Value = p.UpdatedAt;
            ws.Cell(row + 2, 7).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            ws.Cell(row + 2, 8).Value = photoCount;

            // Field values con tipos preservados según declaración de la plantilla.
            for (int i = 0; i < fieldKeys.Length; i++)
            {
                if (!data.FieldValues.TryGetValue((p.Id, fieldKeys[i]), out var raw) || string.IsNullOrEmpty(raw))
                    continue;
                var cell = ws.Cell(row + 2, fixedCols.Length + i + 1);
                WriteTypedValue(cell, raw, fieldTypes.GetValueOrDefault(fieldKeys[i], string.Empty));
            }
        }

        // Header congelado y autosize de columnas para que el usuario no tenga que ajustar.
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();

        // Hoja secundaria con metadata de la exportación (útil para auditoría).
        var meta = workbook.Worksheets.Add("Info");
        meta.Cell(1, 1).Value = "Relevamiento";
        meta.Cell(1, 2).Value = data.Survey.Name;
        meta.Cell(2, 1).Value = "ID";
        meta.Cell(2, 2).Value = data.Survey.Id.ToString();
        meta.Cell(3, 1).Value = "Plantilla v";
        meta.Cell(3, 2).Value = $"{data.Template.TemplateName} v{data.Template.VersionNumber}";
        meta.Cell(4, 1).Value = "Puntos";
        meta.Cell(4, 2).Value = data.Points.Count;
        meta.Cell(5, 1).Value = "Exportado UTC";
        meta.Cell(5, 2).Value = DateTime.UtcNow;
        meta.Cell(5, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
        meta.Column(1).Style.Font.Bold = true;
        meta.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Escribe el value JSON crudo en una celda, preservando el tipo declarado por
    /// la plantilla cuando se puede. Los <c>numero</c> y <c>fecha</c> entran como
    /// número/fecha real para que Excel los entienda.
    /// </summary>
    private static void WriteTypedValue(IXLCell cell, string rawJson, string fieldType)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            switch (fieldType)
            {
                case "numero":
                    if (root.TryGetDecimal(out var dec))
                        cell.Value = (double)dec;
                    else
                        cell.Value = root.ToString();
                    break;
                case "fecha":
                    if (root.ValueKind == JsonValueKind.String &&
                        DateTime.TryParse(root.GetString(), out var dt))
                    {
                        cell.Value = dt;
                        cell.Style.DateFormat.Format = "yyyy-mm-dd";
                    }
                    else
                    {
                        cell.Value = root.ToString();
                    }
                    break;
                case "booleano":
                    cell.Value = root.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => root.ToString(),
                    };
                    break;
                default: // texto, selección, desconocido
                    cell.Value = root.ValueKind == JsonValueKind.String
                        ? root.GetString() ?? string.Empty
                        : root.ToString();
                    break;
            }
        }
        catch
        {
            cell.Value = rawJson;
        }
    }

    private async Task<ExportData> LoadAsync(Guid surveyId, CurrentUserContext user, CancellationToken ct)
    {
        // Visibilidad por rol — reusamos GetSurveyAsync para que tire 403/404 si el user no puede.
        var survey = await _getSurvey.GetByIdAsync(surveyId, user, ct);

        var template = await _getTemplate.GetForSurveyAsync(surveyId, ct);

        var points = await _db.Points.AsNoTracking()
            .Where(p => p.SurveyId == surveyId && !p.IsDeleted)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PointRow(
                p.Id, p.Latitude, p.Longitude, p.AccuracyM, p.CaptureMode,
                p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);

        var pointIds = points.Select(p => p.Id).ToHashSet();

        var fieldValues = new Dictionary<(Guid, string), string?>();
        if (pointIds.Count > 0)
        {
            var rows = await _db.PointFieldValues.AsNoTracking()
                .Where(v => pointIds.Contains(v.PointId))
                .Select(v => new { v.PointId, v.FieldKey, v.ValueJson })
                .ToListAsync(ct);
            foreach (var r in rows)
                fieldValues[(r.PointId, r.FieldKey)] = r.ValueJson;
        }

        var photoCounts = new Dictionary<Guid, int>();
        if (pointIds.Count > 0)
        {
            var counts = await _db.Photos.AsNoTracking()
                .Where(p => pointIds.Contains(p.PointId) && !p.IsDeleted)
                .GroupBy(p => p.PointId)
                .Select(g => new { PointId = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            foreach (var c in counts)
                photoCounts[c.PointId] = c.Count;
        }

        return new ExportData(survey, template, points, fieldValues, photoCounts);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Detectar el separador `;` además de `,` por si en el futuro cambiamos.
        var needsQuote = value.IndexOfAny(new[] { ';', ',', '"', '\n', '\r' }) >= 0;
        if (!needsQuote) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>El JSON crudo guarda strings con comillas: <c>"Buena"</c>.
    /// Para CSV/Excel queremos el valor limpio; para tipos no-string respetamos el formato textual.</summary>
    private static string UnwrapJsonValue(string? rawJson)
    {
        if (string.IsNullOrEmpty(rawJson)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            return doc.RootElement.ValueKind switch
            {
                JsonValueKind.String => doc.RootElement.GetString() ?? string.Empty,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => doc.RootElement.ToString(),
            };
        }
        catch
        {
            return rawJson;
        }
    }

    private sealed record ExportData(
        SurveyDto Survey,
        TemplateVersionDetailDto Template,
        IReadOnlyList<PointRow> Points,
        Dictionary<(Guid PointId, string FieldKey), string?> FieldValues,
        Dictionary<Guid, int> PhotoCounts);

    private sealed record PointRow(
        Guid Id,
        decimal Latitude,
        decimal Longitude,
        decimal? AccuracyM,
        string CaptureMode,
        DateTime CreatedAt,
        DateTime UpdatedAt);
}
