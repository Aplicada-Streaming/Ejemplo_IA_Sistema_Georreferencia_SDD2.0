namespace Sgr.Modules.Storage.Imaging;

/// <summary>
/// Lee metadata EXIF de una foto. Devuelve <see cref="ExifData.None"/> si la foto
/// no tiene EXIF o si los campos relevantes (GPS / fecha) están vacíos. La idea es
/// nunca tirar excepciones sobre fotos malformadas — el caller decide si encolarlas
/// como "pendientes de georreferenciar" o si la fecha de captura cae al fallback
/// de la fecha de subida.
/// </summary>
public interface IExifReader
{
    ExifData Read(Stream content);
}

public sealed record ExifData(
    decimal? Latitude,
    decimal? Longitude,
    DateTime? TakenAtUtc)
{
    public static readonly ExifData None = new(null, null, null);

    public bool HasGps => Latitude is not null && Longitude is not null;
}
