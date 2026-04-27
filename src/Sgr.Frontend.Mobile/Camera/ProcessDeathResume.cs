namespace Sgr.Frontend.Mobile.Camera;

/// <summary>
/// Maneja el resume-path para sobrevivir a un process-death durante el intent
/// de cámara (Android low-memory kill).
///
/// Patrón:
///   1. Antes de <c>MediaPicker.CapturePhotoAsync</c>, llamar <see cref="MarkResumePath"/>
///      con la ruta actual del Blazor.
///   2. Después del retorno (sin process-death), llamar <see cref="ClearResumePath"/>
///      para limpiar la marca y evitar redirigir spuriosamente la próxima vez.
///   3. Cuando Blazor arranca en <c>/</c> (Home) tras un restart, leer
///      <see cref="ConsumeResumePathIfRecent"/>; si hay una ruta reciente, redirigir
///      a esa página.
///
/// Las marcas son válidas durante 2 minutos (margen razonable para que el usuario
/// saque la foto y vuelva); después de ese plazo se ignoran para no redirigir
/// usuarios que reabren la app horas después.
/// </summary>
public static class ProcessDeathResume
{
    private const string PathKey = "sgr.resume_path";
    private const string SetAtKey = "sgr.resume_path_set_at_utc";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);

    public static void MarkResumePath(string path)
    {
        Preferences.Default.Set(PathKey, path);
        Preferences.Default.Set(SetAtKey, DateTime.UtcNow.ToString("o"));
    }

    public static void ClearResumePath()
    {
        Preferences.Default.Remove(PathKey);
        Preferences.Default.Remove(SetAtKey);
    }

    /// <summary>Devuelve la ruta guardada si existe y es reciente, y la consume.
    /// Si no hay ruta o expiró, devuelve null.</summary>
    public static string? ConsumeResumePathIfRecent()
    {
        var path = Preferences.Default.Get(PathKey, string.Empty);
        var setAtRaw = Preferences.Default.Get(SetAtKey, string.Empty);
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(setAtRaw))
            return null;

        if (!DateTime.TryParse(setAtRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var setAt))
        {
            ClearResumePath();
            return null;
        }

        if (DateTime.UtcNow - setAt > Ttl)
        {
            ClearResumePath();
            return null;
        }

        ClearResumePath();
        return path;
    }
}
