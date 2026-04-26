# Reglas Constructivas — SA-06: Backlog Técnico

**Sección objetivo:** `docs/06_backlog-tecnico/`  
**Rol del subagente:** Product Owner + Scrum Master  
**Depende de:** SA-02 completo + SA-05 completo + SA-00 (roadmap)  
**Es upstream de:** SA-07

---

## 1. Documentos a leer ANTES de escribir

1. `docs/02_especificacion_funcional/casos-de-uso/CU-*.md` — todos
2. `docs/05_arquitectura_tecnica/arquitectura-solucion_v1.0.md`
3. `docs/00_contexto/roadmap-producto_v1.0.md`
4. `docs/00_contexto/acuerdo-equipo_v1.0.md` — DoR y DoD

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `product-backlog_v1.0.md` | Lista priorizada de User Stories con MoSCoW | Sí |
| `user-stories/US-01-[nombre]_v1.0.md` | Una US por archivo | Sí (mínimo 1 por épica) |
| `backlog-tecnico_v1.0.md` | Tareas técnicas no funcionales (BT-XX) | Sí |
| `definition-of-ready_v1.0.md` | Criterios DoR formales | Sí |

---

## 3. Reglas constructivas

### 3.1 User Stories

Formato obligatorio:

```
Como [rol/actor],
quiero [acción concreta],
para [beneficio o razón].
```

Cada US debe tener: código (US-XX), épica de pertenencia, prioridad MoSCoW, puntos de historia estimados, CUs relacionados, criterios de aceptación (Given/When/Then), DoR checklist.

### 3.2 Priorización MoSCoW

- **Must Have:** sin esto el MVP no cumple su propósito mínimo.
- **Should Have:** importante pero el MVP funciona sin esto.
- **Could Have:** agrega valor pero puede esperar a v2.
- **Won't Have (v1):** documentado para no olvidar, fuera de scope.

Regla: si todo es Must Have, la priorización es incorrecta. Forzar que Must Have sea ≤ 60% del backlog total.

### 3.3 Backlog técnico

Las BT-XX son tareas de infraestructura, setup, deuda técnica o trabajo no funcional. No son User Stories. Cada BT tiene: descripción, justificación, estimación, sprint sugerido.

---

## 4. Criterios de calidad

- [ ] ¿Cada CU de SA-02 tiene al menos una US en el backlog?
- [ ] ¿El Must Have es ≤ 60% del backlog?
- [ ] ¿Cada US tiene criterios de aceptación verificables?
- [ ] ¿Existe el documento de Definition of Ready?
