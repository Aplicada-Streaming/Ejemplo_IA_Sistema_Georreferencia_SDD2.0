**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-06-capturar-punto-georreferenciado_v1.0.md
**Versión:** 1.1
**Estado:** Aprobado (re-planteo de modo móvil → recorrido)
**Fecha:** 2026-04-27
**Autor:** Generado por SA-02 via orquestador

---

# CU-06 — Capturar punto georreferenciado (modo detenido y modo recorrido)

> **Nota terminológica (v1.1):** "Modo recorrido" es la nomenclatura UX visible al
> usuario. El campo `captureMode` interno conserva el string `"movil"` por compat
> con datos previos. El flujo es disparado por la **foto del usuario** (no por
> un timer GPS), tal como ya se describía en este documento — la v1.0 del código
> implementaba un loop automático sin fotos que se reemplaza en US-08 v1.1.

**Código:** CU-06
**Actor primario:** Relevador o Colaborador asignado
**Frente:** Móvil

## Precondiciones

- El usuario está autenticado con rol `relevador` y estado `activo`.
- El usuario tiene un Relevamiento abierto seleccionado del que es dueño o colaborador asignado.
- La app móvil tiene la versión de plantilla del relevamiento descargada.
- El dispositivo tiene acceso a cámara y ubicación (gestión de permisos en flujo).

## Postcondiciones

- Se crea un Punto con coordenadas, una Foto asociada con su comentario inicial, y los ValorDeCampo iniciales según la plantilla.
- Se generan eventos de auditoría `created` para Punto y para Foto.
- El estado se guarda en DB local del móvil y en la outbox.

## Flujo principal — Modo detenido

1. El usuario tiene seleccionado el modo de captura `detenido` y un marcador actual (recién creado o seleccionado en mapa).
2. El usuario presiona el botón de cámara.
3. El sistema invoca el **diálogo unificado de captura** ([referencia: PROJECT-BRIEF Sec. 7](../../../devs/intake/PROJECT-BRIEF.md)):
   - S0: verifica permisos cámara + ubicación.
   - S2: obtiene fix de GPS con timeout configurable (parámetro de plantilla).
   - S3-OK: precisión aceptable.
4. El sistema abre la cámara nativa.
5. El usuario toma la foto.
6. El sistema genera GUIDs locales para Punto (si es nuevo) y Foto.
7. Si **había marcador seleccionado**: la foto se asocia al marcador actual sin crear un nuevo Punto.
8. Si **no había marcador seleccionado**: se crea un nuevo Punto en la ubicación obtenida + la foto.
9. El sistema procesa la foto localmente (normalización, thumb) según parámetros de la plantilla.
10. El sistema persiste eventos en outbox local y refresca el mapa.

## Flujo alternativo — Modo recorrido con radio configurable

> UX label: "Recorrido". `captureMode` interno: `"movil"` (string sin cambios).

1a. El usuario inicia la sesión de "Recorrido" en la pantalla `/surveys/{id}/track`.
    El sistema arranca el monitoreo GPS en background con foreground service
    (notif persistente — DT-bg-tracking) pero **NO crea ningún punto**.
2a. El loop GPS actualiza la última posición conocida cada N segundos
    (intervalo de muestreo del template, default 10s).
3a. Al sacar foto, el sistema usa la última posición conocida (o la pide fresh).
4a. Si **NO hay punto activo**: se crea un nuevo Punto en la posición actual
    (lazy creation — sin fotos no hay puntos vacíos) y la foto se asocia. Ese
    Punto pasa a ser el "punto activo".
5a. Si **HAY punto activo** y la posición está **dentro del radio** del Punto
    activo, la foto se asocia al mismo Punto.
6a. Si la posición está **fuera del radio** (detectado por el loop antes del
    siguiente disparo de foto), el Punto activo se libera. La próxima foto
    crea un nuevo Punto activo.
