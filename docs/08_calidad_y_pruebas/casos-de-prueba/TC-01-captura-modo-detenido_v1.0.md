**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** TC-01-captura-modo-detenido_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# TC-01 — Captura modo detenido con foto y GPS

**ID:** TC-01
**CU relacionado:** [CU-06](../../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md)
**Tipo:** E2E manual + automatizado parcial
**Prioridad:** Crítica

## Precondiciones

- App móvil instalada en dispositivo físico Android o iOS.
- Usuario `relevador.test@vialidad` en estado `activo`.
- Relevamiento abierto "Test Sprint 2" sobre plantilla raíz publicada.
- Permisos cámara y ubicación otorgados.
- GPS activo con buena recepción.
- Modo `detenido` seleccionado.

## Datos de entrada

- Marcador actual: ninguno (la captura debe crear nuevo Punto).
- Toma de foto del entorno (cualquier imagen).

## Pasos

1. Abrir el relevamiento "Test Sprint 2".
2. Confirmar que el modo activo es `detenido`.
3. Pulsar el botón de cámara.
4. Esperar el diálogo S2 obteniendo GPS.
5. Esperar transición a S3-OK y apertura de cámara nativa.
6. Tomar una foto del entorno.
7. Confirmar la foto en el flujo de cámara.
8. Volver al mapa.

## Resultado esperado

- Diálogo S0 → S2 → S3-OK transcurrió en < 30 segundos.
- Cámara nativa abierta y foto capturada.
- En el mapa aparece un marcador nuevo en la posición GPS actual (precisión ≤ 50m).
- La foto está asociada al marcador.
- En el panel de estado de sync hay 1 operación pendiente (creación del Punto + Foto).
- Tras sync con conexión, el Punto aparece en la web del jefe de área.

## Resultado obtenido

(Se completa al ejecutar.)

## Estado

Pendiente.

## Notas y observaciones

- Si el dispositivo está en una zona con mala recepción GPS, validar también E4 (S3-LOWACC) y E5 (S3-TIMEOUT) en pruebas siguientes.
- TC complementario para asociar foto a marcador existente en TC-aux derivado.

---

**Fin del documento — TC-01-captura-modo-detenido_v1.0.md**
