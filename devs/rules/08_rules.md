# Reglas Constructivas — SA-08: Calidad y Pruebas

**Sección objetivo:** `docs/08_calidad_y_pruebas/`  
**Rol del subagente:** QA Lead + Test Architect  
**Depende de:** SA-02 completo + SA-05 completo + SA-06 completo  
**Es upstream de:** SA-09

---

## 1. Documentos a leer ANTES de escribir

1. `docs/02_especificacion_funcional/casos-de-uso/CU-*.md` — criterios de aceptación
2. `docs/05_arquitectura_tecnica/contratos-interfaces_v1.0.md`
3. `docs/06_backlog-tecnico/product-backlog_v1.0.md`

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `estrategia-testing_v1.0.md` | Pirámide de testing, tipos, cobertura objetivo | Sí |
| `definition-of-done_v1.0.md` | DoD canónico con criterios técnicos y de negocio | Sí |
| `casos-de-prueba/TC-01-[nombre]_v1.0.md` | Test cases para CUs críticos | Mínimo 1 por CU crítico |
| `matriz-cobertura_v1.0.md` | Tabla CU × tipo de test × prioridad | Sí |

---

## 3. Reglas constructivas

### 3.1 Pirámide de testing

Definir la proporción objetivo: típicamente 70% unit / 20% integración / 5% snapshot / 5% E2E. Ajustar según el tipo de sistema.

### 3.2 Definition of Done

El DoD debe incluir criterios en tres dimensiones:
- **Código:** cobertura mínima, sin warnings, linting pasa.
- **Funcional:** todos los criterios de aceptación del CU verificados.
- **Proceso:** code review aprobado, documentación actualizada, build pasa en CI.

### 3.3 Test cases

Cada TC traza a un CU. Estructura: ID, CU relacionado, precondición, datos de entrada, pasos, resultado esperado, resultado obtenido (se completa al ejecutar), estado (pendiente/pass/fail).

---

## 4. Criterios de calidad

- [ ] ¿El DoD tiene criterios en las tres dimensiones (código, funcional, proceso)?
- [ ] ¿Cada CU crítico tiene al menos un test case documentado?
- [ ] ¿La pirámide de testing tiene porcentajes definidos y justificados?
