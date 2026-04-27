**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** roadmap-producto_v1.0.md
**Versión:** 1.1
**Estado:** Slices 1-10 completados — pendiente Sprint 11 estabilización
**Fecha:** 2026-04-27
**Autor:** Generado por SA-00 via orquestador

> **Estado al 2026-04-27**: las 10 épicas funcionales (Slices 1-10) están
> implementadas con tests integración en CI (100/100 unit + 66/66 integration).
> Las deudas técnicas diferidas explícitamente en cada slice están consolidadas
> en [`deudas-tecnicas-mvp_v1.0.md`](../06_backlog-tecnico/deudas-tecnicas-mvp_v1.0.md).
> Resta Sprint 11 (estabilización + R-MVP).

---

# Roadmap del Producto

Roadmap estructurado en **Fases → Épicas → Milestones**, alineado a la decisión de [vertical slicing](alcance-proyecto_v1.0.md) tomada en `PROJECT-BRIEF` Sección 8. Cada épica es una funcionalidad end-to-end completa que atraviesa móvil/web + backend + DB + tests.

> Los números de sprint que aparecen en la columna "Sprint estimado" son indicativos. Los sprints concretos los fija SA-07 con su ejercicio de planificación una vez conocidas la velocidad del equipo y la duración del sprint (`[REQUIERE_INFO]` en `PROJECT-BRIEF` Sec. 8.4). Aquí se nombra un orden relativo y dependencias.

---

## Visión general del roadmap

```
┌────────────────────────────────────────────────────────────────────┐
│  FASE 0 — Cimientos                                                │
│   Sprint 0 (Walking Skeleton) → Spike Sync (1 semana)              │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│  FASE 1 — Núcleo Multi-Colaborador                                 │
│   Slice 1: Sync entre dos dispositivos                             │
│   Slice 2: Captura modo detenido + plantilla raíz                  │
│   Slice 3: Modo recorrido con radio configurable                   │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│  FASE 2 — Gestión y Plantillas                                     │
│   Slice 4: Edición desde web                                       │
│   Slice 5: Plantillas de puente y pavimento                        │
│   Slice 6: Roles, áreas, asignaciones, permisos por punto          │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│  FASE 3 — Capacidades Operativas                                   │
│   Slice 7: Carga manual web con EXIF                               │
│   Slice 8: Storage configurable real + wizard                      │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────────┐
│  FASE 4 — Resolución de Conflictos                                 │
│   Slice 9: Panel de conflictos + merge manual                      │
│   Slice 10: Detección y UI de fusión de puntos                     │
└────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                      [ Release MVP ]
                              │
                              ▼
       FASES POSTERIORES (fuera del MVP) — ver Sección 7
```

---

## Fase 0 — Cimientos

**Objetivo:** establecer la columna vertebral mínima del sistema y desriesgar la complejidad dominante (sincronización multi-colaborador) antes de comprometer slices funcionales.

**Duración estimada:** 1 sprint + 1 semana de spike (`[REQUIERE_INFO]` duración de sprint definitiva).

**Criterio de completitud:** un slice trivial end-to-end funciona localmente con scripts `.bat`, y el spike de sync valida el protocolo offline-first multi-colaborador con dos dispositivos físicos.

| Épica | Estado | Descripción | Componentes | Sprint estimado |
|---|---|---|---|---|
| EP-00.1 Walking Skeleton | ✅ | Auth ROPC con JWT, abstracción de storage (puerto + adaptador local), esqueleto de sync (outbox + pull diferencial vacíos), scripts `.bat`, pipeline de logs/errores/migraciones, slice trivial: login + crear relevamiento + un punto vacío + persistir + verlo en web | Backend + Web + Móvil + DB + Storage + Scripts | Sprint 0 |
| EP-00.2 Spike de sincronización | ✅ | Validación del protocolo de sync con dos dispositivos físicos. **No produce código de producción**, valida supuestos del diseño en `PROJECT-BRIEF` Sec. 5. | Móvil + Backend (ambiente experimental) | Spike de 1 semana |

---

## Fase 1 — Núcleo Multi-Colaborador

**Objetivo:** entregar la propuesta de valor diferencial: dos relevadores trabajando offline en el mismo relevamiento se ven mutuamente al sincronizar.

**Criterio de completitud:** el flujo de captura colaborativa funciona con plantilla raíz, modo detenido y modo recorrido con radio configurable.