7a. Al **finalizar** la sesión, el Punto activo (si existe) queda como Punto
    regular en BD. Los datos del template (campos custom) se completan luego
    en web post-captura.

## Flujo alternativo — Reubicar marcador antes de tomar foto

1b. El usuario tiene un Punto creado y lo arrastra en el mapa antes de tomar la foto siguiente.
2b. El sistema actualiza las coordenadas del Punto y emite un evento `field_updated` para el campo `coordenadas`.
3b. La próxima foto se asocia al marcador en su nueva posición.

## Flujos de error (estados S1 y S3 del diálogo)

- E1. **S1-CAM-DENY**: permiso de cámara denegado → diálogo "Ir a configuración" / Cancelar.
- E2. **S1-LOC-DENY**: permiso de ubicación denegado → diálogo equivalente.
- E3. **S1-BOTH-DENY**: ambos denegados → mensaje combinado.
- E4. **S3-LOWACC**: precisión menor que el threshold → diálogo "Reintentar / Continuar igual / Cancelar". El botón "Continuar igual" puede estar deshabilitado según parámetro `allow_continue_with_low_accuracy` de la plantilla.
- E5. **S3-TIMEOUT**: sin fix tras el timeout → "Reintentar / Cancelar". Si la plantilla habilita ingreso manual, aparece la opción "Ingresar manualmente".
- E6. **S3-NOSIGNAL**: GPS desactivado en el dispositivo → "Abrir ajustes de ubicación / Cancelar".
- E7. Almacenamiento local lleno → mensaje y bloqueo de captura.

## Reglas de negocio relacionadas

- [RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md) — Permisos por punto (gobierna quién puede crear y editar).
- [RN-06](../reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md) — GUIDs en cliente.
- [RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md) — No se permite capturar en relevamientos cerrados; si el cierre llegó por sync mientras se capturaba offline, las capturas se marcan como rechazadas al sincronizar.
- [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md) — Eventos append-only.

## Trazabilidad

- Origen: [NB-01](../../01_necesidades_negocio/necesidades-de-negocio/NB-01-captura-georreferenciada-en-campo_v1.0.md).
- RFs cubiertos: RF-14, RF-15, RF-16, RF-17, RF-18, RF-20, RF-21, RF-22, RF-23.

## Criterios de aceptación

- **CA-06.1** — *Given* modo `detenido` con marcador seleccionado, *when* el usuario toma una foto, *then* la foto se asocia al marcador actual sin crear un nuevo Punto.
- **CA-06.2** — *Given* modo recorrido (`captureMode="movil"`) con radio = 10m
  y un punto activo en (0,0), *when* el usuario toma foto a 5m del punto,
  *then* la foto se asocia al mismo Punto. *When* el monitoreo GPS detecta
  posición a 15m (fuera del radio), libera el punto activo y la **siguiente
  foto** crea un nuevo Punto.
- **CA-06.2.bis** — *Given* recorrido recién iniciado sin fotos tomadas,
  *when* el usuario finaliza la sesión, *then* **no se crea ningún Punto**
  (lazy creation, no hay puntos vacíos).
- **CA-06.3** — *Given* permiso de ubicación denegado, *when* el usuario presiona el botón de cámara, *then* aparece S1-LOC-DENY con opción a configuración.
- **CA-06.4** — *Given* GPS con precisión 80m y threshold 50m, *when* el sistema obtiene el fix, *then* aparece S3-LOWACC.
- **CA-06.5** — *Given* relevamiento creado en plantilla con `gps_timeout_seconds=30`, *when* no hay fix tras 30s, *then* aparece S3-TIMEOUT.
- **CA-06.6** — *Given* el usuario es colaborador asignado, *when* crea un punto nuevo, *then* el Punto queda con `creado_por` = colaborador y se aplican las reglas de [RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md) en ediciones futuras.

---

**Fin del documento — CU-06-capturar-punto-georreferenciado_v1.0.md**
