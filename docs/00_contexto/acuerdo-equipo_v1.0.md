**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** acuerdo-equipo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-00 via orquestador

---

# Acuerdo de Equipo

Este documento describe cómo el equipo se organiza para ejecutar el proyecto: roles, responsabilidades, herramientas, ceremonias, definiciones de listo y de hecho, y normas de colaboración. Lo que aún no fue definido por el equipo se marca como `[PENDIENTE]` y se confirma en el primer ceremonial de Sprint 0.

---

## 1. Roles del equipo

### 1.1. Roles del cliente

| Rol | Responsabilidad | Ocupante |
|---|---|---|
| Sponsor del proyecto | Aprobación, prioridades, decisiones de alcance | `[REQUIERE_INFO]` (`PROJECT-README` Sec. 2.3) |
| Referente del cliente | Definición funcional, validación de plantillas y campos, validación de UX en hitos | `[REQUIERE_INFO]` (`PROJECT-README` Sec. 2.3) |

### 1.2. Roles del equipo de desarrollo

| Rol | Responsabilidad | Asignación |
|---|---|---|
| Product Owner | Mantiene el backlog priorizado, traduce necesidades del cliente a US, valida la entrega de cada slice | `[PENDIENTE]` |
| Tech Lead / Arquitecto | Custodia de las decisiones de diseño DD-01 a DD-24, revisión de PRs críticas, escalamientos técnicos | `[PENDIENTE]` |
| Desarrollador backend (.NET) | Implementación del monolito modular y workers | `[PENDIENTE]` (`[REQUIERE_INFO]` cantidad) |
| Desarrollador frontend web (Blazor) | Implementación de la app web | `[PENDIENTE]` (`[REQUIERE_INFO]` cantidad) |
| Desarrollador móvil (MAUI) | Implementación de la app móvil offline-first | `[PENDIENTE]` (`[REQUIERE_INFO]` cantidad y experiencia previa con MAUI) |
| QA / Test engineer | Plan de pruebas, automatización de e2e, regresión | `[PENDIENTE]` |
| DevOps / Infraestructura | Scripts `.bat`, pipelines, ambientes superiores cuando se definan | `[PENDIENTE]` |

> El intake no especifica tamaño ni composición del equipo (`PROJECT-README` Sec. 7.3). Las asignaciones quedan pendientes hasta que el sponsor lo defina.

### 1.3. Sustituciones y cobertura

`[PENDIENTE]` Definir sustituto natural por rol crítico para vacaciones / ausencias.

---

## 2. Herramientas y canales

| Función | Herramienta acordada | Acceso |
|---|---|---|
| Repositorio de código | `[PENDIENTE]` (`PROJECT-BRIEF` Sec. 11.1) | — |
| Gestión del backlog | `[PENDIENTE]` (Jira / Azure DevOps / Linear / GitHub Projects) | — |
| Comunicación sincrónica | `[PENDIENTE]` (Slack / Teams / Discord) | — |
| Comunicación asincrónica | Pull requests en el repositorio + canal de chat | — |
| Documentación viva | Carpeta `/docs` en el repositorio (este documento) | — |
| Diagramas | Mermaid embebido en Markdown cuando posible; herramienta dedicada `[PENDIENTE]` | — |
| Diseño UX/UI | `[PENDIENTE]` (Figma / mockups internos) — referencia de SA-03 | — |
| Bug tracking | Misma herramienta que gestión de backlog | — |
| API contract | OpenAPI generado por backend, hospedado en `/docs` y servido desde el backend en `/swagger` | Compartido |

---

## 3. Cadencia de ceremonias

> Asume sprint de 2 semanas como sugerencia base. La duración definitiva se confirma en Sprint 0 según `[REQUIERE_INFO]` de `PROJECT-BRIEF` Sec. 8.4.

| Ceremonia | Frecuencia | Duración objetivo | Participantes | Output |
|---|---|---|---|---|
| Sprint Planning | Inicio de cada sprint | 2 horas | Equipo de desarrollo + PO | Sprint Backlog comprometido |
| Daily Stand-up | Diaria | 15 minutos | Equipo de desarrollo | Foco del día y blockers |
| Refinamiento del backlog | 1 vez por sprint | 1 hora | Equipo + PO | US listas para próximo sprint |
| Sprint Review (Demo) | Fin de cada sprint | 1 hora | Equipo + PO + cliente (sponsor / referente) | Feedback de lo entregado |
| Sprint Retrospective | Después de la Review | 1 hora | Equipo de desarrollo | Acciones de mejora |
| Validación de hitos | En cada release del roadmap (R-Alpha, R-Beta, R-MVP) | 1-2 horas | Equipo + sponsor + referente | Aceptación o lista de cambios |

