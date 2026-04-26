# Reglas Constructivas — SA-03: UX/UI y Experiencia de Usuario

**Sección objetivo:** `docs/03_ux-ui/`  
**Rol del subagente:** UX Designer + Product Designer  
**Depende de:** SA-02 completo  
**Es upstream de:** SA-05, SA-10

---

## 1. Documentos a leer ANTES de escribir

1. `docs/02_especificacion_funcional/casos-de-uso/CU-*.md` — todos
2. `devs/intake/PROJECT-BRIEF.md` — sección capturas de pantalla
3. `docs/00_contexto/alcance-proyecto_v1.0.md`

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `flujos-de-usuario_v1.0.md` | Flujos principales por actor, happy paths y error paths | Sí |
| `wireframes-descripcion_v1.0.md` | Descripción textual de wireframes por pantalla/vista | Sí |
| `guia-experiencia_v1.0.md` | Principios UX, patrones de interacción, guía de estilo | Sí |

---

## 3. Reglas constructivas

- Los flujos de usuario deben originarse en los CUs de SA-02.
- Para cada pantalla principal describir: título, propósito, elementos UI, acciones disponibles, mensajes de error.
- Si PROJECT-BRIEF incluye capturas de pantalla, referenciarlas y anotar qué se conserva y qué cambia.
- Los wireframes son descripciones textuales (no imágenes) — suficiente para que SA-05 diseñe la API.
- Incluir estados de error, estados vacíos y estados de carga para cada pantalla clave.

---

## 4. Criterios de calidad

- [ ] ¿Cada actor del sistema tiene al menos un flujo de usuario documentado?
- [ ] ¿Las capturas de pantalla del intake están referenciadas y anotadas?
- [ ] ¿Los wireframes describen estados vacíos, de carga y de error?
