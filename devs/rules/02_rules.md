# Reglas Constructivas — SA-02: Especificación Funcional

**Sección objetivo:** `docs/02_especificacion_funcional/`  
**Rol del subagente:** Analista Funcional Senior + Arquitecto de Dominio  
**Depende de:** SA-01 completo + SA-00 completo  
**Es upstream de:** SA-03, SA-04, SA-05, SA-06

---

## 1. Documentos a leer ANTES de escribir

1. `docs/01_necesidades_negocio/necesidades-negocio_v1.0.md`
2. `docs/01_necesidades_negocio/necesidades-de-negocio/NB-*.md` — todos
3. `docs/00_contexto/vision-producto_v1.0.md`
4. `docs/00_contexto/alcance-proyecto_v1.0.md`
5. `devs/intake/PROJECT-README.md` — secciones: requerimientos funcionales, actores

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `especificacion-funcional_v1.0.md` | Resumen, actores, flujo general del sistema | Sí |
| `casos-de-uso/CU-01-[nombre]_v1.0.md` | Un caso de uso por archivo | Sí (mínimo 5) |
| `reglas-de-negocio/RN-01-[nombre]_v1.0.md` | Una regla de negocio por archivo | Sí (mínimo 3) |
| `modelo-datos-conceptual_v1.0.md` | Entidades principales, atributos clave, relaciones | Sí |

---

## 3. Reglas constructivas

### 3.1 Casos de uso

Cada CU describe una interacción completa entre un actor y el sistema. Estructura obligatoria:

```markdown
# CU-XX — [Nombre del caso de uso]
**Código:** CU-XX
**Actor primario:** [quién inicia]
**Precondiciones:** [qué debe ser verdad antes]
**Postcondiciones:** [qué es verdad al terminar]
**Flujo principal:** [pasos numerados]
**Flujos alternativos:** [variaciones y errores]
**Reglas de negocio relacionadas:** [RN-XX]
**Trazabilidad:** [NB-XX que origina este CU]
**Criterios de aceptación:** [Given/When/Then]
```

### 3.2 Reglas de negocio

Cada RN es una restricción o condición de negocio que el sistema debe cumplir. No es un requerimiento técnico.

```markdown
# RN-XX — [Nombre de la regla]
**Descripción:** [Qué restricción impone]
**Origen:** [NB-XX o decisión de negocio]
**CUs afectados:** [Lista de CU-XX]
**Prioridad:** [Alta / Media / Baja]
**Ejemplos:** [Casos concretos de aplicación y violación]
```

### 3.3 Modelo de datos conceptual

- Solo entidades de dominio, no tablas de base de datos.
- Atributos con tipo conceptual (texto, número, fecha, booleano) — sin tipos técnicos.
- Relaciones con cardinalidad (1:1, 1:N, N:M).
- El modelo surge de los CUs, no al revés.

### 3.4 Trazabilidad

- Cada CU debe trazar a al menos una NB.
- Cada NB debe tener al menos un CU que la resuelva.
- Actualizar la columna "Trazabilidad CU" en los archivos NB correspondientes.

---

## 4. Criterios de calidad antes de pasar a SA-03/04/05

- [ ] ¿Cada CU tiene criterios de aceptación en formato Given/When/Then?
- [ ] ¿Cada NB tiene al menos un CU que la resuelve?
- [ ] ¿Todas las entidades del modelo de datos tienen al menos un CU que las manipula?
- [ ] ¿Las reglas de negocio son de dominio (no técnicas)?
