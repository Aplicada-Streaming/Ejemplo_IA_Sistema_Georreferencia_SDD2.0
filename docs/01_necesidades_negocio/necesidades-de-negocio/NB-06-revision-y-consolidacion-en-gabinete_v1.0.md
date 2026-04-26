**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-06-revision-y-consolidacion-en-gabinete_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-06 — Revisión y consolidación de la información en gabinete

### Problema específico

La captura en campo es solo el inicio. En gabinete, los jefes de área y relevadores necesitan revisar los relevamientos sincronizados, consolidar la información, agregar contexto que en el momento de la captura no se pudo (fotos sin foco claro, comentarios apurados), agrupar fotos por punto, navegar al mapa, editar campos y descripciones. Si esa fase no es eficiente, todo el trabajo de captura queda represado en un repositorio sin curar y pierde valor analítico.

### Impacto si no se resuelve

- Los datos llegan al backend pero no se usan para análisis ni planificación.
- La curaduría manual de fotos en herramientas externas (Excel + carpetas) se mantiene.
- El jefe de área no puede confiar en lo que ve sin abrir cada foto a mano.
- Pérdida del valor de tener todo georreferenciado: los datos no se navegan desde el mapa.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Tiempo medio para revisar un relevamiento típico (100 puntos) | `[REQUIERE_INFO]` (proceso manual) | ≤ 30 minutos por relevamiento típico | Final del MVP | Cronómetro en validación con jefes de área |
| Disponibilidad de catálogo agrupado por punto y vista plana | N/A | Ambas vistas funcionales | Slice 4 | Validación funcional |
| Permisos de edición respetados según rol y propiedad | N/A | Matriz de permisos validada al 100% | Slice 6 | Test de autorización |
| Conexión catálogo ↔ mapa | N/A | Cada foto enlaza a la ubicación de su punto | Slice 4 | Validación funcional |

### Stakeholders

- **Jefe de área** — usuario primario en revisión.
- **Relevador (dueño)** — revisa y completa sus relevamientos.
- **Colaborador asignado** — edita lo que él mismo cargó.
- **Equipo de gabinete (futuro)** — beneficiario de la consolidación.

### RFs y RNFs cubiertos

RF-29, RF-30, RF-31, RF-32, RF-33.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Depende de:** [NB-01](NB-01-captura-georreferenciada-en-campo_v1.0.md), [NB-02](NB-02-trabajo-offline-y-colaborativo_v1.0.md) (la información debe llegar primero), [NB-03](NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md) (renderizado dinámico), [NB-10](NB-10-gestion-jerarquica-de-usuarios_v1.0.md) (permisos por punto).
- **Habilita:** [NB-07](NB-07-resolucion-colaborativa-de-duplicados_v1.0.md), [NB-09](NB-09-visibilidad-de-actividad-colaborativa_v1.0.md).

---

**Fin del documento — NB-06-revision-y-consolidacion-en-gabinete_v1.0.md**
