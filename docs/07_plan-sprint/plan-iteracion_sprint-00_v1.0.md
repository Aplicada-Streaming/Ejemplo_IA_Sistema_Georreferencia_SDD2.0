**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** plan-iteracion_sprint-00_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-07 via orquestador

---

# Sprint 0 — Walking Skeleton + Spike de Sincronización

**Objetivo del sprint:** dejar la columna vertebral del sistema operativa (auth + storage abstraído + esqueleto de sync + scripts `.bat` + un slice trivial end-to-end demostrable) y validar empíricamente el protocolo de sincronización con dos dispositivos físicos antes de comprometer slices funcionales.

**Fechas:** [REQUIERE_INFO] — depende de la fecha de arranque que el sponsor confirme.
**Duración:** 2 semanas (sprint estándar) + 1 semana de spike timeboxed posterior.
**Velocidad comprometida:** [REQUIERE_INFO baseline]. Estimación inicial del equipo, sujeta a calibración real con el primer sprint completado.
**Equipo asignado:** [REQUIERE_INFO] — pendiente confirmación de tamaño y composición con el sponsor.

> **Nota sobre velocidad y duración del sprint:** el intake (`PROJECT-BRIEF` Sec. 8.4) marca como `[REQUIERE_INFO]` la velocidad del equipo, la duración del sprint y la cantidad total. Este plan asume **2 semanas por sprint** y **velocidad inicial de 30-40 SP** (a recalibrar tras Sprint 0). Si el sponsor define otros valores, se actualiza este documento.

---

## 1. US comprometidas

Sprint 0 prioriza el **Walking Skeleton** (EP-00.1). Las US elegidas son las que sostienen toda la cadena posterior y el slice trivial demostrable.

| US | Descripción | Puntos | Owner | Estado |
|---|---|---|---|---|
| [US-01](../06_backlog-tecnico/user-stories/US-01-login-autenticacion-end-to-end_v1.0.md) | Login y autenticación end-to-end | 5 | Backend + Web + Móvil | Comprometida |
| [US-02](../06_backlog-tecnico/user-stories/US-02-esqueleto-relevamiento-persistencia_v1.0.md) | Esqueleto: crear relevamiento + ver en web | 8 | Backend + Web + Móvil | Comprometida |

**Total US:** 13 SP.

---

## 2. Tareas técnicas (BTs) comprometidas

El Sprint 0 carga la mayoría de las BTs por ser la base operativa.

| BT | Descripción | Puntos | Owner |
|---|---|---|---|
| [BT-01](../06_backlog-tecnico/backlog-tecnico_v1.0.md) | Setup de repositorio y solución | 5 | Tech Lead |
| BT-02 | Pipeline CI con tests automatizados | 8 | DevOps |
| BT-03 | Scripts `.bat` de levantamiento local | 5 | DevOps |
| BT-04 | OpenAPI generation y endpoint `/swagger` | 3 | Backend |
| BT-05 | Logging estructurado con Serilog + correlation id | 3 | Backend |
| BT-06 | Migraciones EF Core + tablas base + seeds | 8 | Backend |
| BT-08 | Configuración de SQL Server local | 3 | DevOps |
| BT-09 | Setup MAUI Blazor Hybrid base + SQLite | 8 | Móvil |
| BT-10 | Trigger / política append-only sobre `AuditEvents` | 3 | Backend |
| BT-11 | Setup de testing (xUnit + Moq + Testcontainers) | 5 | Tech Lead |

**Total BTs:** 51 SP.

> El Sprint 0 está intencionalmente cargado de BTs porque sostienen el resto del proyecto. Si la velocidad del equipo no alcanza, recortar BTs no críticos (BT-04 puede diferirse al Slice 1) o extender el Sprint 0 medio sprint.

---

## 3. Spike de sincronización (BT-07)

Posterior al Sprint 0, **timeboxed a 1 semana**. No produce código de producción que se merguea a `main`; produce evidencia validada y un documento de aprendizajes.

**Objetivos:**
- Implementar un prototipo end-to-end de outbox + push + pull diferencial entre dos dispositivos físicos (idealmente Android + Android, alternativa: emulador + dispositivo real).
- Validar idempotencia bajo reintentos, LWW por campo, precedencia del dueño y detección de candidatos a fusión.
- Validar UX del badge de sync y notificación post-sync.

**Salidas:**
- `devs/spikes/sync-spike_v1.0.md` con: protocolo validado, edge cases descubiertos, recomendaciones para US-03/US-04/US-05.
- Eventuales ajustes a [ADR-03](../05_arquitectura_tecnica/adr/ADR-03-sincronizacion-outbox-y-lww-por-campo_v1.0.md).

---

## 4. Criterio de éxito del sprint

- ✅ Un usuario puede loguear en web y móvil con seed users (admin, jefe, relevador).
- ✅ Un relevador puede crear un relevamiento con plantilla raíz desde móvil/web y verlo en el listado web.
- ✅ Los scripts `.bat` levantan los 4+ procesos del sistema en local.
- ✅ La pipeline CI ejecuta tests automatizados y reporta status.
- ✅ OpenAPI documentado en `/swagger` con los endpoints implementados.
- ✅ El log de eventos registra `created` para Surveys y rechaza UPDATE/DELETE.
- ✅ El spike de sync valida el protocolo y deja recomendaciones documentadas.

---

## 5. Demo del Sprint Review

1. Mostrar arranque del sistema con `start-all.bat`.
2. Login de un relevador en móvil y otro en web.
3. Crear un relevamiento desde móvil offline + sincronizar (con conexión simulada).
4. Verlo en la web del jefe de área.
5. Mostrar el evento `created` en la base de datos / endpoint de trazabilidad.
6. Mostrar el spike funcionando con dos dispositivos.

---

## 6. Riesgos identificados

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Equipo sin experiencia previa con MAUI (R-01 del intake) | Media | Alto | Buffer en BT-09; pair programming en setup. |
| Spike de sync revela diseño con problemas | Baja-Media | Alto | Spike es **timeboxed**; si surge cambio fundamental, ajustar ADR-03 antes de Slice 1. |
| Velocidad real menor a la estimada | Media | Medio | Prioridad: Walking Skeleton mínimo (US-01 + US-02 + BT-01 a BT-08). BT no críticos diferibles. |
| Setup de SQL Server local falla en alguna máquina del equipo | Baja | Medio | Documentar instalación detallada en BT-14 (postergable a Slice 8 si urgencia). |

---

## 7. Dependencias

- **Upstream:** ninguna (es el primer sprint).
- **Downstream:** Sprint 1 (Slice 1) **depende** de Sprint 0 + spike completados, especialmente BT-07.

---

## 8. Pendientes del DoR a resolver

- [ ] Confirmar duración del sprint (2 semanas asumido).
- [ ] Confirmar tamaño y composición del equipo.
- [ ] Confirmar fecha de arranque del proyecto.
- [ ] Confirmar herramienta de gestión del backlog del [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md).

---

**Fin del documento — plan-iteracion_sprint-00_v1.0.md**
