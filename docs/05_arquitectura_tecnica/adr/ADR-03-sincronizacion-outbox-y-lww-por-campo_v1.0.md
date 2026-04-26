**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** ADR-03-sincronizacion-outbox-y-lww-por-campo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-05 via orquestador

---

# ADR-03 — Sincronización con outbox + LWW por campo + detección de candidatos a fusión

**Estado:** Aceptado.

## Contexto

La sincronización multi-colaborador es la complejidad central del sistema y es lo que entrega la propuesta de valor diferencial. Múltiples relevadores trabajan offline en el mismo relevamiento y deben converger en un estado consistente cuando aparece conexión, sin perder información ni introducir duplicaciones. El intake (`PROJECT-BRIEF` Sec. 5) describe el catálogo completo de conflictos posibles (C-01 a C-09) y delinea el enfoque: GUIDs en cliente + outbox + pull diferencial + LWW por campo + detección de candidatos a fusión sin auto-fusión.

Las decisiones del intake afines son DD-08 a DD-13 y DD-21. La regla [RN-09](../../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md) formaliza la detección.

## Decisión

### Componentes

1. **GUIDs generados en cliente** para Surveys, Points, Photos, AuditEvents. El backend acepta el GUID como ID definitivo.
2. **Timestamp de origen** en cada evento: cuando ocurrió en el dispositivo, no cuando llegó al backend. Es el criterio de orden para LWW.
3. **Outbox local** en el móvil (`PendingOperations`): drena en background con reintentos exponenciales (5s, 15s, 1m, 5m, 15m, etc.).
4. **Push** vía `POST /api/v1/sync/push` con lista de eventos.
5. **Pull diferencial** vía `GET /api/v1/surveys/{id}/changes?since={timestamp}` que devuelve eventos posteriores al último pull.
6. **Resolución LWW por campo** ([RN-07](../../02_especificacion_funcional/reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md)): si dos eventos compiten por el mismo campo, gana el de timestamp original mayor; los demás campos no se ven afectados.
7. **Precedencia del dueño**: en conflictos entre dueño y colaborador, gana el dueño incondicionalmente, aunque su timestamp sea anterior.
8. **Casos especiales** que NO se resuelven automáticamente:
   - Capturas post-cierre del relevamiento ([RN-08](../../02_especificacion_funcional/reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md)) → estado `rechazado` + panel.
   - Eliminaciones con actividad posterior → resucita el punto si la actividad posterior es de edición.
   - Candidatos a fusión ([RN-09](../../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md)) → marca, no fusión automática; revisión manual.
9. **Notificación post-sync** al cliente con el resumen de cambios y conflictos.
10. **Trazabilidad técnica via AuditEvents append-only** ([RN-10](../../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md)): los eventos *son* lo que se sincroniza y *son* la auditoría técnica.

### Idempotencia

La clave `(event_id, timestamp_original, event_type)` garantiza que reenvíos no producen efectos secundarios duplicados. Los reintentos del outbox son seguros.

### Detección de candidatos a fusión

El Worker de Sync, al aplicar un evento `point.created.v1` o `point.coords.updated.v1`, ejecuta una consulta espacial sobre `Points` del mismo `survey_id` con un buffer geodésico de `merge_radius_m` (default 10m, configurable por plantilla). Filtra por:

- `created_by` distinto entre los dos puntos.
- Diferencia de timestamps ≤ `merge_time_window` (default 24h).
- Par no marcado previamente como `mantenido_separado`.

Inserta un `MergeCandidate` en estado `pendiente`.

## Consecuencias positivas

- **Determinismo de la resolución automática.** LWW por campo + timestamp original es predecible y replicable.
- **Idempotencia robusta.** Reintentos no producen estados inconsistentes.
- **Trazabilidad técnica completa.** El log de eventos sostiene la sync, la consulta histórica y, eventualmente, una auditoría regulatoria si el cliente la prioriza más adelante.
- **Detección de duplicados sin pérdida silenciosa.** Los candidatos a fusión van al panel; ningún punto se pierde por una decisión automática.
- **Operación offline plena.** El móvil opera con su DB local + outbox; sincroniza cuando puede.

