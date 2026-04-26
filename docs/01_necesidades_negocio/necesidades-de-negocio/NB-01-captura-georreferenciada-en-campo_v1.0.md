**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-01-captura-georreferenciada-en-campo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-01 — Captura georreferenciada eficiente en campo con conectividad limitada

### Problema específico

Los relevadores de Vialidad recorren rutas y puentes para inspeccionar el estado de la infraestructura, pero el proceso actual de captura es manual y desestructurado: notas en papel, fotos en cámaras independientes sin metadata geoespacial sistemática, descripciones textuales del lugar ("kilómetro 47, lado derecho"). En campo además enfrentan conectividad intermitente o nula, GPS de calidad variable, restricciones de permisos del dispositivo, y la necesidad de capturar muchas observaciones cercanas en pocos minutos sin perder tiempo en cada toma.

### Impacto si no se resuelve

- Las observaciones quedan desconectadas del lugar exacto donde se hicieron.
- El equipo de gabinete reconcilia manualmente fotos con notas, con costo de tiempo y errores.
- El relevador en campo dedica tiempo a tareas administrativas en vez de a inspeccionar.
- El proceso no escala a campañas grandes ni a varios relevadores.
- La calidad y cantidad de inspecciones que la organización puede hacer queda topeada por el costo del proceso manual.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Tiempo medio para capturar un punto con foto y datos básicos | `[REQUIERE_INFO]` (proceso manual) | ≤ 15 segundos por punto en modo fluido | Final del MVP | Cronómetro en sesiones de validación con relevadores reales |
| Porcentaje de fotos con coordenadas válidas asociadas al punto correcto | `[REQUIERE_INFO]` (estimado bajo, depende de cámara con GPS) | ≥ 99% | Permanente desde Slice 2 | Auditoría sobre tabla `points` con coordenadas no nulas |
| Tasa de capturas perdidas por fallo de hardware o permisos | N/A | ≤ 0,5% sobre intentos de captura | Permanente desde Slice 2 | Logs de la máquina de estados de captura |
| Aceptación de la app por relevadores en validación de campo | N/A | ≥ 80% considera la app más rápida que el método actual | Final del MVP | Encuesta a relevadores que probaron en piloto |

### Stakeholders

- **Relevador (dueño)** — usuario primario; captura sus propios puntos.
- **Colaborador asignado** — usuario primario; captura puntos en relevamientos ajenos.
- **Jefe de área** — beneficiario indirecto; recibe los datos consolidados antes que con el método manual.
- **Sponsor del proyecto** — beneficiario estratégico; ve mejorada la productividad del organismo.

### RFs y RNFs cubiertos

RF-14, RF-15, RF-16, RF-17, RF-18, RF-19, RF-20, RF-21, RF-22, RF-23, RNF-01.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Depende de:** [NB-02](NB-02-trabajo-offline-y-colaborativo_v1.0.md) (offline-first es la base técnica de toda la captura), [NB-03](NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md) (los campos a capturar dependen de la plantilla), [NB-11](NB-11-portabilidad-storage-y-config-inicial_v1.0.md) (las fotos se guardan en el storage configurado).
- **Habilita:** [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md), [NB-08](NB-08-trazabilidad-tecnica-de-cambios_v1.0.md), [NB-09](NB-09-visibilidad-de-actividad-colaborativa_v1.0.md).

---

**Fin del documento — NB-01-captura-georreferenciada-en-campo_v1.0.md**
