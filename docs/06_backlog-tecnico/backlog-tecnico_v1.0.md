**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** backlog-tecnico_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# Backlog Técnico

Tareas técnicas que **no son User Stories** pero son necesarias para que el sistema funcione: setup, infraestructura local, deuda técnica, instrumentación, spikes. Tienen su propio identificador `BT-XX` y se planifican entre las US del sprint.

| ID | Descripción | Justificación | Estimación (SP) | Sprint sugerido | Dependencia |
|---|---|---|---|---|---|
| BT-01 | Setup del repositorio y estructura de solución (.NET solution, proyectos por módulo, branching) | Base para todo el desarrollo | 5 | Sprint 0 | — |
| BT-02 | Pipeline CI con tests automatizados (build, unit, integration, lint) | Garantiza el DoD del [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) | 8 | Sprint 0 | BT-01 |
| BT-03 | Scripts `.bat` para levantamiento local (start-db, start-backend, start-web, start-worker, start-mobile) | RNF-06 obligatorio | 5 | Sprint 0 | BT-01, BT-08 |
| BT-04 | OpenAPI generation y endpoint `/swagger`; export del JSON a `/docs` automatizado | DD-16 + RNF-05 (portabilidad) | 3 | Sprint 0 | BT-01 |
| BT-05 | Logging estructurado con Serilog + correlation id | Observabilidad mínima desde Sprint 0 | 3 | Sprint 0 | BT-01 |
| BT-06 | Migraciones EF Core + tablas base + seeds (admin raíz, área default, plantilla raíz) | Habilita US-01, US-02, US-06 | 8 | Sprint 0 | BT-01, BT-08 |
| BT-07 | Spike de sincronización de 1 semana con dos dispositivos físicos (DD-20) | Mitigación del riesgo dominante antes de comprometer slices | 13 (timeboxed) | Spike Fase 0 | BT-01, BT-09 |
| BT-08 | Configuración de SQL Server local + scripts de creación de DB | Necesario para todo el resto | 3 | Sprint 0 | — |
| BT-09 | Setup MAUI Blazor Hybrid base + SQLite + Entity Framework | Habilita el frente móvil | 8 | Sprint 0 | BT-01 |
| BT-10 | Trigger / política append-only sobre `AuditEvents` (rechaza UPDATE/DELETE) | Implementa [RN-10](../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md) | 3 | Sprint 0 | BT-06 |
| BT-11 | Setup de testing: xUnit + FluentAssertions + Moq + Testcontainers para integración | Cumple DoD de tests | 5 | Sprint 0 | BT-02 |
| BT-12 | Cifrado de credenciales en `SystemConfig` (DPAPI / proveedor pluggable) | Seguridad básica para US-17/US-18 | 3 | Slice 8 | BT-06 |
| BT-13 | Telemetría básica: contadores en `/metrics` Prometheus textfile | Operación productiva (`[REQUIERE_INFO]` ambiente) | 5 | Slice 9 (ajustable) | BT-05 |
| BT-14 | Documentación de runbooks operativos (start, stop, troubleshooting básico) | Onboarding y operación | 3 | Slice 8 | BT-03 |

---

## 1. Distribución por sprint

| Sprint | Tareas técnicas |
|---|---|
| Sprint 0 (Walking Skeleton) | BT-01, BT-02, BT-03, BT-04, BT-05, BT-06, BT-08, BT-09, BT-10, BT-11 |
| Spike Fase 0 | BT-07 |
| Slice 8 | BT-12, BT-14 |
| Slice 9 | BT-13 |

> El sprint 0 es denso en BTs porque sostiene el resto del proyecto. SA-07 fija el alcance final del sprint considerando capacidad real del equipo.

---

## 2. Tareas técnicas excluidas explícitamente del MVP

Por consistencia con [alcance-proyecto](../00_contexto/alcance-proyecto_v1.0.md):

| Excluido | Motivo | Disparador para reabrir |
|---|---|---|
| Migración masiva entre adaptadores de storage | EX-02 | Cliente prioriza consolidar storage histórico |
| Pipeline CI/CD productivo (deploy a ambientes superiores) | `[REQUIERE_INFO]` ambientes | Cliente define ambientes |
| Cifrado en reposo a nivel aplicación de DB y storage | EX-14 | Política de seguridad explícita |
| Backup/restore automatizado | EX-15 | Política formal de respaldo |

---

## 3. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md) | Componentes que requieren BTs (logging, OpenAPI, scripts) |
| [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) | DoR/DoD que justifican BT-02, BT-11 |
| `devs/intake/PROJECT-BRIEF.md` Sec. 8 | DD-19 walking skeleton, DD-20 spike de sync |

---

**Fin del documento — backlog-tecnico_v1.0.md**
