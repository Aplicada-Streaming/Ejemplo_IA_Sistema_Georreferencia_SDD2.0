# Reglas Constructivas — SA-07: Plan de Sprints

**Sección objetivo:** `docs/07_plan-sprint/`  
**Rol del subagente:** Scrum Master + Tech Lead  
**Depende de:** SA-06 completo + PROJECT-BRIEF.md (Sección 5 — metodología)  
**Es upstream de:** SA-08

---

## 1. Documentos a leer ANTES de escribir

1. `docs/06_backlog-tecnico/product-backlog_v1.0.md`
2. `docs/06_backlog-tecnico/user-stories/US-*.md` — todos los Must Have y Should Have
3. `devs/intake/PROJECT-BRIEF.md` — metodología, velocidad estimada, cantidad de sprints
4. `docs/00_contexto/acuerdo-equipo_v1.0.md`

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `plan-iteracion_sprint-01_v1.0.md` | Plan del Sprint 1 con US-XX comprometidas | Sí |
| `plan-iteracion_sprint-02_v1.0.md` | Plan del Sprint 2 | Según proyecto |
| `plan-iteracion_sprint-XX_v1.0.md` | Tantos como el roadmap defina | Según proyecto |
| `tracking-velocity_v1.0.md` | Template de tracking de velocidad | Sí |

---

## 3. Reglas constructivas

### 3.1 Estructura de cada plan de sprint

```markdown
# Sprint XX — [Nombre del sprint]
**Objetivo del sprint:** [Una oración que define el valor entregado]
**Fechas:** [Inicio → Fin]
**Velocidad comprometida:** [story points]

## US comprometidas
| US | Descripción | Puntos | Owner | Estado |
...

## Criterio de éxito del sprint
## Riesgos identificados
## Dependencias con otros sprints
```

### 3.2 Estrategia de slicing (leer de PROJECT-BRIEF)

**Si vertical slicing:** cada sprint debe entregar al menos una funcionalidad completa demostrable al usuario. El Sprint 1 siempre incluye el "esqueleto funcional mínimo" (login + una feature end-to-end).

**Si horizontal slicing:** cada sprint entrega una capa completa. Documentar qué capa y qué integración habilita.

### 3.3 Distribución del backlog

- Sprint 1: Must Have de mayor riesgo técnico.
- Sprints intermedios: Must Have restantes + Should Have de alto valor.
- Último sprint antes del release: estabilización, docs, QA final.

---

## 4. Criterios de calidad

- [ ] ¿El Sprint 1 tiene un objetivo claro y demostrable?
- [ ] ¿La suma de puntos por sprint respeta la velocidad estimada en el BRIEF?
- [ ] ¿Todos los Must Have están asignados a algún sprint?