## Consecuencias negativas

- **El backend depende de la confiabilidad del timestamp del cliente.** Un dispositivo con reloj alterado puede afectar resoluciones LWW. Mitigación: detectar drift mayor a un umbral y rechazar (alertar al usuario).
- **Crecimiento del log de eventos.** Cada cambio genera un registro append-only. Para volúmenes grandes, requerirá políticas de archivado en una fase posterior (alcance EX-06).
- **Detección espacial cuesta** en relevamientos con muchos puntos. Mitigación: índice espacial (`SPATIAL_IX_Points_coords`) y filtros previos por `survey_id` y rango temporal.
- **Resolución manual no es bulk-friendly.** Si aparecen muchos conflictos a la vez, el panel obliga a resolver uno por uno. Aceptable para MVP; el threshold de fusión y el mecanismo de "mantener separados" persistente reduce reincidencias.

## Alternativas consideradas

1. **CRDTs (Conflict-free Replicated Data Types).** Descartada por sobre-ingeniería para el caso: el modelo es simple (campos planos por punto) y los conflictos esperados son acotados.
2. **Locking pesimista en backend** (un colaborador a la vez por punto). Descartada por incompatibilidad con offline.
3. **Auto-fusión por proximidad.** Descartada (DD-21) por riesgo de pérdida silenciosa de información cuando los puntos cercanos son defectos genuinamente distintos.
4. **LWW a nivel de entidad completa** en lugar de por campo. Descartada porque pisa cambios independientes de otros campos sin razón.
5. **Solo push del cliente al servidor** (sin pull). Descartada (DD-10) porque no soporta multi-colaborador.

## Riesgos y mitigación

- **R-Sync.1**: Drift de reloj del cliente → detección al sync push y rechazo si supera umbral configurable; alerta al usuario para corregir.
- **R-Sync.2**: Sincronización parcial deja la outbox en estado intermedio → reintentos por evento (idempotente por GUID), sync resumible.
- **R-Sync.3**: Volumen de candidatos a fusión genera ruido al jefe → threshold conservador + opción "mantener separados" persistente.

## Validación previa

Antes de comprometer slices funcionales, se ejecuta el **spike de sincronización de 1 semana** definido en `PROJECT-BRIEF` Sec. 8 / DD-20 con dos dispositivos físicos. El spike debe validar:

- Idempotencia bajo reintentos.
- LWW correcta con timestamps.
- Precedencia del dueño en escenario realista.
- Detección de candidatos a fusión.
- Notificaciones post-sync.

## Trazabilidad

- DD-08, DD-09, DD-10, DD-11, DD-12, DD-13, DD-20, DD-21 (`PROJECT-BRIEF` Sec. 4).
- `PROJECT-BRIEF` Sec. 5 (catálogo de conflictos C-01 a C-09).
- RFs RF-34 a RF-48 (`PROJECT-README` Sec. 5.6, 5.7).
- [NB-02](../../01_necesidades_negocio/necesidades-de-negocio/NB-02-trabajo-offline-y-colaborativo_v1.0.md), [NB-07](../../01_necesidades_negocio/necesidades-de-negocio/NB-07-resolucion-colaborativa-de-duplicados_v1.0.md), [NB-08](../../01_necesidades_negocio/necesidades-de-negocio/NB-08-trazabilidad-tecnica-de-cambios_v1.0.md).
- [RN-06](../../02_especificacion_funcional/reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md), [RN-07](../../02_especificacion_funcional/reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md), [RN-08](../../02_especificacion_funcional/reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md), [RN-09](../../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md), [RN-10](../../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md).

---

**Fin del documento — ADR-03-sincronizacion-outbox-y-lww-por-campo_v1.0.md**