| Épica | Estado | Descripción | Sprint estimado | Depende de |
|---|---|---|---|---|
| EP-01.1 Sincronización entre dos dispositivos | ✅ | Dos dispositivos crean puntos offline en el mismo relevamiento, sincronizan, ven los puntos del otro. Outbox + GUIDs en cliente + pull diferencial + LWW por campo + reintentos exponenciales. | Slice 1 | EP-00.1, EP-00.2 |
| EP-01.2 Captura modo detenido + plantilla raíz | ✅ | Diálogo unificado de captura, foto + GPS + asociación a marcador actual, modo detenido como default. Plantilla genérica raíz con campos comunes. | Slice 2 | EP-01.1 |
| EP-01.3 Modo recorrido con radio configurable | ✅ | UX label "Recorrido"; `captureMode="movil"` interno. Disparado por foto del usuario: la foto se asocia al punto activo si está dentro del radio del template; salida del radio libera el activo, próxima foto crea uno nuevo. Lazy creation: sin fotos no hay puntos. | Slice 3 | EP-01.2 |

---

## Fase 2 — Gestión y Plantillas

**Objetivo:** llevar el sistema a operación normal con web de revisión, tipos concretos de inspección, y permisos por rol y por punto.

**Criterio de completitud:** un jefe de área puede gestionar relevamientos del área desde la web y los relevadores pueden trabajar bajo plantillas de puente y pavimento con permisos correctos.

| Épica | Estado | Descripción | Sprint estimado | Depende de |
|---|---|---|---|---|
| EP-02.1 Edición desde web | ✅ | Catálogo de fotos por punto y vista plana, edición de título y descripción del punto, comentario por foto, mapa con marcadores. | Slice 4 | EP-01.3 |
| EP-02.2 Plantillas de puente y pavimento + renderizado dinámico | ✅ | Plantillas hijas con herencia, versionado, parámetros de captura específicos, renderizado dinámico de campos en móvil y web. | Slice 5 | EP-02.1 |
| EP-02.3 Roles, áreas, asignación de colaboradores, permisos por punto | ✅ | Aceptación jerárquica admin → jefe → relevador, asignación múltiple a un relevamiento, permisos de edición por punto (vía sync push). | Slice 6 | EP-02.2 |

---

## Fase 3 — Capacidades Operativas

**Objetivo:** habilitar entradas alternativas de datos (carga manual web) y operación con storage productivo configurable.

**Criterio de completitud:** un usuario puede subir un lote de fotos previas con EXIF desde la web y el sistema operar con storage S3/FTP/SFTP elegido por el admin raíz.

| Épica | Estado | Descripción | Sprint estimado | Depende de |
|---|---|---|---|---|
| EP-03.1 Carga manual desde web con EXIF | ✅ | Subida en lote, extracción de EXIF, cola de fotos sin GPS para ingreso manual, agrupación por modo seleccionado, comentarios genéricos editables. | Slice 7 | EP-02.3 |
| EP-03.2 Storage configurable real + wizard de primer arranque | ✅ | Adaptadores S3 / FTP / SFTP funcionales, wizard del admin raíz en primer arranque para configurar storage y credenciales, tabla de configuración persistida con DataProtection. Hot-swap del activo requiere restart (deuda diferida). | Slice 8 | EP-02.3 |

---

## Fase 4 — Resolución de Conflictos

**Objetivo:** cerrar el ciclo de la sincronización con UX humana de revisión: lo que LWW resuelve automáticamente se notifica, y los casos ambiguos van a paneles de revisión.

**Criterio de completitud:** un jefe de área puede resolver desde la web los conflictos pendientes y revisar candidatos a fusión con UI lado a lado.

| Épica | Estado | Descripción | Sprint estimado | Depende de |
|---|---|---|---|---|
| EP-04.1 Panel de conflictos y merge manual | ✅ | Panel web con sobrescrituras a revertir (LWW), precedencia del dueño, capturas rechazadas por relevamiento cerrado. UI con valores lado a lado y acción revert/keep. | Slice 9 | EP-03.1, EP-03.2 |
| EP-04.2 Detección y UI de fusión de puntos cercanos | ✅ | Detección durante sync (RN-09): puntos cercanos por colab. distintos en ventana temporal. Panel de revisión, acciones Fusionar (centroide / keep_a / keep_b) o Mantener separados, evento `merged`. Selector field-by-field y mini-mapa lado a lado quedan como deuda. | Slice 10 | EP-04.1 |

---

## Releases

| Release | Contenido | Significado |
|---|---|---|
| R-Skeleton | Final de EP-00.1 | Demostrable internamente; aún no se ofrece a usuarios. |
| R-Sync Spike | Final de EP-00.2 | Decisión informada de avanzar con el diseño de sync; no es release de software. |
| R-Alpha Multi-colab | Final de Fase 1 (EP-01.3) | Primera demostración al cliente: dos relevadores capturando en el mismo relevamiento. |
| R-Beta Operativo | Final de Fase 2 (EP-02.3) | Cliente puede probar con uno o dos equipos reales en piloto controlado, con plantillas de puente y pavimento. |
| R-MVP | Final de Fase 4 (EP-04.2) | Liberación al cliente del MVP completo. Incluye storage productivo, panel de conflictos, fusión de puntos. |

