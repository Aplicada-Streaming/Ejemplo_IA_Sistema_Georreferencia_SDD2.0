# Reglas Constructivas — SA-00: Contexto Estratégico

**Sección objetivo:** `docs/00_contexto/`  
**Rol del subagente:** Product Strategist + Arquitecto de Soluciones  
**Depende de:** PROJECT-README.md (Secciones 2, 3, 4, 8) + PROJECT-BRIEF.md (Sección 5)  
**Es upstream de:** SA-01, SA-06

---

## 1. Documentos a leer ANTES de escribir

En este orden exacto:

1. `devs/intake/PROJECT-README.md` — secciones: cliente, problema, qué construir, éxito
2. `devs/intake/PROJECT-BRIEF.md` — secciones: arquitectura, metodología, decisiones tomadas
3. Si existe: `docs/00_contexto/` — leer todos los archivos existentes para no duplicar

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `vision-producto_v1.0.md` | Visión, propuesta de valor, audiencia, métricas SMART | Sí |
| `alcance-proyecto_v1.0.md` | Qué incluye y qué excluye el proyecto, criterios de inclusión | Sí |
| `roadmap-producto_v1.0.md` | Fases, milestones, épicas de alto nivel, releases | Sí |
| `acuerdo-equipo_v1.0.md` | Roles, responsabilidades, DoR, DoD de alto nivel, normas | Sí |

---

## 3. Reglas constructivas

### 3.1 Visión del producto (`vision-producto_v1.0.md`)

- Usar el template de visión:  
  *"Para [quién], que [necesita], [nombre del producto] es [tipo de sistema] que [beneficio clave], a diferencia de [alternativa actual]."*
- La visión debe ser específica a este proyecto. Si aplica a cualquier producto, es demasiado vaga.
- Incluir sección "Propuesta de valor" con 3-5 bullets concretos.
- Incluir métricas de éxito SMART: específicas, medibles, alcanzables, relevantes, con fecha.
- NO incluir detalles de implementación técnica en este documento.

### 3.2 Alcance del proyecto (`alcance-proyecto_v1.0.md`)

- Listar explícitamente qué INCLUYE el sistema (en términos funcionales).
- Listar explícitamente qué EXCLUYE (con justificación de por qué queda fuera).
- Si hay fases/versiones, definir el alcance de cada una.
- Toda exclusión debe tener una razón: tiempo, presupuesto, complejidad, dependencia externa.

### 3.3 Roadmap (`roadmap-producto_v1.0.md`)

- Estructurar en Fases → Épicas → Milestones.
- Cada fase debe tener: objetivo, duración estimada, épicas incluidas, criterio de completitud.
- Si se eligió vertical slicing en PROJECT-BRIEF, las épicas deben ser funcionalidades completas.
- Si se eligió horizontal slicing, las épicas son capas técnicas.
- El roadmap conecta con el backlog (SA-06) y los sprints (SA-07) — no inventar números de sprint.

### 3.4 Acuerdo de equipo (`acuerdo-equipo_v1.0.md`)

- Incluir: roles del equipo, herramientas acordadas, canales de comunicación, cadencia de ceremonias.
- Definir DoR (Definition of Ready) de alto nivel: cuándo una historia está lista para sprintearse.
- Definir DoD (Definition of Done) de alto nivel: cuándo un ítem está terminado.
- Marcar como `[PENDIENTE]` los campos que el equipo aún no definió.

---

## 4. Reglas de formato

- Nomenclatura de archivos: `kebab-case_v1.0.md`.
- Todos los archivos deben tener header con: Proyecto, Documento, Versión, Estado, Fecha, Autor.
- Estado inicial: `Borrador` — el humano lo cambia a `Aprobado` tras revisión.
- Links internos: siempre en formato Markdown clickeable `[texto](ruta)`.
- Tablas para datos estructurados, párrafos para narrativa.
- Máximo 3 niveles de encabezado (H1, H2, H3).

---

## 5. Anti-patrones a evitar

| Anti-patrón | Por qué es un problema |
|---|---|
| Visión genérica ("sistema robusto y escalable") | No diferencia, no guía decisiones |
| Roadmap sin épicas trazables | Imposible priorizar el backlog |
| Alcance sin exclusiones | Todo parece Must Have, nada se puede recortar |
| DoD vacío o genérico | Los sprints nunca terminan claramente |
| Inventar restricciones no mencionadas en el intake | Contamina la cadena con datos falsos |

---

## 6. Criterios de calidad antes de pasar a SA-01

- [ ] ¿La visión puede diferenciarse de cualquier otro sistema de software?
- [ ] ¿El alcance tiene al menos 3 exclusiones explícitas con justificación?
- [ ] ¿El roadmap conecta fases con épicas identificables?
- [ ] ¿El acuerdo de equipo tiene DoR y DoD con al menos 3 criterios cada uno?
- [ ] ¿Todos los links internos entre los 4 documentos son válidos?
