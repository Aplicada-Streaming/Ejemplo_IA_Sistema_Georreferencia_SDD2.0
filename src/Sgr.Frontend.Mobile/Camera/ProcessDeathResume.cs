using System.Text.Json;

namespace Sgr.Frontend.Mobile.Camera;

/// <summary>
/// Maneja el resume completo (ruta + state de captura) para sobrevivir a un
/// process-death durante el intent de cámara (Android low-memory kill).
///
/// Patrón:
///   1. Antes de <c>MediaPicker.CapturePhotoAsync</c>, llamar <see cref="MarkResumePoint"/>
///      con la ruta actual del Blazor + el state que querés recuperar (lat/lng/foto previa).
///   2. Después del retorno (sin process-death), llamar <see cref="ClearResumePath"/>
///      para limpiar la marca y evitar redirigir spuriosamente la próxima vez.
///   3. Cuando Blazor arranca en <c>/</c> (Home) tras un restart, leer
///      <see cref="ConsumePathIfRecent"/>; si hay una ruta reciente, redirigir
///      a esa página. La página destino llama <see cref="TryConsumeState{T}"/> en
///      su <c>OnInitialized</c> para rehidratar el state perdido.
///
/// Las marcas son válidas durante 2 minutos (margen razonable para que el usuario
/// saque la foto y vuelva); después de ese plazo se ignoran para no redirigir
/// usuarios que reabren la app horas después.
/// </summary>
public static class ProcessDeathResume
{
    private const string PathKey = "sgr.resume_path";
    private const string SetAtKey = "sgr.resume_path_set_at_utc";
    private const string StateKey = "sgr.resume_state";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Persiste path + state opcional. Si <paramref name="state"/> es null sólo guarda la ruta
    /// (compat con flujos viejos que usaban MarkResumePath).
    /// </summary>
    public static void MarkResumePoint<T>(string path, T? state)
        where T : class
    {
        Preferences.Default.Set(PathKey, path);
        Preferences.Default.Set(SetAtKey, DateTime.UtcNow.ToString("o"));
        if (state is null)
            Preferences.Default.Remove(StateKey);
        else
            Preferences.Default.Set(StateKey, JsonSerializer.Serialize(state, JsonOpts));
    }

    /// <summary>Variante sin state — preserva sólo la ruta (DT-camera-state-resume legacy).</summary>
    public static void MarkResumePath(string path) => MarkResumePoint<object>(path, null);

    public static void ClearResumePath()
    {
        Preferences.Default.Remove(PathKey);
        Preferences.Default.Remove(SetAtKey);
        Preferences.Default.Remove(StateKey);
    }

    /// <summary>
    /// Devuelve la ruta guardada si existe y es reciente, y la consume.
    /// IMPORTANTE: NO consume el state — eso lo hace la página destino con
    /// <see cref="TryConsumeState{T}"/> después de re-renderearse.
    /// </summary>
    public static string? ConsumePathIfRecent()
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

        // Limpiamos sólo path/setAt; el state queda hasta que la página lo consuma.
        Preferences.Default.Remove(PathKey);
        Preferences.Default.Remove(SetAtKey);
        return path;
    }

    /// <summary>Alias retro-compatible para callers que ya usaban este nombre.</summary>
    public static string? ConsumeResumePathIfRecent() => ConsumePathIfRecent();

    /// <summary>
    /// Intenta deserializar el state guardado por <see cref="MarkResumePoint{T}"/>.
    /// Lo CONSUME (lo borra después de leer) — sólo se restaura una vez.
    /// </summary>
    public static T? TryConsumeState<T>() where T : class
    {
        var raw = Preferences.Default.Get(StateKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return null;
        Preferences.Default.Remove(StateKey);
        try { return JsonSerializer.Deserialize<T>(raw, JsonOpts); }
        catch { return null; }
    }
}
