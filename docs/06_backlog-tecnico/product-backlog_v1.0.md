**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** product-backlog_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# Product Backlog

Backlog de User Stories priorizadas por MoSCoW, alineadas con el [roadmap](../00_contexto/roadmap-producto_v1.0.md) y los [casos de uso](../02_especificacion_funcional/especificacion-funcional_v1.0.md). Las estimaciones en story points son **iniciales del equipo** y serán refinadas en la primera sesión de refinamiento.

---

## 1. Resumen

| Métrica | Valor |
|---|---|
| Total US | 22 |
| Must Have | 12 (54,5%) |
| Should Have | 7 (31,8%) |
| Could Have | 3 (13,6%) |
| Total story points estimados | 211 SP |

> Cumple regla "Must Have ≤ 60%".

---

## 2. Listado priorizado de User Stories

Las US están agrupadas por épica del roadmap. La columna **Sprint sugerido** es el orden indicativo; la asignación definitiva la fija SA-07 cuando conozca velocidad y duración del sprint.

### Fase 0 — Cimientos

| US | Nombre | Épica | MoSCoW | SP | Sprint |
|---|---|---|---|---|---|
| [US-01](user-stories/US-01-login-autenticacion-end-to-end_v1.0.md) | Login y autenticación end-to-end | EP-00.1 | Must | 5 | Sprint 0 |
| [US-02](user-stories/US-02-esqueleto-relevamiento-persistencia_v1.0.md) | Esqueleto: crear relevamiento + ver en web | EP-00.1 | Must | 8 | Sprint 0 |

### Fase 1 — Núcleo Multi-Colaborador

| US | Nombre | Épica | MoSCoW | SP | Sprint |
|---|---|---|---|---|---|
| [US-03](user-stories/US-03-outbox-local-reintentos_v1.0.md) | Outbox local móvil con reintentos exponenciales | EP-01.1 | Must | 13 | Slice 1 |
| [US-04](user-stories/US-04-push-eventos-idempotencia_v1.0.md) | Push de eventos al backend con idempotencia | EP-01.1 | Must | 13 | Slice 1 |
| [US-05](user-stories/US-05-pull-diferencial_v1.0.md) | Pull diferencial de eventos al móvil | EP-01.1 | Must | 8 | Slice 1 |
| [US-06](user-stories/US-06-plantilla-raiz_v1.0.md) | Plantilla raíz con campos comunes y parámetros de captura | EP-01.2 | Must | 8 | Slice 2 |
| [US-07](user-stories/US-07-captura-modo-detenido_v1.0.md) | Captura modo detenido con diálogo unificado | EP-01.2 | Must | 13 | Slice 2 |
| [US-08](user-stories/US-08-modo-movil-radio_v1.0.md) | Modo móvil con radio configurable | EP-01.3 | Must | 8 | Slice 3 |

### Fase 2 — Gestión y Plantillas

| US | Nombre | Épica | MoSCoW | SP | Sprint |
|---|---|---|---|---|---|
| [US-09](user-stories/US-09-catalogo-fotos-web_v1.0.md) | Catálogo de fotos por punto y vista plana en web | EP-02.1 | Must | 13 | Slice 4 |
| [US-10](user-stories/US-10-mapa-colaborativo_v1.0.md) | Mapa colaborativo con diferenciación por colaborador | EP-02.1 | Should | 8 | Slice 4 |
| [US-11](user-stories/US-11-plantillas-puente-pavimento_v1.0.md) | Plantillas hijas: inspección de puente y pavimento | EP-02.2 | Must | 13 | Slice 5 |
| [US-12](user-stories/US-12-renderizado-dinamico_v1.0.md) | Renderizado dinámico de campos en web y móvil | EP-02.2 | Should | 8 | Slice 5 |
| [US-13](user-stories/US-13-aceptacion-jerarquica-usuarios_v1.0.md) | Aceptación jerárquica admin → jefe → relevador | EP-02.3 | Must | 8 | Slice 6 |
| [US-14](user-stories/US-14-permisos-por-punto_v1.0.md) | Permisos por punto: dueño edita todo, colaborador lo suyo | EP-02.3 | Must | 8 | Slice 6 |

### Fase 3 — Capacidades Operativas

