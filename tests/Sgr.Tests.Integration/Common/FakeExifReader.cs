using System.Collections.Concurrent;
using Sgr.Modules.Storage.Imaging;

namespace Sgr.Tests.Integration.Common;

/// <summary>
/// Reemplaza al <see cref="IExifReader"/> real para tests determinísticos: registramos
/// un mapa "filename → ExifData" y devolvemos el valor según el primer chunk del stream.
///
/// Como el ManualUploadService bufferea el stream antes de pasarlo, el truco es marcar
/// las "imágenes" de test con un prefijo identificable y leer ese prefijo como tag.
/// </summary>
public sealed class FakeExifReader : IExifReader
{
    private readonly ConcurrentDictionary<string, ExifData> _byTag = new();

    public void Register(string tag, ExifData exif) => _byTag[tag] = exif;
    public void Reset() => _byTag.Clear();

    public ExifData Read(Stream content)
    {
        if (!content.CanSeek) return ExifData.None;
        var pos = content.Position;
        try
        {
            content.Position = 0;
            using var sr = new StreamReader(content, leaveOpen: true);
            // Leemos sólo la primera línea (suficiente para nuestros tests).
            var firstLine = sr.ReadLine() ?? string.Empty;
            // Formato esperado del payload de test: "EXIF-TAG:<tag>" en la primera línea.
            const string prefix = "EXIF-TAG:";
            if (!firstLine.StartsWith(prefix, StringComparison.Ordinal))
                return ExifData.None;
            var tag = firstLine[prefix.Length..].Trim();
            return _byTag.TryGetValue(tag, out var exif) ? exif : ExifData.None;
        }
        finally
        {
            content.Position = pos;
        }
    }
}
