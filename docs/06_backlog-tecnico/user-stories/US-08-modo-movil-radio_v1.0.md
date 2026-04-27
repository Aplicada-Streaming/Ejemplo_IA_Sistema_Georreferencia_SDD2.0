**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-08-modo-movil-radio_v1.0.md
**Versión:** 1.1
**Estado:** Aprobado (re-planteo del modelo)
**Fecha:** 2026-04-27
**Autor:** Generado por SA-06 via orquestador

---

# US-08 — Modo recorrido (captura asociada a foto, con radio dinámico)

**Épica:** EP-01.3 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Slice 3

> Como **relevador caminando un tramo (puente, tramo de ruta, drenaje)**,
> quiero **arrancar un "recorrido" e ir tomando fotos cuando vea algo de interés, y que el sistema asocie automáticamente las fotos sucesivas al mismo marcador mientras siga dentro de un radio dinámico, creando uno nuevo recién cuando me alejo**,
> para **capturar rápido sin tener que confirmar "abrir punto" / "cerrar punto" cada vez**.

## CUs y RNs relacionados
- CU: [CU-06](../../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md)
- RN: [RN-09](../../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md) (radio dinámico)

## Re-planteo (v1.1)

La versión 1.0 describía un loop automático que creaba puntos cada N segundos
basado en movimiento. Esto generaba puntos vacíos sin valor semántico. La v1.1
**redefine el modelo**:

- El **disparador** de creación de punto es la **foto del usuario**, NO un tick GPS.
- El sistema sólo **monitorea** posición en background para decidir si la próxima
  foto se asocia al punto activo o crea uno nuevo.
- "Modo recorrido" es la nomenclatura UX. El campo `captureMode` interno sigue
  siendo `"movil"` (string) por compat con datos previos.

## Alcance

**UI** (página `/surveys/{id}/track`):
- Toggle entre **Detenido** (US-07: captura puntual con GPS modal + foto opcional) y
  **Recorrido** (este US).
- En modo recorrido: botón **"Iniciar recorrido"** que arranca el monitoreo GPS
  con `IForegroundTrackingHost` (notif Android persistente, ver DT-bg-tracking).
- Una vez iniciado: botón principal grande **"Tomar foto"** + estado del
  punto activo (lat/lng, cantidad de fotos asociadas, hora de apertura) +
  banner "Saliste del radio" cuando aplica.
- Botón secundario **"Finalizar recorrido"** detiene FG service + monitoreo.

**Lógica:**
- Al **iniciar recorrido**: arranca FG service + loop GPS de monitoreo
  (intervalo de muestreo del template, default 10s). NO crea punto.
- En cada **fix GPS**: actualiza lastFix.
  - Si hay punto activo y `Haversine(lastFix, activePoint) > radio` → libera
    punto activo y muestra banner.
- Al tap **"Tomar foto"**:
  - Captura foto via `MediaPicker`.
  - Si NO hay punto activo → encola `point.created` (lazy creation) y lo
    convierte en activo.
  - Encola `photo.uploaded` asociada al punto activo (current `pointId`).
  - Drainer pushea al backend.
- Al **finalizar**: stop FG service + loop GPS. El punto activo (si existe)
  queda como punto regular en BD; los datos del template se completan luego
  en web.

**Parámetros desde plantilla** (vía `ICaptureProfileResolver` — US-07):
- `gps_timeout_seconds` — timeout por fix.
- `gps_accuracy_threshold_m` — threshold para considerar válido el fix.
- `movil_radius_m` — **radio dinámico**: distancia máxima dentro del cual
  una foto se asocia al punto activo.
- (sampling implícito en cliente, ~10s, se extraerá a parámetro de plantilla
  en un slice posterior).

## Criterios de aceptación

- **CA-8.1** Lazy creation: al iniciar recorrido sin tomar fotos y luego
  finalizar, **no se crea ningún punto** (no hay puntos vacíos en BD).
- **CA-8.2** Dentro del radio: usuario toma 3 fotos sin moverse → las 3 fotos
  quedan asociadas al **mismo** `pointId` (verificado vía `GET /api/v1/points/{id}/photos`).
- **CA-8.3** Salida del radio: con plantilla `movil_radius_m=10`, fotos en
  posiciones A, A+5m, A+15m → primera y segunda comparten punto, tercera crea
  nuevo punto. Banner UI advierte la salida del radio.
- **CA-8.4** Conmutación a "Detenido" durante el recorrido pausa el monitoreo
  pero NO descarta el punto activo. Volver a "Recorrido" lo retoma.
- **CA-8.5** Plantilla con radio distinto al default → la lógica usa el valor
  publicado en la `VersionDePlantilla` activa del relevamiento (resuelto
  via `ICaptureProfileResolver`).
- **CA-8.6** Indicador visual: durante el recorrido se muestra siempre
  (a) lat/lng del fix más reciente, (b) info del punto activo o "esperando
  primera foto", (c) precisión del último fix.
- **CA-8.7** El monitoreo GPS sobrevive a la app pasando a background gracias
  al foreground service (DT-bg-tracking).

## Dependencias

- US-07 (modo detenido y resolver de capture profile).
- US-06 (plantilla raíz, parámetros de captura).
- DT-bg-tracking (foreground service Android).

## DoR — checklist
- [x] Atada a EP-01.3.
- [x] Criterios verificables (con backend + outbox).
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma re-planteo (2026-04-27).

---
**Fin — US-08**
