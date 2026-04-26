**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** README.md (SA-04 — Prompts IA)
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-04 via orquestador

---

# SA-04 — No aplica para este proyecto

El sistema **no incorpora funcionalidades basadas en LLMs ni en IA generativa** dentro del alcance del MVP. Las verificaciones realizadas sobre el intake confirman que:

- `PROJECT-README.md` Secciones 5 (RFs) no menciona inferencia con LLMs, generación de texto, embeddings, ni RAG.
- `PROJECT-BRIEF.md` Sección 6.4 ("Trabajo diferido para futuro escalamiento") menciona **explícitamente como exclusión del MVP**:
  - *"Pipeline ML de pre-clasificación de defectos en pavimento o estructuras."*
  - *"Detección automática de fotos borrosas o con mala exposición."*
- [alcance-proyecto](../00_contexto/alcance-proyecto_v1.0.md) Sección 2 ratifica:
  - **EX-03** — *"Pipeline de ML de pre-clasificación de defectos sobre las fotos"* → fuera del MVP.
  - **EX-04** — *"Detección automática de fotos borrosas o con mala exposición"* → fuera del MVP.

Por lo tanto, la sección `docs/04_prompts_ai/` no produce artefactos en esta versión de la documentación.

---

## Cuándo reabrir este documento

Si en una fase posterior el cliente prioriza alguna de las exclusiones EX-03 / EX-04 o cualquier otra funcionalidad basada en IA, este documento debe reabrirse y SA-04 debe ejecutarse para producir:

| Artefacto | Cuándo |
|---|---|
| `prompts-sistema_v1.0.md` | Si se incorpora cualquier interacción con LLM (resumen, clasificación, extracción) |
| `few-shot-examples_v1.0.md` | Si se necesita guiar al modelo con ejemplos de input/output |
| `guardrails_v1.0.md` | Si la integración con LLM expone riesgos a controlar (PII, alucinaciones, costos, prompt injection) |

Cada uno de esos artefactos debe cumplir las reglas de [`devs/rules/04_rules.md`](../../devs/rules/04_rules.md), incluyendo trazabilidad a CUs específicos.

---

## Disparadores que reabrirían SA-04

- Detección automática de defectos sobre fotos (clasificación o segmentación con CV/ML).
- Resúmenes automáticos de relevamientos para reportes.
- Generación de descripciones a partir de fotos.
- Búsqueda semántica sobre comentarios y descripciones.
- Asistente conversacional para jefes de área.
- Extracción asistida de coordenadas, identificadores u otra metadata desde fotos.

> Cualquiera de estos requerimientos exigiría además revisar [alcance-proyecto](../00_contexto/alcance-proyecto_v1.0.md) y, posiblemente, [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md).

---

**Fin del documento — README.md (SA-04)**
