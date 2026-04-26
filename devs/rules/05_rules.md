# Reglas Constructivas — SA-05: Arquitectura Técnica

**Sección objetivo:** `docs/05_arquitectura_tecnica/`  
**Rol del subagente:** Arquitecto de Software Senior  
**Depende de:** SA-02 completo + SA-03 completo + PROJECT-BRIEF.md (Secciones 1, 3, 4, 7)  
**Es upstream de:** SA-06, SA-08, SA-09

---

## 1. Documentos a leer ANTES de escribir

1. `docs/02_especificacion_funcional/especificacion-funcional_v1.0.md`
2. `docs/02_especificacion_funcional/modelo-datos-conceptual_v1.0.md`
3. `docs/03_ux-ui/flujos-de-usuario_v1.0.md`
4. `devs/intake/PROJECT-BRIEF.md` — secciones: arquitectura propuesta, decisiones tomadas, restricciones técnicas
5. `docs/00_contexto/alcance-proyecto_v1.0.md`

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `arquitectura-solucion_v1.0.md` | Visión técnica, componentes, flujos, decisiones ADR | Sí |
| `contratos-interfaces_v1.0.md` | Contratos de API, eventos, interfaces entre componentes | Sí |
| `modelo-datos-logico_v1.0.md` | Modelo de datos técnico (tablas, columnas, tipos, índices) | Sí |
| `adr/ADR-01-[nombre]_v1.0.md` | Architecture Decision Records — una por decisión clave | Mínimo 3 |

---

## 3. Reglas constructivas

### 3.1 Arquitectura de solución

- Respetar el tipo de arquitectura definido en PROJECT-BRIEF (monolito/microservicios/etc.).
- Respetar todas las decisiones de diseño listadas en PROJECT-BRIEF Sección 4.
- Describir el pipeline de procesamiento principal del sistema (flujo de datos end-to-end).
- Cada componente debe tener: nombre, responsabilidad, tecnología, interfaces de entrada/salida.

### 3.2 ADRs (Architecture Decision Records)

Cada ADR documenta una decisión de arquitectura. Estructura:

```markdown
# ADR-XX — [Título de la decisión]
**Estado:** Propuesto / Aceptado / Reemplazado
**Contexto:** [Por qué hay que tomar esta decisión]
**Decisión:** [Qué se decidió]
**Consecuencias positivas:** [Beneficios]
**Consecuencias negativas:** [Trade-offs]
**Alternativas consideradas:** [Qué se evaluó y por qué se descartó]
```

### 3.3 Contratos de interfaces

- Definir contratos para cada integración entre componentes.
- Incluir: método/evento, request/payload, response/resultado, errores posibles.
- Para APIs REST: método HTTP, path, request body, response codes.
- Para eventos/mensajes: nombre del evento, schema del payload, consumer esperado.

---

## 4. Criterios de calidad

- [ ] ¿La arquitectura respeta todas las decisiones tomadas en PROJECT-BRIEF?
- [ ] ¿Cada componente tiene un contrato de interfaz definido?
- [ ] ¿Hay al menos un ADR por cada decisión técnica relevante?
- [ ] ¿El modelo de datos lógico traza al modelo conceptual de SA-02?
