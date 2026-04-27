using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.Extensions.Logging;

namespace Sgr.Modules.Storage.Imaging;

/// <summary>
/// Implementación de <see cref="IExifReader"/> usando <c>MetadataExtractor</c>.
/// Defensiva ante cualquier malformación: en caso de error devuelve <see cref="ExifData.None"/>
/// y loggea — la foto sigue subiéndose, simplemente entra a la cola pendiente de georreferenciar.
/// </summary>
public sealed class MetadataExtractorExifReader : IExifReader
{
    private readonly ILogger<MetadataExtractorExifReader> _logger;

    public MetadataExtractorExifReader(ILogger<MetadataExtractorExifReader> logger)
    {
        _logger = logger;
    }

    public ExifData Read(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            // ImageMetadataReader hace seek si puede; si no, lee desde el offset actual.
            if (content.CanSeek) content.Position = 0;

            var directories = ImageMetadataReader.ReadMetadata(content);

            // GPS: GpsDirectory expone GeoLocation? (struct nullable). Latitude/Longitude vienen
            // ya convertidos de DMS a grados decimales — perfecto para guardar como decimal.
            var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
            decimal? lat = null, lng = null;
            var geo = gps?.GetGeoLocation();
            if (geo is { } g && !g.IsZero)
            {
                lat = (decimal)g.Latitude;
                lng = (decimal)g.Longitude;
            }

            // Fecha: prioridad DateTimeOriginal (cuando se disparó la foto) > DateTime (modificación).
            var subIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            DateTime? takenAtUtc = null;
            if (subIfd is not null)
            {
                if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dto))
                    takenAtUtc = DateTime.SpecifyKind(dto, DateTimeKind.Utc);
                else if (subIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var ddg))
                    takenAtUtc = DateTime.SpecifyKind(ddg, DateTimeKind.Utc);
            }
            if (takenAtUtc is null)
            {
                var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (ifd0 is not null && ifd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dt))
                    takenAtUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }

            return new ExifData(lat, lng, takenAtUtc);
        }
        catch (Exception ex)
        {
            // Foto malformada o sin EXIF — devolvemos None y dejamos que la cola pendiente la maneje.
            _logger.LogDebug(ex, "EXIF read failed; returning empty.");
            return ExifData.None;
        }
    }
}
