# Guía de Usuario — Metodología SDD con Subagentes Orquestados

**Documento:** GUIA-DE-USUARIO.md
**Versión:** 1.0
**Fecha:** 2026-04-23
**Audiencia:** Desarrolladores y arquitectos que quieren usar esta metodología en proyectos nuevos

---

## Índice

1. [¿Qué es esto y para quién es?](#1-qué-es-esto-y-para-quién-es)
2. [Prerequisitos y estructura de archivos](#2-prerequisitos-y-estructura-de-archivos)
3. [Paso 1 — La conversación de idea en Claude web](#3-paso-1--la-conversación-de-idea-en-claude-web)
4. [Paso 2 — Generar los documentos de intake](#4-paso-2--generar-los-documentos-de-intake)
5. [Paso 3 — Revisar y completar los intakes](#5-paso-3--revisar-y-completar-los-intakes)
6. [Paso 4 — Preparar el repositorio](#6-paso-4--preparar-el-repositorio)
7. [Paso 5 — Ejecutar Claude Code (setup del template)](#7-paso-5--ejecutar-claude-code-setup-del-template)
8. [Paso 6 — Ejecutar el orquestador documental](#8-paso-6--ejecutar-el-orquestador-documental)
9. [Paso 7 — La compuerta de aprobación](#9-paso-7--la-compuerta-de-aprobación)
10. [Paso 8 — Fase de codeo y testing](#10-paso-8--fase-de-codeo-y-testing)
11. [Referencia rápida — Cheat sheet](#11-referencia-rápida--cheat-sheet)
12. [Troubleshooting frecuente](#12-troubleshooting-frecuente)

---

## 1. ¿Qué es esto y para quién es?

Esta metodología combina **Specification-Driven Development (SDD)** con una cadena de subagentes de IA especializados. El objetivo es pasar de una conversación libre con un cliente a documentación SDD completa y aprobada, antes de escribir una sola línea de código.

**Para quién es:**
- Desarrolladores o arquitectos que trabajan con clientes reales y necesitan documentar antes de codear
- Equipos pequeños (1–8 personas) que quieren disciplina de producto sin burocracia
- Cualquiera que use Claude + Claude Code y quiera sistematizar su forma de trabajo

**Qué resuelve:**
- El caos de empezar a codear antes de entender bien qué construir
- La pérdida de contexto entre lo que el cliente quiso decir y lo que el equipo entendió
- La falta de trazabilidad entre requerimientos, código y tests
- El trabajo manual de hacer de puente entre herramientas de IA

**Qué NO es:**
- Un proceso waterfall disfrazado — la documentación se genera rápido y en una cadena automatizada
- Una metodología para proyectos de 2 horas — tiene sentido para proyectos de semanas o meses
- Un reemplazo del criterio técnico — el humano sigue siendo el que aprueba antes de codear

---

## 2. Prerequisitos y estructura de archivos

### 2.1 Herramientas necesarias

| Herramienta | Versión mínima | Para qué |
|---|---|---|
| Claude.ai (cuenta) | Plan Pro o Team | Conversación de idea + orquestador |
| VS Code | Cualquier reciente | IDE principal |
| Claude Code (extensión VS Code) | Última disponible | Ejecutar el plan de setup y el codeo |
| Git | Cualquier | Control de versiones del proyecto |

### 2.2 Archivos de esta metodología

Estos son los archivos que necesitás tener disponibles para usar la metodología:

```
[estos archivos — el kit de la metodología]
├── INTAKE-HARVEST-PROMPT.md       ← prompt para cosechar la conversación
├── PLAN-RECONVERSION-METODOLOGIA.md ← plan que ejecuta Claude Code
└── CLAUDE-CODE-EXECUTION-PROMPT.md  ← prompt que le das a Claude Code
```

Una vez que Claude Code ejecuta el plan, tu repositorio tendrá:

```
[tu proyecto nuevo]
├── devs/
│   ├── intake/
│   │   ├── PROJECT-README.md        ← llenás vos con info del cliente
│   │   └── PROJECT-BRIEF.md         ← llenás vos con info técnica
│   ├── rules/
│   │   ├── 00_rules.md              ← reglas para subagente de contexto
│   │   ├── 01_rules.md              ← reglas para subagente de necesidades
│   │   ├── 02_rules.md              ← ... y así hasta 09_rules.md
│   │   └── [08 archivos más]
│   └── orchestrator/
│       └── master-prompt.md         ← el prompt orquestador completo
└── docs/
    └── .gitkeep                     ← carpeta vacía, la llena el orquestador
```

### 2.3 Estructura final de `/docs/` (la genera el orquestador)

```
docs/
├── README.md
├── 00_contexto/
│   ├── vision-producto_v1.0.md
│   ├── alcance-proyecto_v1.0.md
│   ├── roadmap-producto_v1.0.md
│   └── acuerdo-equipo_v1.0.md
├── 01_necesidades_negocio/
│   ├── necesidades-negocio_v1.0.md
│   └── necesidades-de-negocio/
│       ├── NB-01-[nombre]_v1.0.md
│       └── NB-XX-[nombre]_v1.0.md
├── 02_especificacion_funcional/
│   ├── especificacion-funcional_v1.0.md
│   ├── modelo-datos-conceptual_v1.0.md
│   ├── casos-de-uso/
│   │   └── CU-XX-[nombre]_v1.0.md
│   └── reglas-de-negocio/
│       └── RN-XX-[nombre]_v1.0.md
├── 03_ux-ui/
├── 04_prompts_ai/
├── 05_arquitectura_tecnica/
│   ├── arquitectura-solucion_v1.0.md
│   ├── contratos-interfaces_v1.0.md
│   ├── modelo-datos-logico_v1.0.md
│   └── adr/
├── 06_backlog-tecnico/
│   ├── product-backlog_v1.0.md
│   ├── backlog-tecnico_v1.0.md
│   └── user-stories/
├── 07_plan-sprint/
├── 08_calidad_y_pruebas/
└── 09_devops/
```

---

## 3. Paso 1 — La conversación de idea en Claude web

### Qué hacés en este paso

Abrís un chat nuevo en [claude.ai](https://claude.ai) y hablás con Claude sobre el proyecto. Sin estructura, sin formato, con la naturalidad de una conversación.

### Qué incluir en la conversación

Hablá sobre todo lo que sabés del proyecto. No hace falta un orden, pero intentá cubrir:

- **Quién es el cliente** — nombre, industria, contexto del negocio, qué hace hoy
- **El problema que tiene** — qué le duele, qué no funciona, qué es lento o costoso
- **Qué quiere construir** — la idea del sistema, qué funcionalidades imagina
- **Restricciones** — plataforma, tecnología preferida, tiempo disponible, equipo
- **Decisiones ya tomadas** — si ya eligió alguna tecnología o arquitectura
- **La metodología** — si quiere sprints con entregables funcionales (vertical slicing) o por capas

### Cómo subir capturas de pantalla

Si el cliente te mostró pantallas de un sistema existente, prototipos en Figma, diagramas de arquitectura o cualquier imagen relevante, subila directamente en el chat de Claude. Claude las va a ver, describir e incorporar al análisis.

Para subir una imagen en Claude web: arrastrala al chat o usá el ícono de adjunto.

### Tip: cómo sacar más de la conversación

Hacé preguntas abiertas a Claude para profundizar:

```
¿Qué alternativas de arquitectura tendría este sistema?
¿Vertical slicing o horizontal slicing tiene más sentido para este caso?
¿Cuáles serían los riesgos técnicos más importantes?
¿Qué microservicios auxiliares podría necesitar?
```

Claude va a plantear alternativas, pros y contras. Esas discusiones quedan en el historial y el próximo prompt las va a cosechar.

### Cuándo terminar esta fase

Cuando sientas que la conversación cubrió lo suficiente para arrancar. No hace falta que esté todo resuelto — los `[REQUIERE_INFO]` del siguiente paso van a marcar lo que falta.

---

## 4. Paso 2 — Generar los documentos de intake

### Qué hacés en este paso

Al final de la conversación de idea, pegás el contenido del archivo **`INTAKE-HARVEST-PROMPT.md`** como último mensaje del chat.

### Cómo ejecutarlo

1. Abrí el archivo `INTAKE-HARVEST-PROMPT.md`
2. Copiá todo el contenido de la **Sección 1** (el bloque que empieza con "Basándote en TODO lo que conversamos...")
3. Pegalo como tu último mensaje en el chat de Claude
4. Envialo

### Qué produce

Claude va a generar dos bloques de código markdown:

**Bloque 1 — `PROJECT-README.md`**: con la información del cliente, el problema, requerimientos funcionales y no funcionales, restricciones y definición de éxito.

**Bloque 2 — `PROJECT-BRIEF.md`**: con la arquitectura propuesta, capturas de pantalla referenciadas, decisiones de diseño tomadas durante la conversación, metodología de ejecución elegida e infraestructura.

Al final de los bloques, Claude agrega:

- **CAMPOS `[REQUIERE_INFO]`**: lista exacta de lo que faltó en la conversación
- **DECISIONES IMPLÍCITAS**: inferencias que hizo, para que puedas confirmarlas o corregirlas

### Guardar los outputs

Copiá cada bloque de código en un archivo separado:

```
Bloque 1 → devs/intake/PROJECT-README.md
Bloque 2 → devs/intake/PROJECT-BRIEF.md
```

Si el repositorio todavía no tiene la carpeta `devs/intake/`, crearla manualmente antes de guardar.

---

## 5. Paso 3 — Revisar y completar los intakes

### Qué hacés en este paso

Abrís ambos archivos en VS Code y:

1. **Revisás las DECISIONES IMPLÍCITAS** — confirmás que lo que Claude infirió es correcto. Si algo no es exacto, lo corregís directamente en el archivo.

2. **Completás los `[REQUIERE_INFO]`** — buscás todas las ocurrencias de `[REQUIERE_INFO]` en ambos archivos y las reemplazás con la información real.

3. **Verificás las capturas** — si subiste imágenes en la conversación y el BRIEF las referencia en la tabla SS-XX, asegurate de copiar los archivos a `/devs/assets/screenshots/` con los nombres correspondientes.

### Campos más comunes que quedan como `[REQUIERE_INFO]`

- Stack tecnológico (si no se discutió)
- Cantidad de desarrolladores disponibles
- Velocidad estimada en story points
- Ambientes de deploy requeridos
- Restricciones de seguridad específicas

### Criterio para avanzar al siguiente paso

Los dos archivos están listos cuando:
- No hay ningún `[REQUIERE_INFO]` crítico sin completar (los opcionales pueden esperar)
- Las DECISIONES IMPLÍCITAS fueron confirmadas o corregidas
- La sección de metodología (vertical/horizontal slicing) está explícita en el BRIEF

---

## 6. Paso 4 — Preparar el repositorio

### Qué hacés en este paso

Copiás los archivos de la metodología al repositorio del proyecto nuevo.

### Estructura que debe tener el repo antes de ejecutar Claude Code

```
[tu-proyecto-nuevo]/
├── PLAN-RECONVERSION-METODOLOGIA.md    ← copiarlo a la raíz
├── CLAUDE-CODE-EXECUTION-PROMPT.md     ← copiarlo a la raíz
├── devs/
│   └── intake/
│       ├── PROJECT-README.md           ← el que generaste en Paso 2
│       └── PROJECT-BRIEF.md            ← el que generaste en Paso 2
└── docs/
    └── .gitkeep                        ← crear esta carpeta vacía
```

> Si el repo ya tiene código fuente o una estructura existente, no hay problema — el plan de Claude Code solo crea carpetas dentro de `/devs/` y no toca nada más.

### Crear el .gitkeep de docs

```bash
mkdir -p docs
touch docs/.gitkeep
git add docs/.gitkeep
```

---

## 7. Paso 5 — Ejecutar Claude Code (setup del template)

### Qué hacés en este paso

Abrís el proyecto en VS Code y ejecutás el prompt de setup en Claude Code. Esto crea toda la infraestructura de reglas y el orquestador.

### Ejecución paso a paso

1. Abrí VS Code con la carpeta del proyecto como workspace
2. Abrí Claude Code (`Ctrl+Shift+P` → buscar "Claude Code" o desde la barra lateral)
3. Abrí el archivo `CLAUDE-CODE-EXECUTION-PROMPT.md`
4. Copiá **todo** el contenido de la **Sección 2** del archivo
5. Pegalo en el chat de Claude Code
6. Envialo

### Qué hace Claude Code

Claude Code va a:

1. Leer `PLAN-RECONVERSION-METODOLOGIA.md` completo
2. Pedirte confirmación antes de crear archivos (esperá su mensaje y respondé "SÍ")
3. Crear `devs/intake/PROJECT-README-template.md` y `PROJECT-BRIEF-template.md`
4. Crear `devs/rules/00_rules.md` hasta `devs/rules/09_rules.md`
5. Crear `devs/orchestrator/master-prompt.md`
6. Verificar que los 13 archivos existen con el contenido correcto
7. Generar un reporte final de completitud

### Qué confirmar en el reporte final

El reporte de Claude Code debe mostrar:

```
✓ devs/intake/PROJECT-README-template.md
✓ devs/intake/PROJECT-BRIEF-template.md
✓ devs/rules/00_rules.md  ... (hasta 09)
✓ devs/orchestrator/master-prompt.md
✓ /docs/ — sin cambios
```

Si algún archivo falta o tiene pocas líneas, usá los prompts de troubleshooting del archivo `CLAUDE-CODE-EXECUTION-PROMPT.md` (Sección 4).

---

## 8. Paso 6 — Ejecutar el orquestador documental

### Qué hacés en este paso

Abrís un **proyecto** en Claude.ai (no un chat nuevo — un Proyecto), cargás los archivos de reglas como knowledge y ejecutás el master prompt.

### 8.1 Crear el proyecto en Claude.ai

1. En Claude.ai, hacé click en **"Proyectos"** → **"Nuevo proyecto"**
2. Dale un nombre descriptivo: `[nombre del proyecto] — SDD`
3. En la configuración del proyecto, cargá como knowledge estos archivos:
   - `devs/rules/00_rules.md` hasta `devs/rules/09_rules.md` (los 10 archivos)
   - `devs/intake/PROJECT-README.md`
   - `devs/intake/PROJECT-BRIEF.md`

> Por qué un Proyecto: el Proyecto mantiene el knowledge persistente entre chats. Los rules.md son las instrucciones que los subagentes necesitan leer en cada sección — deben estar siempre disponibles.

### 8.2 Ejecutar el master prompt

1. Abrí `devs/orchestrator/master-prompt.md`
2. Copiá el contenido del bloque "El Prompt" (la sección entre las comillas del bloque de código)
3. Pegalo en un chat nuevo dentro del Proyecto
4. Envialo

### 8.3 Qué sucede durante la ejecución

Claude va a:

1. Confirmar que leyó los intakes con un resumen de 3 puntos
2. Ejecutar SA-00 → generar los 4 documentos de `/docs/00_contexto/`
3. Ejecutar SA-01 → generar los documentos de `/docs/01_necesidades_negocio/`
4. Continuar con SA-02 → SA-09 siguiendo el grafo de dependencias
5. En cada transición entre subagentes, describir qué leyó y qué va a producir

### 8.4 Guardar los outputs en el repo

Claude genera los documentos como bloques de código markdown. Para cada archivo:

1. Copiá el bloque de código
2. Creá el archivo en la ruta indicada dentro de `/docs/`
3. Guardalo en el repositorio

> Tip: si la cadena es larga, podés pedirle a Claude Code que cree los archivos pegando el output de Claude en el chat de Claude Code con la instrucción: *"Creá el archivo [ruta] con este contenido: [pegar]"*

---

## 9. Paso 7 — La compuerta de aprobación

### Qué es

Al finalizar SA-09, el orquestador presenta un **Informe de Completitud** y te pide aprobación explícita antes de continuar con cualquier fase de codeo.

### El informe incluye

- Tabla de todos los artefactos generados con su estado
- Campos `[REQUIERE_INFO]` que quedaron sin completar, con su ubicación exacta
- Mapa de trazabilidad: NB → CU → US → Sprint (eslabones completos e incompletos)
- Puntuación de completitud por sección (1-10)

### Cómo revisar la documentación

Antes de aprobar, revisá al menos:

**Revisión mínima (30 minutos):**
- Leer `docs/00_contexto/vision-producto_v1.0.md` — ¿refleja lo que quiere el cliente?
- Leer `docs/01_necesidades_negocio/necesidades-negocio_v1.0.md` — ¿están todos los problemas?
- Revisar la tabla del backlog `docs/06_backlog-tecnico/product-backlog_v1.0.md` — ¿el Sprint 1 tiene sentido?
- Verificar el Sprint 1 `docs/07_plan-sprint/plan-iteracion_sprint-01_v1.0.md` — ¿es alcanzable?

**Revisión completa (recomendada para proyectos grandes):**
- Leer todos los documentos de cada sección
- Verificar que los CUs tienen criterios de aceptación Given/When/Then
- Confirmar que la arquitectura propuesta coincide con lo que decidiste en el BRIEF
- Revisar que el DoD tiene criterios técnicos y funcionales claros

### Cómo sugerir modificaciones

Si algo no está bien, respondé en el mismo chat describiendo qué cambiar:

```
Modificaciones antes de aprobar:
1. En vision-producto_v1.0.md, la propuesta de valor dice X pero debería decir Y
2. Falta la NB de [tema que se discutió en la conversación inicial]
3. El Sprint 1 tiene demasiadas US — reducir a solo US-01 y US-02
```

Claude va a aplicar las modificaciones y presentarte los documentos actualizados.

### Aprobar

Cuando todo está correcto, escribí:

```
APROBADO
```

Desde ese momento, el orquestador habilita la fase de codeo.

---

## 10. Paso 8 — Fase de codeo y testing

### Qué cambia después de la aprobación

La documentación en `/docs/` pasa de estado `Borrador` a `Aprobado`. Ese cambio de estado es el contrato entre la fase documental y la fase de construcción.

### Cómo iniciar la fase de codeo

En el mismo Proyecto de Claude (o en Claude Code), usá el prompt post-aprobación que está al final de `devs/orchestrator/master-prompt.md`:

```
La documentación fue aprobada. Iniciá la fase de codeo.

[... el prompt completo está en devs/orchestrator/master-prompt.md, sección "Prompt para fase de codeo"]
```

### El patrón de trabajo en cada sprint

Para cada User Story del Sprint 1:

1. Abrí el archivo `docs/06_backlog-tecnico/user-stories/US-XX-[nombre].md`
2. Pegalo en Claude Code con la instrucción: *"Implementá esta User Story"*
3. Claude Code lee el CU relacionado, la arquitectura y el DoD
4. Genera el código, los tests y verifica el DoD
5. Confirmá el resultado antes de pasar a la siguiente US

### Cómo mantener la trazabilidad durante el codeo

Cada vez que el código implementa un CU, actualizá el estado en el archivo correspondiente:

```markdown
**Estado:** Implementado
**Sprint:** Sprint 01
**Commit:** [hash del commit]
```

Esto mantiene viva la cadena NB → CU → US → código → test.

### Cuándo volver a la fase documental

Si durante el codeo surge un requerimiento nuevo o cambia algo significativo:

1. Actualizá primero el documento relevante en `/docs/` (el CU, la US, o la arquitectura)
2. Luego implementá el cambio en código
3. Nunca al revés — el código siempre sigue a la especificación

---

## 11. Referencia rápida — Cheat sheet

### El flujo completo en 8 pasos

```
[CHAT CLAUDE WEB]
  1. Conversación libre sobre el proyecto
     → subís capturas, discutís alternativas, tomás decisiones
  2. Pegás INTAKE-HARVEST-PROMPT.md al final
     → Claude genera PROJECT-README.md y PROJECT-BRIEF.md

[VOS - EDITOR]
  3. Revisás y completás los [REQUIERE_INFO]
     → guardás en devs/intake/
  4. Copiás los archivos del kit al repo
     → PLAN + CLAUDE-CODE-EXECUTION-PROMPT en la raíz

[CLAUDE CODE - VS CODE]
  5. Pegás CLAUDE-CODE-EXECUTION-PROMPT (Sección 2)
     → crea devs/rules/ y devs/orchestrator/

[PROYECTO CLAUDE WEB]
  6. Cargás rules/ como knowledge del proyecto
     → ejecutás master-prompt.md
     → Claude genera /docs/ completo

[VOS - REVISIÓN]
  7. Revisás la documentación
     → sugerís cambios o escribís APROBADO

[CLAUDE CODE - VS CODE]
  8. Ejecutás el prompt de codeo post-aprobación
     → US por US, con tests y DoD verificado
```

### Archivos del kit y qué hace cada uno

| Archivo | Cuándo se usa | Qué produce |
|---|---|---|
| `INTAKE-HARVEST-PROMPT.md` | Final de conversación con cliente en Claude web | PROJECT-README.md + PROJECT-BRIEF.md |
| `PLAN-RECONVERSION-METODOLOGIA.md` | Lo lee Claude Code (no lo pegás vos) | — es el plan de ejecución |
| `CLAUDE-CODE-EXECUTION-PROMPT.md` | Se pega en Claude Code una vez por proyecto | devs/rules/ + devs/orchestrator/ |
| `devs/orchestrator/master-prompt.md` | Se pega en Claude Proyecto | /docs/ completo |

### Comandos útiles de verificación

```bash
# Ver todos los archivos del template generados
find devs/intake devs/rules devs/orchestrator -name "*.md" | sort

# Ver toda la documentación generada por el orquestador
find docs -name "*.md" | sort

# Contar documentos generados
find docs -name "*.md" | wc -l

# Ver campos pendientes en toda la documentación
grep -r "REQUIERE_INFO" docs/
```

---

## 12. Troubleshooting frecuente

### "Claude no encuentra el contexto del proyecto"

Si al ejecutar el master prompt Claude dice que no tiene información del proyecto:

Verificar que los archivos `PROJECT-README.md` y `PROJECT-BRIEF.md` están cargados como knowledge del Proyecto en Claude.ai. En la configuración del proyecto, ir a "Archivos" y confirmar que aparecen.

### "El orquestador genera documentos con [REQUIERE_INFO] en campos críticos"

Dos causas posibles:

1. Los intakes no están completos — revisar la Sección 7 (Restricciones conocidas) del README y la Sección 5 (Metodología) del BRIEF.
2. La conversación inicial no cubrió ese tema — volver al chat y agregar la información, luego volver a ejecutar el harvest prompt.

### "Claude Code trunca el contenido de los archivos"

Usar este prompt de corrección en Claude Code:

```
El archivo [ruta] está incompleto — faltan secciones desde [sección X].
Relee la Sección [X.X] del PLAN-RECONVERSION-METODOLOGIA.md
y recreá el archivo completo sin abreviar ni resumir nada.
```

### "La cadena de subagentes se detiene a mitad"

Si Claude para entre subagentes sin terminar, usá este prompt de reanudación:

```
Continuá con SA-[XX]. Ya completaste SA-00 hasta SA-[XX-1].
Leé devs/rules/XX_rules.md y los documentos upstream requeridos.
Producí todos los artefactos marcados como "Obligatorio: Sí".
```

### "La arquitectura generada no coincide con lo que quería"

Esto suele indicar que `PROJECT-BRIEF.md` Sección 1 (Arquitectura propuesta) o Sección 4 (Decisiones tomadas) están incompletos. Antes de que SA-05 se ejecute, añadís las restricciones explícitas al BRIEF y reiniciás desde SA-05:

```
Necesito reiniciar SA-05 con información adicional de arquitectura.
Lee la nueva versión de devs/intake/PROJECT-BRIEF.md Sección 1 y 4,
y regenerá todos los artefactos de docs/05_arquitectura_tecnica/
```

### "La aprobación fue dada pero el orquestador no arranca el codeo"

El prompt de codeo es independiente del prompt documental — hay que ejecutarlo por separado. Está en `devs/orchestrator/master-prompt.md` al final, en la sección "Prompt para fase de codeo (post-aprobación)".

---

**Fin del documento — GUIA-DE-USUARIO.md**