`[PENDIENTE]` Definir día y hora fija de cada ceremonia en Sprint 0.

---

## 4. Definition of Ready (DoR) — alto nivel

Una User Story está lista para entrar a un sprint cuando cumple **todos** estos criterios. SA-06 puede agregar criterios específicos al backlog.

1. **Está atada a una épica del roadmap** ([roadmap-producto](roadmap-producto_v1.0.md)) y a uno o más requerimientos funcionales del intake (RF-XX) o no funcionales (RNF-XX).
2. **Tiene criterios de aceptación claros**, expresados en formato Gherkin o equivalente, verificables por QA sin ambigüedad.
3. **Está estimada por el equipo** en story points u otra unidad acordada.
4. **No depende de información marcada `[REQUIERE_INFO]`** que sea bloqueante para implementarla. Si depende de información secundaria, se documenta el supuesto.
5. **Caben las tres capas afectadas** (móvil/web + backend + DB cuando aplica) en un sprint, o la US está dividida en US menores.
6. **Tiene impacto identificado en plantillas, sincronización o storage** cuando aplica, y los puntos de extensión correspondientes están señalados.
7. **El PO confirma valor de negocio** y prioridad relativa frente al backlog.

---

## 5. Definition of Done (DoD) — alto nivel

Un ítem del sprint se considera terminado cuando cumple **todos** estos criterios. SA-08 produce el DoD detallado por capa (backend, móvil, web).

1. **Código mergeado a la rama principal** mediante pull request aprobado por al menos un revisor distinto del autor.
2. **Tests automatizados pasan en CI** (unitarios + integración + e2e cuando aplica).
3. **Cobertura de tests no degradada** respecto al baseline acordado por SA-08.
4. **No se introduce deuda técnica nueva sin documentar.** Si la US incurre en deuda explícita, queda registrada con ID `DT-XX` y plan de revisión.
5. **Documentación viva actualizada**: OpenAPI del backend, README del módulo afectado, y este `/docs/` cuando la US toca decisiones de diseño.
6. **Validación funcional manual** sobre el slice end-to-end, cuando aplica, con escenarios de la US ejecutados en ambiente local.
7. **Logs y errores observables** en consola del backend; trazabilidad mínima del flujo de la US.
8. **Sin warnings de compilación nuevos** y sin TODOs sin issue asociado.
9. **Para US que tocan sincronización**: prueba con dos dispositivos (físicos o emulados) ejecutada y registrada.
10. **Para US que tocan plantillas**: prueba de renderizado dinámico ejecutada con al menos dos plantillas distintas.
11. **Para US que tocan permisos por rol o por punto**: matriz de pruebas ejecutada con al menos los roles afectados.
12. **El PO acepta la US** según los criterios de aceptación.

---

## 6. Normas de colaboración

### 6.1. Pull requests

- Todo cambio entra por PR; no se commitea directo a la rama principal.
- Cada PR referencia su US (`refs US-XX`).
- Tamaño objetivo: ≤ 500 líneas de cambio efectivo (excluyendo tests). PRs más grandes deben justificarse.
- Un revisor distinto del autor; dos para cambios en módulos críticos (Sync, Storage, Identity).
- El autor del PR es responsable de mantener el branch al día con la rama principal.

### 6.2. Branching

- Rama por feature: `feature/US-XX-titulo-corto`.
- Rama por bugfix: `bugfix/US-XX-titulo-corto`.
- Rama de spike: `spike/nombre-corto` (descartable, no requiere PR final).

### 6.3. Commits

`[PENDIENTE]` Definir convención de mensajes (Conventional Commits sugerido: `feat:`, `fix:`, `chore:`, `refactor:`, `test:`, `docs:`).

### 6.4. Escalamiento

- Bloqueante técnico → al Tech Lead.
- Bloqueante de definición → al PO, que escala al referente del cliente.
- Bloqueante de alcance / plazo → al sponsor.
- Bloqueante en sync, storage o identity (módulos críticos) → escalamiento inmediato al Tech Lead, sin esperar daily.

### 6.5. Toma de decisiones de diseño

