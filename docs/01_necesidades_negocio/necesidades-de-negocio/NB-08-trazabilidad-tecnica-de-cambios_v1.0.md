**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-08-trazabilidad-tecnica-de-cambios_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-08 — Trazabilidad técnica de cambios sobre los datos

### Problema específico

En un sistema con múltiples colaboradores, sincronización offline-first y resolución automática de conflictos, surge inevitablemente la pregunta "¿quién cambió esto, cuándo, desde dónde, y por qué tiene este valor y no otro?" Sin un mecanismo claro para responderla, se vuelve imposible: (a) resolver disputas entre colaboradores; (b) auditar cómo evolucionó un relevamiento; (c) recuperar una versión anterior cuando una sobrescritura automática no fue la deseada; (d) sostener la propia sincronización, que se basa en eventos timestampados. **La trazabilidad acá no es un requisito de compliance regulatorio (no fue planteado por el cliente) sino una funcionalidad técnica derivada del modelo de sync.**

### Impacto si no se resuelve

- La sincronización no funciona (no hay base de eventos que aplicar).
- Los conflictos automáticos no se pueden revertir por falta de histórico.
- Las disputas entre colaboradores no se pueden zanjar con datos.
- El equipo no puede diagnosticar comportamientos extraños del sistema en producción.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Cobertura del log de eventos | N/A | 100% de cambios sobre puntos, fotos y relevamientos quedan registrados | Permanente desde Slice 1 | Test e2e con N cambios y conteo en log |
| Datos por evento | N/A | Quién, cuándo, qué campo, valor anterior, valor nuevo, origen, device_id | Permanente desde Slice 1 | Inspección del modelo |
| Consulta de origen por punto disponible | N/A | UI muestra metadata + histórico de cambios | Slice 4 / Slice 6 | Validación funcional |
| Inmutabilidad de eventos | N/A | Los eventos no se editan ni se borran (append-only) | Permanente desde Slice 1 | Test de no-edición |
| Rendimiento de consultas históricas | N/A | ≤ 500 ms por consulta de histórico de un punto típico | Final del MVP | Benchmark |

### Stakeholders

- **Jefe de área** — consulta el origen y el histórico para entender los datos.
- **Equipo técnico** — debug, soporte, recuperación de información.
- **Relevador (dueño)** — necesita ver quién editó sus puntos.

### RFs y RNFs cubiertos

RF-49, RF-50, RF-51, RNF-03.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Habilita / es habilitada por:** [NB-02](NB-02-trabajo-offline-y-colaborativo_v1.0.md) (el log de eventos *es* lo que se sincroniza; relación bidireccional).
- **Habilita:** [NB-07](NB-07-resolucion-colaborativa-de-duplicados_v1.0.md) (las decisiones de fusión quedan registradas), [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md) (la consulta de histórico vive en la revisión).

> Si en una fase posterior se requiere etapa formal de aprobación / auditoría regulatoria (`[REQUIERE_INFO]` en intake), esta NB es la base sobre la que se construye sin reescritura.

---

**Fin del documento — NB-08-trazabilidad-tecnica-de-cambios_v1.0.md**
