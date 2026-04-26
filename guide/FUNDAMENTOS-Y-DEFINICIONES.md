# Fundamentos y Definiciones — Metodología SDD con Subagentes Orquestados

**Documento:** FUNDAMENTOS-Y-DEFINICIONES.md
**Versión:** 1.0
**Fecha:** 2026-04-23
**Audiencia:** Desarrolladores y arquitectos que quieren entender el marco teórico detrás de la metodología

---

## Índice

1. [Specification-Driven Development (SDD)](#1-specification-driven-development-sdd)
2. [La cadena de subagentes orquestados](#2-la-cadena-de-subagentes-orquestados)
3. [Métodos ágiles integrados](#3-métodos-ágiles-integrados)
4. [Patrones de prompt engineering](#4-patrones-de-prompt-engineering)
5. [Las 13 especialidades de agentes](#5-las-13-especialidades-de-agentes)
6. [Trazabilidad vertical — la cadena completa](#6-trazabilidad-vertical--la-cadena-completa)
7. [Referencias de arquitectura técnica](#7-referencias-de-arquitectura-técnica)
8. [Ejemplos aplicados](#8-ejemplos-aplicados)
9. [Mapa del conocimiento en /devs](#9-mapa-del-conocimiento-en-devs)

---

## 1. Specification-Driven Development (SDD)

### 1.1 Definición

SDD es un enfoque de desarrollo en el que se escribe primero una especificación formal antes de generar o escribir código. En el contexto de desarrollo asistido por IA, la especificación actúa como el **contrato que guía al modelo** para producir implementaciones coherentes y verificables.

La premisa central: **la especificación define el qué y el por qué; el código define el cómo.**

Sin una spec previa, los modelos de lenguaje generan código que es localmente correcto (compila, pasa los tests inmediatos) pero globalmente incoherente: cada sesión produce decisiones de diseño desconectadas, sin trazabilidad a los requerimientos del negocio y sin consistencia entre componentes.

### 1.2 Los tres niveles de SDD

#### Nivel 1 — Spec-First *(el más común)*

> "La spec guía la implementación. Una vez completada, se descarta."

El flujo es: escribir la spec → el agente la implementa → descartar el documento.

**Cuándo usarlo:**
- Tareas de una sola sesión
- Proyectos personales sin coordinación de equipo
- Prototipos y experimentos rápidos
- Correcciones puntuales de bugs

**Ventaja:** sin carga de mantenimiento.
**Desventaja:** en seis meses no habrá documentación disponible si se necesitan modificaciones.

#### Nivel 2 — Spec-Anchored *(estándar para equipos)*

Tanto la especificación como el código se mantienen como **artefactos vivos**. Los cambios a la spec **preceden siempre** a los cambios de código.

**Cuándo usarlo:**
- Proyectos con múltiples contribuyentes
- Sistemas que requieren documentación de compliance
- Productos con horizonte de mantenimiento largo (6+ meses)
- Features que sufrirán iteraciones múltiples

**Requisito crítico:** disciplina para actualizar la spec antes de tocar el código. Sin esa disciplina, spec y código divergen y el artefacto pierde su valor como fuente de verdad.

#### Nivel 3 — Spec-as-Source *(experimental)*

La especificación **es** el artefacto principal; el código se regenera bajo demanda a partir de ella.

**Desafío fundamental — inconsistencia determinística:**
> "Identical specifications do not produce identical code."

Dos generaciones con la misma spec producen implementaciones sintácticamente distintas, lo que complica el debugging y el control de versiones.

**Cuándo tiene sentido:**
- Código altamente repetitivo (operaciones CRUD)
- Código desechable (scripts de migración, scaffolding)
- Entornos con cobertura de tests robusta que validen el comportamiento sin importar la forma del código

### 1.3 Marco de decisión

| Situación | Nivel recomendado |
|---|---|
| Trabajo individual, sesión única | Spec-First (1) |
| Equipo de 2+ personas | Spec-Anchored (2) |
| Sistema con mantenimiento 6+ meses | Spec-Anchored (2) |
| Código repetitivo / desechable con tests | Spec-as-Source (3) |
| Experimento con buena cobertura de tests | Spec-as-Source (3) |

**Esta metodología implementa el Nivel 2 (Spec-Anchored).** La documentación en `/docs/` es el artefacto vivo que precede y guía el código. Toda modificación al sistema debe iniciar con una actualización documental.

### 1.4 Por qué SDD con IA es diferente a documentación tradicional

En el desarrollo tradicional, la documentación se escribe después del código (si se escribe). Con IA, la relación se invierte porque el modelo necesita contexto para producir código coherente.

El flujo SDD con IA:
```
Spec completa → IA genera código coherente con la spec
```

El flujo sin SDD con IA:
```
IA genera código sin contexto → cada sesión es inconsistente
→ el humano "parchea" manualmente la coherencia
→ a los 3 sprints, el sistema es inmantenible
```

La spec no es burocracia — es el mecanismo que hace que la IA produzca resultados reproducibles.

---

## 2. La cadena de subagentes orquestados

### 2.1 Arquitectura de la cadena

La metodología organiza el trabajo en una jerarquía de tres niveles:

```
NIVEL 1 — INTAKE (documentos de entrada)
  PROJECT-README.md    ← contexto, cliente, requerimientos
  PROJECT-BRIEF.md     ← arquitectura, metodología, decisiones

NIVEL 2 — REGLAS (contratos de construcción)
  XX_rules.md          ← un archivo por sección de /docs/
  (qué leer, qué producir, cómo construir, qué no hacer)

NIVEL 3 — ORQUESTADOR (prompt maestro)
  master-prompt.md     ← ejecuta la cadena, resuelve dependencias,
                          gestiona la compuerta de aprobación
```

### 2.2 Por qué subagentes en lugar de un agente único

Un agente único que intente generar toda la documentación en un solo prompt produce outputs genéricos porque no puede mantener el nivel de especialización que requiere cada disciplina. Un arquitecto de software piensa diferente a un analista de negocio, y ese perfil profesional cambia los criterios de calidad del output.

Los subagentes especializados resuelven tres problemas:

**Profundidad de contexto:** cada subagente lee solo los documentos relevantes para su sección, evitando la dilución de contexto que ocurre cuando se le pasa todo el proyecto a un modelo.

**Criterios de calidad específicos:** cada `XX_rules.md` define exactamente qué constituye un buen output para esa disciplina — los criterios de un analista de negocio (trazabilidad NB→CU) son diferentes a los de un DevOps (quality gates en el pipeline).

**Dependencias explícitas:** el grafo de dependencias garantiza que SA-02 siempre lee los outputs de SA-01 antes de generar CUs, eliminando la inconsistencia entre secciones.

### 2.3 El grafo de dependencias

```
SA-00 (Contexto estratégico)
  └→ SA-01 (Necesidades de negocio)
       └→ SA-02 (Especificación funcional)
            ├→ SA-03 (UX/UI) ──────────────────────────┐
            ├→ SA-04 (Prompts IA)                       │
            └→ SA-05 (Arquitectura técnica) ←───────────┘
                 └→ SA-06 (Backlog técnico)
                      └→ SA-07 (Sprint Plan)
                           └→ SA-08 (Calidad y Pruebas)
                                └→ SA-09 (DevOps / CI-CD)
```

SA-03, SA-04 y SA-05 pueden ejecutarse en paralelo una vez que SA-02 está completo. SA-03 es upstream de SA-05 (la arquitectura toma decisiones de API basándose en los flujos de UX).

### 2.4 El rol del humano en la cadena

El humano no es pasivo. Es el **árbitro de calidad** en dos momentos críticos:

**Antes de la cadena:** llena los intakes con información real del cliente. Sin intakes bien completados, los subagentes producen documentación genérica e inútil.

**Al finalizar la cadena:** la compuerta de aprobación es una decisión humana irreemplazable. Ningún subagente puede saber si la visión de producto captura el verdadero dolor del cliente o si el Sprint 1 es realista para ese equipo específico.

### 2.5 La compuerta de aprobación

La compuerta es el mecanismo que separa la fase documental de la fase de construcción. Es una restricción deliberada, no un trámite.

**Por qué es necesaria:**
- El costo de corregir un error en documentación es bajo; el costo de corregirlo en código es alto
- La documentación aprobada se convierte en el contrato que guía el codeo — si el humano no la leyó, ese contrato no existe
- Los `[REQUIERE_INFO]` que quedan en la documentación son señales de que falta información crítica que el código va a necesitar

**Protocolo de aprobación:**
```
1. Orquestador presenta informe de completitud
2. Humano revisa /docs/ (mínimo 30 min)
3. Humano sugiere modificaciones (o no)
4. Orquestador aplica modificaciones
5. Humano escribe "APROBADO"
6. Fase de codeo habilitada
```

---

## 3. Métodos ágiles integrados

### 3.1 Por qué metodología ágil en SDD

SDD y los métodos ágiles no son opuestos — son complementarios. SDD define **qué** documentar y en qué orden; las metodologías ágiles definen **cómo** ejecutar el trabajo en ciclos iterativos una vez que la spec existe.

La integración funciona así: la documentación SDD produce el Product Backlog (US), el Sprint Plan y el DoD. Las ceremonias ágiles operan sobre esos artefactos.

### 3.2 Scrum en el contexto SDD

Los elementos de Scrum que la metodología genera directamente:

| Artefacto Scrum | Generado por | Ubicación |
|---|---|---|
| Product Backlog | SA-06 | `docs/06_backlog-tecnico/product-backlog_v1.0.md` |
| User Stories | SA-06 | `docs/06_backlog-tecnico/user-stories/US-XX.md` |
| Sprint Backlog | SA-07 | `docs/07_plan-sprint/plan-iteracion_sprint-XX.md` |
| Definition of Done | SA-08 | `docs/08_calidad_y_pruebas/definition-of-done_v1.0.md` |
| Definition of Ready | SA-06 | `docs/06_backlog-tecnico/definition-of-ready_v1.0.md` |

Los que el equipo gestiona manualmente durante la ejecución: velocity, burndown, retrospectivas, reviews.

### 3.3 Vertical slicing vs horizontal slicing

Esta es una de las decisiones más importantes que se toma en el PROJECT-BRIEF.

#### Horizontal slicing — el anti-patrón

Cada sprint completa una capa técnica completa. El valor funcional llega solo cuando todas las capas están listas.

```
Sprint 1 → toda la base de datos
Sprint 2 → toda la API
Sprint 3 → todo el frontend
Sprint 4 → primera demo funcional ← valor en semana 8
```

**Problema:** el riesgo se acumula al final. Si el cliente ve algo en la semana 8 y quiere cambios estructurales, el costo es máximo.

#### Vertical slicing — el estándar de esta metodología

Cada sprint entrega una funcionalidad completa end-to-end, aunque limitada en scope.

```
Sprint 1 → "El usuario puede registrarse y hacer login"
           UI mínima + API + base de datos + test
Sprint 2 → "El usuario puede crear un perfil"
           UI de perfil + API + storage + test
Sprint 3 → "El usuario puede invitar a otro usuario"
           UI de invitación + API + email + test
           ← valor en semana 2
```

**Ventaja:** el cliente puede usar (y criticar) algo funcionando desde el Sprint 1. Los cambios estructurales se detectan temprano cuando son baratos.

#### Ejemplos de vertical slicing por tipo de sistema

**Sistema web con autenticación:**

| Sprint | Slice | Capas involucradas |
|---|---|---|
| Sprint 1 | Registro + login con email | Frontend + API + DB + tests |
| Sprint 2 | Perfil de usuario editable | Frontend + API + Storage + tests |
| Sprint 3 | Recupero de contraseña | Frontend + API + Email + tests |

**Librería .NET / SDK:**

| Sprint | Slice | Capas involucradas |
|---|---|---|
| Sprint 1 | Pipeline mínimo funcional end-to-end | Parser + Evaluator básico + Renderer mínimo |
| Sprint 2 | Tipos de datos adicionales | Parser extendido + Evaluator extendido + Renderer |
| Sprint 3 | Primer renderer alternativo | Solo capa de render |

**API REST con microservicios:**

| Sprint | Slice | Capas involucradas |
|---|---|---|
| Sprint 1 | CRUD completo de una entidad | Endpoint + Servicio + Repositorio + DB + tests |
| Sprint 2 | Segunda entidad con relación | ídem + relaciones |
| Sprint 3 | Autenticación en todos los endpoints | Auth middleware + todos los services |

### 3.4 Walking Skeleton — el primer slice

El Walking Skeleton es el primer vertical slice del proyecto. No tiene funcionalidad real completa, pero demuestra que todas las capas se conectan y el pipeline funciona de punta a punta.

**Para qué sirve:**
- Valida que la arquitectura elegida es viable antes de invertir en features
- Da al equipo un baseline funcional desde el que agregar funcionalidad
- Elimina el riesgo de descubrir problemas de integración tarde

**Cómo diseñarlo:**
1. Identificar el flujo más simple que atraviese todas las capas
2. Implementar cada capa con el mínimo necesario para ese flujo
3. El skeleton debe ser deployable y ejecutable — no es un prototipo descartable

**Ejemplo para un sistema web:**
```
Skeleton: "El usuario puede ver una página con su nombre"
  → Frontend: página HTML estática con llamada a API
  → API: endpoint GET /user/me que devuelve {name: "test"}
  → DB: tabla users con un registro de prueba
  → Auth: sin auth por ahora — se agrega en Sprint 2
  → Deploy: funciona en localhost y en staging
```

### 3.5 Priorización MoSCoW

Toda User Story en el Product Backlog lleva una prioridad MoSCoW. La metodología la aplica en SA-06.

| Prioridad | Criterio | Restricción |
|---|---|---|
| **Must Have** | Sin esto el MVP no cumple su propósito | ≤ 60% del backlog |
| **Should Have** | El MVP funciona sin esto pero sería incompleto | Sin límite |
| **Could Have** | Agrega valor pero puede esperar a v2 | Sin límite |
| **Won't Have v1** | Fuera de scope pero documentado | Siempre documentar el por qué |

**Regla crítica:** si todo es Must Have, la priorización es incorrecta. Forzar que Must Have represente como máximo el 60% del backlog. Si no se puede recortar, el scope es demasiado grande para el presupuesto/tiempo disponible.

---

## 4. Patrones de prompt engineering

La metodología identifica 7 patrones recurrentes de prompt que se usan a lo largo de la cadena.

### 4.1 Patrón "Lectura antes de acción"

El patrón más importante. Todo prompt comienza con una lista explícita de archivos a leer antes de escribir cualquier cosa.

```
Antes de escribir cualquier archivo, leé en este orden:
1. devs/intake/PROJECT-README.md
2. devs/intake/PROJECT-BRIEF.md
3. devs/rules/00_rules.md

Respondeme SOLO con:
- Nombre del proyecto
- Problema central que resuelve
- Metodología de ejecución elegida

NO crees ningún archivo todavía.
```

**Por qué funciona:** previene que el agente "invente" contexto basándose en suposiciones. La confirmación explícita de comprensión antes de actuar es el equivalente a un code review previo al commit.

### 4.2 Patrón "Uno a la vez con confirmación"

Para generación de múltiples artefactos, exige creación secuencial con confirmación humana entre cada uno.

```
Creá estos archivos en orden, UNO A LA VEZ,
esperando mi confirmación entre cada uno:

ARCHIVO 1 — docs/00_contexto/vision-producto_v1.0.md
ARCHIVO 2 — docs/00_contexto/alcance-proyecto_v1.0.md

Al terminar cada archivo, mostrame las primeras 10 líneas
para que pueda verificar antes de continuar.
```

**Por qué funciona:** los errores se detectan temprano. Si el primer archivo está mal, no contamina los siguientes.

### 4.3 Patrón "Diagnóstico antes de plan"

Antes de planificar cualquier sprint o nueva funcionalidad, ejecutar un diagnóstico del estado actual.

```
Antes de planificar Sprint 02, leé estos documentos:
1. docs/07_plan-sprint/plan-iteracion_sprint-01_v1.0.md
2. docs/06_backlog-tecnico/product-backlog_v1.0.md

Respondeme SOLO esto, sin generar ningún archivo:
1. Qué US del Sprint 1 están en estado "Completado"
2. Qué US quedaron pendientes con su razón
3. Tu recomendación de qué entra en Sprint 2 y por qué

No planifiques todavía. Solo el diagnóstico.
```

**Por qué funciona:** separa el análisis de la ejecución. Un plan basado en un diagnóstico explícito es más confiable que uno que asume el estado.

### 4.4 Patrón "Decisión de arquitectura embebida"

Cuando hay restricciones de diseño que el agente debe respetar, incluirlas explícitamente en el prompt.

```
RESTRICCIÓN ARQUITECTÓNICA — leer antes de codear:
La librería NO es responsable de obtener datos desde APIs externas.
Esa responsabilidad es del cliente consumidor.
El método Render() recibe los datos ya resueltos como parámetro.

Implementá CU-25 respetando esta restricción.
Si el caso de uso contradice esta restricción, reportalo
antes de generar código.
```

**Por qué funciona:** los modelos tienden a "completar" lo que falta con suposiciones razonables. Las restricciones explícitas evitan que esas suposiciones sean incorrectas.

### 4.5 Patrón "Corrección de desvío"

Cuando el agente pierde contexto (por compactación de conversación larga o cambio de sesión), usarlo para reorientar.

```
El estado actual del proyecto es:
- SA-00 a SA-04: COMPLETOS
- SA-05: EN PROGRESO — falta contratos-interfaces_v1.0.md
- SA-06 a SA-09: PENDIENTES

Continuá desde donde quedamos. Leé:
1. docs/05_arquitectura_tecnica/arquitectura-solucion_v1.0.md (ya existe)
2. devs/rules/05_rules.md

Generá SOLO contratos-interfaces_v1.0.md.
No repitas lo que ya generaste.
```

**Por qué funciona:** reestablece el contexto sin rehacer trabajo ya completado.

### 4.6 Patrón "Orquestador + sub-agentes"

Para tareas de evaluación o refinamiento de múltiples secciones en paralelo, estructurar el prompt como un orquestador que lanza sub-agentes especializados.

```
Sos un agente orquestador. Tu trabajo es lanzar 3 sub-agentes
especializados y consolidar sus hallazgos.

SA-01 — Analista de Negocio:
  Scope: docs/01_necesidades_negocio/
  Misión: Verificar trazabilidad NB→CU
  Output: tabla de hallazgos con ID, severidad, archivo, descripción

SA-02 — Analista Funcional:
  Scope: docs/02_especificacion_funcional/
  Misión: Verificar criterios de aceptación Given/When/Then
  Output: tabla de hallazgos

Consolidación: Dashboard con puntuación 1-10 por sección
y Top 5 hallazgos críticos.
```

**Por qué funciona:** mantiene el foco de cada sub-agente en su dominio, produce outputs estructurados comparables y facilita la priorización de correcciones.

### 4.7 Patrón "Spec-to-code con DoD"

Para la fase de codeo, el prompt incluye la US, el CU relacionado y el DoD como criterio de terminación.

```
Implementá la siguiente User Story:
[pegar contenido de US-01]

Lee también:
- docs/02_especificacion_funcional/casos-de-uso/CU-01-[nombre].md
- docs/08_calidad_y_pruebas/definition-of-done_v1.0.md

Al terminar, verificá cada criterio del DoD antes de
marcar la US como completada. Si algún criterio no se cumple,
indícalo antes de continuar.
```

**Por qué funciona:** el DoD es el único criterio objetivo de "terminado". Sin él, el agente declara terminado cuando compila — no cuando la funcionalidad está verificada contra los criterios del negocio.

---

## 5. Las 13 especialidades de agentes

Cada sección de `/docs/` está asignada a un agente con el perfil profesional más adecuado para evaluar y producir su contenido.

| ID | Especialidad | Sección que gestiona | Competencias clave |
|---|---|---|---|
| AG-ROOT | Arquitecto de Soluciones Senior | `docs/README.md` | Coherencia cross-documental, narrativa técnica, onboarding |
| AG-00 | Product Manager / Analista de Negocio | `docs/00_contexto/` | Visión de producto, alcance, roadmap, gestión de expectativas |
| AG-01 | Analista de Negocio Senior | `docs/01_necesidades_negocio/` | NB, trazabilidad vertical, métricas de valor |
| AG-02 | Analista Funcional / Ing. Requisitos | `docs/02_especificacion_funcional/` | CU, RN, modelo conceptual, criterios BDD |
| AG-03 | Especialista en Developer Experience | `docs/03_ux-ui/` | Flujos de usuario, wireframes, ergonomía de API |
| AG-04 | Ingeniero de Prompts / AI Specialist | `docs/04_prompts_ai/` | Prompts estructurados, guardrails, evaluación de LLM outputs |
| AG-05 | Arquitecto de Software Senior | `docs/05_arquitectura_tecnica/` | ADRs, contratos/interfaces, diagramas C4, extensibilidad |
| AG-06 | Scrum Master / Agile Coach | `docs/06_backlog-tecnico/` | Product Backlog, DoR, MoSCoW, estimación |
| AG-07 | Scrum Master / Gestión de Proyectos | `docs/07_plan-sprint/` | Sprint planning, velocity, reviews, burndown |
| AG-08 | Ingeniero QA / SDET Senior | `docs/08_calidad_y_pruebas/` | Testing strategy, DoD, casos de prueba, quality gates |
| AG-09 | Ingeniero DevOps Senior | `docs/09_devops/` | CI/CD, SemVer, ambientes, rollback |
| AG-10 | Technical Writer / Developer Advocate | `docs/10_developer_guide/` | Quick start, tutoriales, troubleshooting |
| AG-11 | Developer Advocate / Ing. de Aplicación | `docs/11_examples/` | Ejemplos funcionales, progresión de complejidad |

### Principio de especialidad

Cada agente tiene un perfil que determina sus **criterios de calidad implícitos**. No es solo un título decorativo — cambia cómo evalúa un output.

Un **Analista de Negocio (AG-01)** considera incompleto un documento si las NB no tienen criterios de éxito medibles. Un **Arquitecto de Software (AG-05)** considera incompleto un documento si los contratos de interfaz no especifican los errores posibles. Un **DevOps Senior (AG-09)** considera incompleto un pipeline que no tiene rollback documentado.

Estos criterios están formalizados en los archivos `XX_rules.md` — cada uno es la concreción del perfil de ese agente especialista.

---

## 6. Trazabilidad vertical — la cadena completa

La metodología garantiza trazabilidad vertical completa a través de toda la documentación. Cada artefacto referencia a sus padres (upstream) y sus hijos (downstream).

```
Visión del producto (00_contexto)
  ↓ define el problema que resuelve
Necesidades de Negocio NB-XX (01_necesidades)
  ↓ cada NB origina al menos un CU
Casos de Uso CU-XX (02_especificacion)
  ↓ cada CU define restricciones de diseño
Decisiones de Arquitectura ADR (05_arquitectura)
  ↓ la arquitectura habilita el diseño del backlog
User Stories US-XX (06_backlog)
  ↓ cada US referencia el CU que implementa
Backlog Técnico BT-XX (06_backlog)
  ↓ las US se organizan en iteraciones
Sprint Plan TK-XX (07_plan-sprint)
  ↓ cada sprint tiene criterios de completitud
Test Cases TC-XX (08_calidad)
  ↓ los tests validan los criterios del CU
Pipeline CI/CD (09_devops)
  ↓ el pipeline ejecuta los tests automáticamente
```

### Por qué importa la trazabilidad

**Para el desarrollo:** cuando un desarrollador implementa una US, puede leer el CU completo (con flujos alternativos y reglas de negocio) para entender exactamente qué debe hacer el código. No hay ambigüedad.

**Para el testing:** cada test case traza a un CU con criterios Given/When/Then. El QA sabe exactamente qué validar y cuándo un test falla realmente importa.

**Para el mantenimiento:** cuando el cliente pide un cambio seis meses después, el desarrollador puede seguir la cadena hacia arriba (código → US → CU → NB) para entender el impacto del cambio antes de tocarlo.

**Para la IA:** cuando Claude genera código a partir de una US, puede leer el CU completo, las reglas de negocio relacionadas y el DoD. El código resultante es coherente con la intención del negocio, no solo con el enunciado de la US.

### Verificación de trazabilidad

El informe de completitud del orquestador incluye un mapa de trazabilidad que muestra los eslabones completos e incompletos:

```
NB-01 → CU-01 ✓ → US-01 ✓ → Sprint 1 ✓
NB-01 → CU-02 ✓ → US-02 ✓ → Sprint 1 ✓
NB-02 → CU-03 ✓ → US-03 ✗ ← eslabón roto: CU-03 no tiene US asignada
NB-03 → [sin CU] ✗ ← eslabón roto: NB-03 no tiene CU
```

Los eslabones rotos son gaps que el humano debe resolver antes de aprobar.

---

## 7. Referencias de arquitectura técnica

Esta sección documenta los marcos arquitectónicos que los subagentes (especialmente SA-05) tienen disponibles como referencia.

### 7.1 Arquitectura de microservicios

#### Cuándo usarla

| Condición | Usar microservicios | No usar microservicios |
|---|---|---|
| Dominios de negocio | Claramente separados (chat, pagos, formularios) | Un solo dominio cohesivo |
| Equipos | Múltiples equipos que deployean independientemente | 1-3 desarrolladores |
| Escalabilidad | Heterogénea por servicio | Uniforme |
| Fase del proyecto | Producto maduro | MVP o prototipo |

#### Evolución natural de arquitecturas

```
Fase 1 — MVP (1-3 devs):
  Monolito modular → un solo deploy, separación por carpetas

Fase 2 — Producto (3-8 devs):
  Microservicios → extraer servicios uno a uno según el bottleneck

Fase 3 — Escala (8-20 devs):
  Microservicios maduros + event bus + API gateway

Fase 4 — Plataforma (20+ devs):
  Microservicios + API pública + webhooks + micro-frontends
```

La recomendación: **no saltar fases**. Empezar con monolito modular, migrar a microservicios cuando hay razones concretas (equipo, escala, dominios) y no antes.

#### Scaffolding estándar por microservicio (.NET — Clean Architecture)

```
ServiceName/
├── ServiceName.Domain/          ← entidades, value objects, interfaces
├── ServiceName.Application/     ← commands, queries, handlers (MediatR)
├── ServiceName.Infrastructure/  ← repositorios, EF Core, servicios externos
└── ServiceName.API/             ← controllers, middleware, DI config
```

Cada capa tiene una responsabilidad explícita y no depende de las capas externas a ella.

### 7.2 Patrones de integración entre servicios

| Patrón | Cuándo | Tecnología típica |
|---|---|---|
| REST síncrono | Request/response, baja latencia | HTTP + OpenAPI |
| Eventos asíncronos | Desacoplamiento, tolerancia a fallos | RabbitMQ + MassTransit |
| gRPC | Comunicación interna, alta performance | Protobuf |
| WebSockets | Tiempo real (chat, notificaciones) | SignalR |

### 7.3 Multi-tenancy

En sistemas SaaS B2B, la multi-tenancy define cómo se aíslan los datos de cada cliente.

**Estrategia por columna (shared database):**
```sql
-- Todo registro tiene TenantId
SELECT * FROM Orders WHERE TenantId = @tenantId
```
Más simple, menor costo de infraestructura, suficiente para la mayoría de los casos.

**Estrategia por schema:**
```sql
-- Cada tenant tiene su propio schema
SELECT * FROM tenant_001.Orders
```
Mayor aislamiento, más complejidad operacional.

**Estrategia por base de datos:**
Máximo aislamiento, costo proporcional al número de tenants.

### 7.4 Gestión de archivos y storage

Principios fundamentales para cualquier sistema que maneje archivos:

**Abstracción de proveedor:** ningún servicio debe acoplarse a S3, Azure Blob o disco local. Definir una interfaz `IFileStorage` e inyectar la implementación.

**Archivos como recursos con identidad:** cada archivo recibe un ID único (GUID), se almacena con metadata (nombre original, MIME type, tamaño, hash SHA-256) y se referencia por ID, nunca por path.

**URLs firmadas de corta duración:** los archivos nunca se sirven directamente. Se generan URLs pre-firmadas con expiración (5-60 minutos) que autorizan el acceso temporal.

**Flujo estándar de upload:**
```
Cliente → POST /files/upload → File Service
  → valida MIME type y tamaño
  → guarda en storage con GUID
  → indexa metadata en DB
  → devuelve { fileId, expiresAt, downloadUrl }
```

### 7.5 Frontend: estrategias de cliente

| Estrategia | Cuándo | Tecnología |
|---|---|---|
| Blazor Server | Aplicaciones internas, admin panels, low-latency | .NET + SignalR |
| SPA (React/Angular/Vue) | Aplicaciones públicas, UX compleja, equipos frontend | Framework + REST/GraphQL |
| Aplicación móvil nativa | iOS/Android con features de plataforma | Swift/Kotlin o MAUI |
| Widgets embebibles | Integración en páginas de terceros | Web Components + iframe |

**Elección en función del equipo:** si el equipo es principalmente .NET, Blazor Server reduce el cambio de contexto. Si hay diseñadores UX o desarrolladores frontend especializados, una SPA es más adecuada.

---

## 8. Ejemplos aplicados

### 8.1 Ejemplo completo — Motor DSL de documentos (.NET)

El proyecto que originó esta metodología. Un motor de transformación que convierte plantillas DSL (JSON) en documentos imprimibles (ESC/POS, texto, MAUI).

**Decisiones arquitectónicas documentadas como ADRs:**

| ADR | Decisión | Justificación |
|---|---|---|
| ADR-01 | Arquitectura modular + pipeline | Extensibilidad — agregar renderers sin tocar el core |
| ADR-02 | Abstract Document Model | Independencia del formato de salida |
| ADR-03 | DSL declarativo en JSON | Flexibilidad para templates sin recompilar |
| ADR-04 | La librería no obtiene datos de APIs | Responsabilidad del cliente consumidor |

**El pipeline de transformación:**
```
Plantilla DSL (JSON)
  ↓ Parser
Abstract Syntax Tree (AST)
  ↓ Evaluador (variables, condicionales, loops)
Modelo abstracto del documento
  ↓ Motor de layout
  ↓ Renderer (ESC/POS | Texto | MAUI Preview)
Documento final
```

**Trazabilidad NB → CU:** NB-01 (desacoplamiento datos/diseño) originó CU-13 (cargar plantilla), CU-14 (validar plantilla), CU-15 (parsear DSL), etc.

**Slicing aplicado:** cada sprint entregó un slice funcional del pipeline. Sprint 1: pipeline mínimo funcional (input → output). Sprint 2: variables dinámicas. Sprint 3: condicionales. Sprint 4: loops y colecciones.

### 8.2 Ejemplo — API REST con autenticación (Node.js / Express)

**Estructura de `/docs/` para este tipo de proyecto:**

`00_contexto/vision-producto_v1.0.md`: plataforma de gestión de tareas multi-tenant.

`01_necesidades_negocio/`: NB-01 (usuarios necesitan organizar trabajo en equipo), NB-02 (equipos necesitan visibilidad del progreso), NB-03 (integraciones con calendarios externos).

`02_especificacion_funcional/casos-de-uso/`: CU-01 (registrar usuario), CU-02 (crear workspace), CU-03 (crear tarea), CU-04 (asignar tarea a usuario), CU-05 (marcar tarea como completada).

`05_arquitectura_tecnica/adr/`: ADR-01 (JWT para autenticación — sin sessions), ADR-02 (PostgreSQL — datos relacionales, ACID), ADR-03 (Redis para cache de sesiones activas), ADR-04 (REST sobre GraphQL — equipo no tiene experiencia con GraphQL).

`06_backlog-tecnico/product-backlog_v1.0.md`:

| US | Descripción | MoSCoW | Sprint |
|---|---|---|---|
| US-01 | Como usuario quiero registrarme con email y contraseña | Must | 1 |
| US-02 | Como usuario quiero crear un workspace | Must | 1 |
| US-03 | Como usuario quiero crear tareas en mi workspace | Must | 1 |
| US-04 | Como usuario quiero asignar tareas a miembros del equipo | Must | 2 |
| US-05 | Como usuario quiero recibir notificaciones por email | Should | 3 |
| US-06 | Como usuario quiero exportar mis tareas a CSV | Could | 4 |

**Walking Skeleton del Sprint 1:**
```
GET /health → 200 OK  (valida que el servidor responde)
POST /auth/register → crea usuario en DB
POST /auth/login → devuelve JWT
GET /workspaces → lista workspaces del usuario autenticado
POST /workspaces → crea workspace
GET /tasks?workspaceId=X → lista tareas
POST /tasks → crea tarea
```

### 8.3 Ejemplo — Librería npm (TypeScript)

**Contexto:** librería de validación de formularios para React.

**Decisiones de diseño en PROJECT-BRIEF.md:**
- Stack: TypeScript, React, Zod para schemas
- Bundler: tsup (genera CJS + ESM)
- Testing: Vitest
- Publicación: npm público

**Estructura de `/docs/` ajustada para una librería:**

`03_ux-ui/` se convierte en `03_developer-experience/` — ya no son pantallas de usuario sino la experiencia del desarrollador que consume la librería. Documenta la API pública, los tipos exportados y los ejemplos de uso.

`04_prompts_ai/` puede omitirse si la librería no usa LLMs — se documenta como "No aplica".

`10_developer_guide/` es el artefacto más importante para una librería — es la documentación que verán los consumidores en npm.

**Slicing aplicado:**

| Sprint | Slice | Qué entrega |
|---|---|---|
| Sprint 1 | Validación de campo de texto | `useField`, validación sync, mensaje de error |
| Sprint 2 | Validación de formulario completo | `useForm`, submit handling, reset |
| Sprint 3 | Validación async (ej: username disponible) | `useField` con validación async, debounce |
| Sprint 4 | Integración con Zod schemas | `zodResolver`, tipos inferidos automáticamente |

### 8.4 Ejemplo — Sistema con IA generativa

**Contexto:** chatbot de soporte técnico para un SaaS.

**Sección SA-04 (Prompts IA) activa:** en este tipo de sistema, `04_prompts_ai/` es una sección crítica — no opcional.

**Estructura de `04_prompts_ai/`:**

```
04_prompts_ai/
├── system-prompt_v1.0.md         ← prompt de sistema del asistente
├── few-shot-examples_v1.0.md     ← ejemplos de conversaciones correctas
├── guardrails_v1.0.md            ← qué no debe hacer el modelo
└── evaluation-criteria_v1.0.md   ← cómo medir la calidad del output
```

**Estructura del system prompt (patrón):**
```
Sos un asistente de soporte técnico para [nombre del producto].
Tu función es ayudar a los usuarios con [categorías de soporte].

RESTRICCIONES:
- Solo respondés preguntas relacionadas con [producto]
- Si no tenés información suficiente, decís: "[frase estándar de escalado]"
- No prometes fechas de resolución de bugs
- No accedés a datos de otros clientes

TONO: [formal/amigable/técnico]
FORMATO DE RESPUESTA: [estructura esperada]
```

**ADRs específicos para sistemas con IA:**

ADR-AI-01: qué modelo se usa y por qué (GPT-4 Turbo vs Claude 3 Sonnet vs modelo propio).

ADR-AI-02: estrategia de RAG si se usa (retrieval-augmented generation) — qué se indexa, con qué embedding model, qué vector DB.

ADR-AI-03: estrategia de fallback cuando el LLM no tiene información suficiente.

---

## 9. Mapa del conocimiento en /devs

Esta sección documenta la jerarquía del conocimiento disponible en la carpeta `/devs/` del repositorio original, organizada por tipo de conocimiento.

### 9.1 Jerarquía de documentos en /devs

```
devs/
├── intake/                          ← INPUTS de cada proyecto
│   ├── PROJECT-README-template.md   ← relevamiento del cliente (vacío)
│   └── PROJECT-BRIEF-template.md    ← delineamientos técnicos (vacío)
│
├── rules/                           ← CONTRATOS de construcción
│   ├── 00_rules.md                  ← cómo construir 00_contexto
│   ├── 01_rules.md                  ← cómo construir 01_necesidades
│   ├── 02_rules.md                  ← cómo construir 02_especificacion
│   ├── 03_rules.md                  ← cómo construir 03_ux-ui
│   ├── 04_rules.md                  ← cómo construir 04_prompts_ai
│   ├── 05_rules.md                  ← cómo construir 05_arquitectura
│   ├── 06_rules.md                  ← cómo construir 06_backlog
│   ├── 07_rules.md                  ← cómo construir 07_plan-sprint
│   ├── 08_rules.md                  ← cómo construir 08_calidad
│   └── 09_rules.md                  ← cómo construir 09_devops
│
├── orchestrator/                    ← MOTOR de la cadena
│   └── master-prompt.md             ← prompt orquestador + post-aprobación
│
├── metodologia-sdd-prompting/       ← METODOLOGÍA base (referencia)
│   ├── sdd.md                       ← fundamentos teóricos SDD
│   ├── metodologia-sdd-prompting.md ← ciclo de 6 fases + patrones
│   ├── resumen.md                   ← mapeo agente→documentación
│   └── referencias.md               ← links externos
│
├── especialidades/                  ← PERFILES de agentes (referencia)
│   ├── README.md                    ← índice de las 13 especialidades
│   ├── tabla-caracterizacion-agentes.md ← tabla completa con competencias
│   ├── AG-ROOT-arquitecto-soluciones.md
│   ├── AG-00-product-manager.md
│   ├── AG-01-analista-negocio.md
│   ├── AG-02-analista-funcional.md
│   ├── AG-03-especialista-dx.md
│   ├── AG-04-ingeniero-prompts.md
│   ├── AG-05-arquitecto-software.md
│   ├── AG-06-scrum-master.md
│   ├── AG-07-gestion-proyectos-agiles.md
│   ├── AG-08-ingeniero-qa.md
│   ├── AG-09-ingeniero-devops.md
│   ├── AG-10-technical-writer.md
│   └── AG-11-developer-advocate.md
│
├── refinamiento_docs/               ← REFINAMIENTO de docs existentes
│   ├── agentes-refinamiento-docs.md ← criterios por agente para revisión
│   └── prompt_refinamiento.md       ← prompt maestro de refinamiento
│
├── metodos-agiles/                  ← METODOLOGÍAS ÁGILES (referencia)
│   ├── guia-de-estudio.md           ← fundamentos de Scrum/Kanban/XP
│   └── metodologias-agiles-jira.md  ← referencia completa con Jira
│
└── arquitectura/                    ← ARQUITECTURA TÉCNICA (referencia)
    ├── guia-arquitectura-microservicios.md ← patrones, scaffolding, evolución
    └── guia-manejo-recursos.md      ← files, storage, compresión, firma digital
```

### 9.2 Qué leer según el objetivo

**Si querés entender la metodología desde cero:**
1. `devs/metodologia-sdd-prompting/sdd.md`
2. `devs/metodologia-sdd-prompting/resumen.md`
3. `devs/metodologia-sdd-prompting/metodologia-sdd-prompting.md`

**Si querés saber qué hace cada subagente:**
1. `devs/especialidades/README.md`
2. `devs/especialidades/tabla-caracterizacion-agentes.md`
3. `devs/especialidades/AG-[XX]-[nombre].md` — el del rol que te interesa

**Si querés adaptar las reglas de construcción:**
1. `devs/rules/XX_rules.md` — el de la sección que querés modificar

**Si querés refinar documentación ya generada:**
1. `devs/refinamiento_docs/agentes-refinamiento-docs.md`
2. `devs/refinamiento_docs/prompt_refinamiento.md`

**Si necesitás referencias de arquitectura para un proyecto específico:**
- Sistema con múltiples dominios → `devs/arquitectura/guia-arquitectura-microservicios.md`
- Sistema con manejo de archivos → `devs/arquitectura/guia-manejo-recursos.md`

**Si querés profundizar en metodologías ágiles:**
- Base conceptual → `devs/metodos-agiles/guia-de-estudio.md`
- Referencia completa con Jira → `devs/metodos-agiles/metodologias-agiles-jira.md`

### 9.3 Relación entre capas del conocimiento

```
[CONOCIMIENTO ESTRATÉGICO]
  SDD fundamentos (metodologia-sdd-prompting/sdd.md)
  Métodos ágiles (metodos-agiles/)
      ↓ informa
[CONOCIMIENTO METODOLÓGICO]
  Ciclo de 6 fases y patrones (metodologia-sdd-prompting.md)
  Especialidades de agentes (especialidades/)
      ↓ se concreta en
[INSTRUMENTOS OPERACIONALES]
  Reglas de construcción (rules/)
  Orquestador (orchestrator/)
  Templates de intake (intake/)
      ↓ produce
[OUTPUTS DE PROYECTO]
  Documentación SDD (/docs/)
  Código fuente (/src/)
  Tests (/tests/)
```

---

**Fin del documento — FUNDAMENTOS-Y-DEFINICIONES.md**