---

## Hitos de validación con el cliente

| Hito | Cuándo | Qué se valida |
|---|---|---|
| H-1 Demo inicial | Final de Fase 1 | Multi-colaborador funciona; el cliente confirma que el flujo refleja lo que pidió. |
| H-2 Validación de plantillas | Final de Fase 2 | Las plantillas iniciales (puente y pavimento) son las correctas; ajustes antes de Fase 3. |
| H-3 Piloto operativo | Final de Fase 3 | Un equipo real opera con storage configurado; feedback de UX y estabilidad. |
| H-4 Aceptación del MVP | Final de Fase 4 | El sponsor acepta el MVP para go-live. |

---

## Mapeo de épicas a milestones del backlog

Cada épica del roadmap se descompone en User Stories en SA-06 (backlog técnico). El identificador `EP-XX.Y` se conserva como prefijo de trazabilidad: las US derivadas portarán referencia explícita a su épica de origen.

```
EP-00.1 → US-walking-skeleton-*
EP-00.2 → no produce US (es spike)
EP-01.1 → US-sync-bidireccional-*
EP-01.2 → US-captura-detenido-*
EP-01.3 → US-modo-movil-radio-*
EP-02.1 → US-edicion-web-*
EP-02.2 → US-plantillas-puente-pavimento-*
EP-02.3 → US-roles-permisos-*
EP-03.1 → US-carga-manual-web-*
EP-03.2 → US-storage-wizard-*
EP-04.1 → US-panel-conflictos-*
EP-04.2 → US-fusion-puntos-*
```

> El detalle de US y sus identificadores definitivos los fija SA-06.

---

## Cronograma estimado

`[REQUIERE_INFO]` Las duraciones absolutas dependen de:

- Velocidad del equipo (story points por sprint).
- Duración del sprint (típicamente 2 semanas; a confirmar).
- Tamaño y composición del equipo (`[REQUIERE_INFO]` en `PROJECT-README` Sec. 7.3).
- Fecha objetivo de entrega del MVP (`[REQUIERE_INFO]` en `PROJECT-README` Sec. 7.3).

Una vez disponible esta información, SA-07 producirá el plan concreto del Sprint 1 (y subsiguientes a medida que se ejecuten).

---

## Posicionamiento del MVP en el ciclo de vida del producto

```
Pre-MVP            MVP (este roadmap)               Post-MVP (fuera de alcance)
  │                       │                                  │
  └──> Walking ──> Slices 1-10 ──> Release MVP ──> Reportería avanzada
       Skeleton                                       ML pre-clasificación
       + Spike                                        Cierre formal
                                                      Migración OAuth 2.1
                                                      Archivado frío
                                                      Integraciones externas
```

---

## 7. Trabajo reservado para fases posteriores

Funcionalidades documentadas como excluidas en [alcance-proyecto](alcance-proyecto_v1.0.md) Sección 2 que el cliente puede priorizar en releases post-MVP:

| Fase candidata | Funcionalidad | Disparador esperado |
|---|---|---|
| F+1 — Cierre formal | Etapa de aprobación de relevamiento por jefe de área | Decisión del sponsor |
| F+1 — Reportería | Dashboards y exportes a Excel/PDF | Necesidades de gestión interna |
| F+2 — Operación a escala | Migración masiva entre storages, archivado frío | Volumen real supera umbrales |
| F+2 — Seguridad | Migración OAuth 2.1 con code+PKCE | Endurecimiento de postura o cliente externo |
| F+3 — Inteligencia | Pipeline ML de pre-clasificación de defectos | Disponibilidad de dataset etiquetado |
| F+3 — Integraciones | Conexión con sistemas existentes de Vialidad | Identificación por el sponsor |

---

## 8. Trazabilidad

| Documento upstream | Aporte al roadmap |
|---|---|
| `devs/intake/PROJECT-BRIEF.md` Sec. 8 | Estructura de slices y metodología vertical slicing |
| `devs/intake/PROJECT-BRIEF.md` Sec. 5 | Justificación del spike de sync como Fase 0 |
| `devs/intake/PROJECT-README.md` Sec. 5 | Cobertura funcional por épica |
| [Alcance del proyecto](alcance-proyecto_v1.0.md) | Lista de exclusiones que reaparecen como fases post-MVP |

## 9. Documentos relacionados (esta sección)

- [Visión del producto](vision-producto_v1.0.md)
- [Alcance del proyecto](alcance-proyecto_v1.0.md)
- [Acuerdo de equipo](acuerdo-equipo_v1.0.md)

---

**Fin del documento — roadmap-producto_v1.0.md**
