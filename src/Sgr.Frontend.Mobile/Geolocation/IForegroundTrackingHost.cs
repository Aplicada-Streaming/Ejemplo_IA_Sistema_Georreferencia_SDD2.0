namespace Sgr.Frontend.Mobile.Geolocation;

/// <summary>
/// Host de "foreground service" del SO para que el tracking continuo (modo móvil)
/// sobreviva cuando el usuario sale de la app, bloquea pantalla, o el SO decide
/// throttear procesos en background (DT-bg-tracking).
///
/// Implementaciones:
///   • Android — <c>AndroidForegroundTrackingHost</c> (Platforms/Android/Tracking)
///     levanta un <c>Android.App.Service</c> con notificación persistente y
///     <c>ForegroundServiceType.Location</c>. Mientras la notif esté visible,
///     Android no mata el proceso ni le bloquea acceso a GPS.
///   • Windows / iOS / desktop — <see cref="NoopForegroundTrackingHost"/>
///     no hace nada porque esas plataformas no requieren este "permiso".
///
/// El tracker llama Start cuando arranca el loop y Stop cuando lo detiene.
/// Si el host no puede arrancar (permisos denegados, etc) NO debe tirar excepción
/// que rompa el tracker — registra warning y sigue. El loop seguirá funcionando
/// mientras la app esté en foreground; al pasar a background el SO eventualmente
/// throttea, lo cual es el comportamiento pre-fix.
/// </summary>
public interface IForegroundTrackingHost
{
    void Start(string title, string description);
    void Stop();
}

/// <summary>
/// Implementación no-op para plataformas donde no aplica el concepto de
/// foreground service (Windows desktop, iOS — iOS tiene su propio modelo
/// de background location no implementado todavía, ver DT-ios-bg-location).
/// </summary>
public sealed class NoopForegroundTrackingHost : IForegroundTrackingHost
{
    public void Start(string title, string description) { }
    public void Stop() { }
}
