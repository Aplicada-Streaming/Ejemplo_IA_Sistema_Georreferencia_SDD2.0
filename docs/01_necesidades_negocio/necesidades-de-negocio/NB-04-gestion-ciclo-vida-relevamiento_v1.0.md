**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-04-gestion-ciclo-vida-relevamiento_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-04 — Gestión del ciclo de vida del relevamiento

### Problema específico

Una campaña de relevamiento no es un evento puntual: comienza cuando un relevador la crea, evoluciona a medida que se capturan puntos (en campo y en gabinete), puede asignarse a colaboradores, puede cerrarse cuando la cobertura se considera completa y reabrirse si aparece la necesidad de capturar más puntos. Sin una gestión explícita de este ciclo de vida, los relevamientos se acumulan sin estado claro, no se sabe cuál está activo, cuál está cerrado, quién es el responsable, ni qué etiquetas o filtros usar para encontrarlos. Además, el cliente exige reglas estrictas sobre quién puede eliminar un relevamiento y quién no.

### Impacto si no se resuelve

- Los relevamientos se acumulan sin estado y sin responsable claro.
- Imposible filtrar para análisis o gestión: "muéstrame los relevamientos abiertos del área X de los últimos 30 días" no se puede responder.
- Eliminaciones por error o no autorizadas pueden destruir trabajo de campo.
- Sin reapertura, una captura tardía obliga a crear un nuevo relevamiento desconectado del original.
- Los colaboradores no saben en qué relevamientos están asignados ni qué se espera de ellos.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Estados del relevamiento bien definidos | N/A | ≥ 4 estados (Abierto / Cerrado, plus eliminado lógico) con transiciones reglamentadas | Slice 1 | Diagrama de estados en SA-02 |
| Filtros funcionales en listado | N/A | Filtros por área, estado, fecha y etiquetas operativos | Slice 4 | Validación funcional |
| Eliminaciones por colaboradores rechazadas | N/A | 100% rechazadas | Permanente desde Slice 6 | Test de autorización |
| Reapertura desde móvil disponible | N/A | El dueño puede reabrir y capturar nuevamente | Slice 1 | Validación funcional |
| Etiquetas como mecanismo de búsqueda | N/A | Búsqueda por etiqueta operativa | Slice 4 | Validación funcional |

### Stakeholders

- **Relevador (dueño)** — crea, edita, abre, cierra y elimina sus propios relevamientos.
- **Jefe de área** — gestiona los relevamientos del área.
- **Colaborador asignado** — usa el relevamiento pero no puede eliminarlo.

### RFs y RNFs cubiertos

RF-01, RF-02, RF-03, RF-04, RF-05, RF-06.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Depende de:** [NB-10](NB-10-gestion-jerarquica-de-usuarios_v1.0.md) (los permisos de eliminar/editar dependen del rol y de la propiedad del relevamiento), [NB-03](NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md) (el relevamiento se crea sobre una plantilla).
- **Habilita:** [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md), [NB-09](NB-09-visibilidad-de-actividad-colaborativa_v1.0.md).

---

**Fin del documento — NB-04-gestion-ciclo-vida-relevamiento_v1.0.md**