- Decisiones tácticas dentro del módulo: el desarrollador con consulta al revisor del PR.
- Decisiones que afectan más de un módulo: discusión asíncrona en PR / canal técnico, con el Tech Lead como árbitro.
- Decisiones que cambian alguna DD del intake: aprobación del Tech Lead + PO; se documenta en el PR y se actualiza `PROJECT-BRIEF` o se crea ADR (Architecture Decision Record) en `/docs/05_arquitectura_tecnica/adr/` (estructura concreta a definir por SA-05).

### 6.6. Documentación

- Toda decisión de diseño nueva genera o actualiza un documento en `/docs/`.
- Los `[REQUIERE_INFO]` se reportan en cada Sprint Review al cliente para resolverlos.

---

## 7. Gestión de riesgos del equipo

Los riesgos identificados en `PROJECT-README` Sec. 9.2 que afectan al equipo:

| Riesgo | Mitigación operativa | Owner |
|---|---|---|
| R-01 MAUI Blazor Hybrid + experiencia previa | Spike técnico opcional en Sprint 0 si el equipo no tiene MAUI. Buffer en estimaciones de slices con captura. | Tech Lead |
| R-02 Calidad del fix de GPS en campo | Filtros de accuracy configurables por plantilla; UI de reintento; ingreso manual como fallback. | Desarrollador móvil |
| R-03 Volumen de fotos | Defaults sensatos en plantilla raíz; monitoreo en piloto. | Backend + Móvil |
| R-04 Permisos por punto | Capa de autorización fina como invariante del módulo Identity. | Backend |
| R-05 Transaccionalidad backend ↔ storage | Patrón outbox documentado en arquitectura. | Backend |
| R-06 Blazor Server bajo redes inestables | Validar UX de reconexión SignalR en piloto. | Frontend web |

---

## 8. Compromiso del equipo

El equipo se compromete a:

1. **Respetar el alcance documentado en [alcance-proyecto](alcance-proyecto_v1.0.md).** Cambios significativos al alcance se procesan formalmente, no por iniciativa individual durante el sprint.
2. **Mantener viva la documentación.** Si una decisión de diseño cambia, el `/docs/` se actualiza en el mismo PR.
3. **No saltarse el DoR ni el DoD.** Una US que no cumple DoR no entra al sprint; una US que no cumple DoD no se reporta como completa.
4. **Comunicar bloqueos temprano.** El daily existe para esto; no esperar al sprint review.
5. **Compartir aprendizajes.** Si algo del intake o del diseño se descubre durante la implementación que cambia el plan, se comparte con el equipo y se ajusta el documento correspondiente.

---

## 9. Pendientes a resolver en Sprint 0

| Pendiente | Responsable propuesto |
|---|---|
| Confirmar duración del sprint (sugerido: 2 semanas) | Equipo + PO |
| Confirmar día/hora de las ceremonias | Equipo |
| Confirmar herramienta de gestión del backlog | Tech Lead + PO |
| Confirmar repositorio y normas de branching | Tech Lead |
| Confirmar herramienta de comunicación | Equipo |
| Resolver `[REQUIERE_INFO]` de tamaño y composición del equipo con el sponsor | Sponsor |
| Resolver `[REQUIERE_INFO]` de fecha objetivo del MVP con el sponsor | Sponsor |
| Definir convención de mensajes de commit | Tech Lead |
| Confirmar herramienta de diseño UX/UI con el cliente | PO + Cliente |

---

## 10. Versionado de este documento

Este documento se actualiza cuando hay cambios estructurales al equipo o a las normas. Los cambios menores (correcciones, ajustes de redacción) suben el patch (v1.0.1, v1.0.2…). Cambios de roles o normas suben la minor (v1.1, v1.2…).

---

## 11. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| `devs/intake/PROJECT-README.md` Sec. 7.3 | `[REQUIERE_INFO]` de tamaño de equipo y plazo |
| `devs/intake/PROJECT-README.md` Sec. 9.2 | Riesgos del proyecto que afectan al equipo |
| `devs/intake/PROJECT-BRIEF.md` Sec. 8 | Metodología Scrum + vertical slicing como base de ceremonias |

## 12. Documentos relacionados (esta sección)

- [Visión del producto](vision-producto_v1.0.md)
- [Alcance del proyecto](alcance-proyecto_v1.0.md)
- [Roadmap del producto](roadmap-producto_v1.0.md)

---

**Fin del documento — acuerdo-equipo_v1.0.md**
