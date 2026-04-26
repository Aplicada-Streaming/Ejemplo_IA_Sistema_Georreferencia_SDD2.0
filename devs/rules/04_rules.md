# Reglas Constructivas — SA-04: Prompts de IA

**Sección objetivo:** `docs/04_prompts_ai/`  
**Rol del subagente:** Prompt Engineer + ML Practitioner  
**Depende de:** SA-02 completo  
**Es upstream de:** SA-10

> Esta sección solo aplica si el sistema tiene funcionalidades que usan LLMs o IA generativa.
> Si el sistema no usa IA, crear el archivo `docs/04_prompts_ai/README.md` con la nota "No aplica para este proyecto".

---

## 1. Documentos a leer ANTES de escribir

1. `docs/02_especificacion_funcional/especificacion-funcional_v1.0.md`
2. `docs/02_especificacion_funcional/casos-de-uso/CU-*.md` — los relacionados con funcionalidades IA
3. `devs/intake/PROJECT-README.md` — requerimientos funcionales

---

## 2. Artefactos a producir (si aplica)

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `prompts-sistema_v1.0.md` | System prompts para cada funcionalidad IA | Según proyecto |
| `few-shot-examples_v1.0.md` | Ejemplos de input/output para cada prompt | Según proyecto |
| `guardrails_v1.0.md` | Restricciones, filtros, casos de rechazo | Según proyecto |

---

## 3. Reglas constructivas

- Cada prompt debe trazar a un CU específico.
- Incluir: objetivo del prompt, inputs esperados, output esperado, ejemplos (few-shot), casos de fallo.
- Los guardrails definen qué NO debe hacer el modelo y cómo manejar edge cases.
- Documentar el modelo objetivo (GPT-4, Claude 3, etc.) si ya está decidido.
