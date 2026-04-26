**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-02-trabajo-offline-y-colaborativo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-02 — Trabajo offline-first y colaborativo en una misma campaña

### Problema específico

Una misma campaña de relevamiento puede ser asignada a varios relevadores trabajando simultáneamente: por ejemplo, dos relevadores cubriendo un mismo puente desde extremos distintos, o varios relevadores recorriendo el mismo tramo de ruta. Hoy el proceso manual los obliga a consolidar al regreso, con riesgo de duplicaciones, omisiones o conflictos no detectados. Además, cualquier solución que asuma conexión continua falla en el contexto vial real, donde la señal de datos es intermitente o nula durante horas o días enteros. La consolidación tardía de información de varios colaboradores es la fuente principal de pérdida de información en el proceso actual.

### Impacto si no se resuelve

- El equipo no puede paralelizar relevamientos grandes ni reducir el tiempo de campo.
- La información de cada relevador se pierde entre la captura y la consolidación.
- Los conflictos entre datos de distintos colaboradores se descubren tarde y resuelven mal.
- Cualquier corte de conexión se convierte en pérdida de trabajo o reescritura.
- La organización queda topeada en su capacidad de cubrir su red vial con relevamientos digitales.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Capacidad de operar offline en jornada completa | N/A | ≥ 8 horas continuas sin conexión sin pérdida de datos | Permanente desde Slice 1 | Prueba de campo simulada con dos dispositivos físicos |
| Tasa de pérdida de datos en sincronización | N/A | ≤ 0,1% (alineado con [MET-05](../../00_contexto/vision-producto_v1.0.md#4-métricas-de-éxito-smart)) | Permanente desde Slice 1 | Métrica `pending_operations` en estado terminal-error |
| Tiempo desde sync hasta visibilidad de los puntos del otro colaborador | N/A | ≤ 30 segundos en red nominal | Permanente desde Slice 1 | Logs de timestamp de sync |
| Conflictos de edición concurrente resueltos automáticamente | N/A | ≥ 95% sin intervención humana | Permanente desde Fase 4 | Conteo en panel de conflictos |
| Idempotencia de reenvíos | N/A | 0 duplicados después de reenvíos forzados | Permanente desde Slice 1 | Test e2e con corte y reenvío |

### Stakeholders

- **Relevador (dueño)** — depende del offline para trabajar en campo.
- **Colaborador asignado** — trabaja en relevamientos ajenos y necesita ver los puntos del dueño.
- **Jefe de área** — necesita que la información llegue completa y consolidada.
- **Sponsor** — la posibilidad real de cubrir más rutas con menos personas depende de esta NB.

### RFs y RNFs cubiertos

RF-34, RF-35, RF-36, RF-37, RF-38, RF-39, RF-40, RF-41, RF-42, RF-43, RNF-01, RNF-02.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Habilita / es habilitada por:** [NB-01](NB-01-captura-georreferenciada-en-campo_v1.0.md) (la captura usa offline; el offline existe para la captura).
- **Habilita:** [NB-04](NB-04-gestion-ciclo-vida-relevamiento_v1.0.md), [NB-07](NB-07-resolucion-colaborativa-de-duplicados_v1.0.md), [NB-08](NB-08-trazabilidad-tecnica-de-cambios_v1.0.md), [NB-09](NB-09-visibilidad-de-actividad-colaborativa_v1.0.md).
- **Depende de:** [NB-08](NB-08-trazabilidad-tecnica-de-cambios_v1.0.md) (el log de eventos *es* lo que se sincroniza).

> Esta es la NB de mayor riesgo técnico del MVP. Se mitiga con el **spike de sincronización** de Fase 0 (`PROJECT-BRIEF` Sec. 8 / DD-20).

---

**Fin del documento — NB-02-trabajo-offline-y-colaborativo_v1.0.md**