| US | Nombre | Épica | MoSCoW | SP | Sprint |
|---|---|---|---|---|---|
| [US-15](user-stories/US-15-carga-lote-exif_v1.0.md) | Subida en lote desde web con extracción EXIF | EP-03.1 | Should | 13 | Slice 7 |
| [US-16](user-stories/US-16-cola-fotos-pendientes-geo_v1.0.md) | Cola de fotos pendientes de georreferenciar manualmente | EP-03.1 | Should | 8 | Slice 7 |
| [US-17](user-stories/US-17-wizard-storage_v1.0.md) | Wizard de primer arranque para configurar storage | EP-03.2 | Must | 8 | Slice 8 |
| [US-18](user-stories/US-18-adaptadores-storage-s3-ftp-sftp_v1.0.md) | Adaptadores S3, FTP y SFTP funcionales | EP-03.2 | Should | 13 | Slice 8 |

### Fase 4 — Resolución de Conflictos

| US | Nombre | Épica | MoSCoW | SP | Sprint |
|---|---|---|---|---|---|
| [US-19](user-stories/US-19-panel-conflictos_v1.0.md) | Panel de conflictos pendientes en web | EP-04.1 | Should | 13 | Slice 9 |
| [US-20](user-stories/US-20-merge-manual_v1.0.md) | Merge manual con valores lado a lado | EP-04.1 | Should | 8 | Slice 9 |
| [US-21](user-stories/US-21-deteccion-candidatos-fusion_v1.0.md) | Detección automática de candidatos a fusión durante sync | EP-04.2 | Must | 13 | Slice 10 |
| [US-22](user-stories/US-22-ui-revision-fusion_v1.0.md) | UI de revisión y resolución de candidato a fusión | EP-04.2 | Could | 13 | Slice 10 |

---

## 3. Distribución MoSCoW

| Categoría | Total | % | US |
|---|---|---|---|
| Must Have | 12 | 54,5% | US-01, US-02, US-03, US-04, US-05, US-06, US-07, US-08, US-11, US-13, US-14, US-17, US-21 |
| Should Have | 7 | 31,8% | US-09, US-10, US-12, US-15, US-16, US-18, US-19, US-20 |
| Could Have | 3 | 13,6% | US-22 |
| Won't Have v1 | — | — | Ver [alcance EX-01 a EX-18](../00_contexto/alcance-proyecto_v1.0.md) |

> Algunas US que parecen "obvias" (mapa colaborativo, panel de conflictos) están en Should y no Must porque el MVP entrega valor sin ellas: la captura, sync y revisión básica funcionan. Permiten recortar si la fecha lo exige.

---

## 4. Cobertura de CUs por US

| CU | US |
|---|---|
| CU-01 | US-01, US-13 |
| CU-02 | US-17 |
| CU-03 | US-06, US-11, US-12 |
| CU-04 | US-02 |
| CU-05 | US-13, US-14 |
| CU-06 | US-07, US-08 |
| CU-07 | US-09 (catálogo), US-07 (móvil) |
| CU-08 | US-03, US-04, US-05, US-19 |
| CU-09 | US-15, US-16 |
| CU-10 | US-09, US-10, US-12 |
| CU-11 | US-21, US-22 |
| CU-12 | US-09 (parte de la pestaña trazabilidad) |

> Cada CU tiene al menos una US que lo cubre.

---

## 5. Backlog Técnico

Documento separado: [backlog-tecnico_v1.0.md](backlog-tecnico_v1.0.md). Incluye tareas de infraestructura, deuda técnica y trabajo no funcional (BT-01 a BT-10).

---

## 6. Definition of Ready

Documento separado: [definition-of-ready_v1.0.md](definition-of-ready_v1.0.md). Formaliza los criterios para que una US ingrese a un sprint.

---

## 7. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [roadmap-producto](../00_contexto/roadmap-producto_v1.0.md) | Épicas de pertenencia |
| [especificacion-funcional](../02_especificacion_funcional/especificacion-funcional_v1.0.md) | CUs cuya cobertura el backlog garantiza |
| [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md) | Capas tocadas por cada US |
| [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) | DoR / DoD de alto nivel |

---

**Fin del documento — product-backlog_v1.0.md**
