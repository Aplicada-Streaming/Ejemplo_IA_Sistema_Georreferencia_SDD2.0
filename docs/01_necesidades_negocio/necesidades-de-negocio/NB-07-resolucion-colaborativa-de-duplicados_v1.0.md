**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-07-resolucion-colaborativa-de-duplicados_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-07 — Resolución colaborativa de puntos duplicados

### Problema específico

Cuando dos colaboradores trabajan offline en el mismo relevamiento (mismo puente, mismo tramo) y crean puntos en lugares físicamente cercanos, no hay forma de saber a priori si están registrando **el mismo defecto** observado desde dos ángulos distintos o **dos defectos genuinamente cercanos** (por ejemplo, dos baches a 5 metros). Una fusión automática perdería información cuando los puntos son distintos. Una no-fusión sin alerta dejaría duplicados invisibles que el equipo de gabinete tendría que descubrir manualmente. Tanto el silencio como la decisión automática son malos resultados.

### Impacto si no se resuelve

- Los puntos duplicados pasan inadvertidos hasta el análisis posterior.
- Se generan estadísticas infladas (más defectos de los que realmente hay).
- Los planes de intervención se basan en datos contaminados.
- El usuario pierde confianza en la información del sistema.
- Inversamente: si se fusionan automáticamente puntos cercanos pero distintos, se borra evidencia del segundo defecto.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Detección de candidatos a fusión | N/A | Detección automática post-sync con threshold geo y temporal configurables | Slice 10 | Test e2e con dos puntos cercanos creados por colaboradores distintos |
| Tasa de candidatos resueltos manualmente vs. ignorados | N/A | ≥ 95% revisados en ≤ 7 días | A partir del piloto | Métrica del panel de conflictos |
| Tiempo mediano hasta resolver un candidato | N/A | ≤ 48h en horario hábil (alineado con [MET-06](../../00_contexto/vision-producto_v1.0.md#4-métricas-de-éxito-smart)) | A partir del piloto | Logs del panel |
| Decisiones de fusión preservan historia | N/A | Evento `PointMerge` con quién, cuándo, valores antes/después | Slice 10 | Inspección del log de eventos |
| Acción "mantener separados" persistente | N/A | El par no se vuelve a proponer | Slice 10 | Test e2e |

### Stakeholders

- **Jefe de área** — toma la decisión de fusión típicamente.
- **Relevador (dueño)** — co-decisor cuando es su relevamiento.
- **Sponsor** — confianza en los datos resultantes.

### RFs y RNFs cubiertos

RF-44, RF-45, RF-46, RF-47, RF-48.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Depende de:** [NB-02](NB-02-trabajo-offline-y-colaborativo_v1.0.md) (la sincronización es donde se detectan los candidatos), [NB-08](NB-08-trazabilidad-tecnica-de-cambios_v1.0.md) (la decisión queda registrada en el log).
- **Habilitada por:** [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md) (el panel vive en la web de revisión).

> Decisión de diseño DD-21 (`PROJECT-BRIEF` Sec. 4): **detección + revisión humana, no fusión automática.**

---

**Fin del documento — NB-07-resolucion-colaborativa-de-duplicados_v1.0.md**
