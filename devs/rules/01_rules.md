# Reglas Constructivas — SA-01: Necesidades de Negocio

**Sección objetivo:** `docs/01_necesidades_negocio/`  
**Rol del subagente:** Analista de Negocio Senior (CBAP)  
**Depende de:** SA-00 completo + PROJECT-README.md (Secciones 3, 4, 5)  
**Es upstream de:** SA-02, SA-06

---

## 1. Documentos a leer ANTES de escribir

1. `docs/00_contexto/vision-producto_v1.0.md`
2. `docs/00_contexto/alcance-proyecto_v1.0.md`
3. `devs/intake/PROJECT-README.md` — secciones: problema, requerimientos funcionales y no funcionales
4. Si existe: `docs/01_necesidades_negocio/` — leer para no duplicar

---

## 2. Artefactos a producir

| Archivo | Descripción | Obligatorio |
|---|---|---|
| `necesidades-negocio_v1.0.md` | Resumen ejecutivo de todas las NB, mapa de stakeholders | Sí |
| `necesidades-de-negocio/NB-01-[nombre]_v1.0.md` | Una necesidad de negocio por archivo | Sí (mínimo 3) |
| `necesidades-de-negocio/NB-02-[nombre]_v1.0.md` | ... | Sí |
| `necesidades-de-negocio/NB-XX-[nombre]_v1.0.md` | Tantas como el análisis del intake identifique | Según proyecto |

---

## 3. Reglas constructivas

### 3.1 Identificación de necesidades de negocio

Cada NB debe surgir de los requerimientos funcionales del PROJECT-README. Proceso:

1. Leer cada RF del README.
2. Preguntarse: ¿qué problema de negocio está detrás de este requerimiento?
3. Agrupar RFs que responden al mismo problema de negocio → una NB.
4. Una NB nunca es técnica. Es siempre un problema de negocio o una oportunidad.

### 3.2 Estructura de cada NB (`NB-XX-nombre_v1.0.md`)

Cada archivo de necesidad de negocio debe tener:

```markdown
## NB-XX — [Nombre descriptivo]
**Problema específico:** [Descripción del dolor actual]
**Impacto si no se resuelve:** [Consecuencias medibles]
**Criterios de éxito:** [Tabla con métrica, baseline y target]
**Stakeholders:** [Quién tiene este problema]
**Trazabilidad a Casos de Uso:** [Tabla NB → CU — se completa en SA-02]
**Dependencias con otras NB:** [Si aplica]
```

### 3.3 Trazabilidad

- Cada NB debe trazar a al menos un Caso de Uso (columna se completa cuando SA-02 finalice).
- Cada RF del PROJECT-README debe tener al menos una NB que lo contenga.
- Si un RF no traza a ninguna NB, es señal de que falta una NB o el RF es técnico (no de negocio).

### 3.4 Nomenclatura

- Nombre del archivo: `NB-XX-nombre-en-kebab-case_v1.0.md`
- El nombre después del código debe ser descriptivo: `NB-03-cambios-sin-recompilar_v1.0.md`, no `NB-03_v1.0.md`.
- Numeración secuencial empezando en NB-01.

---

## 4. Anti-patrones a evitar

| Anti-patrón | Por qué es un problema |
|---|---|
| NB técnicas ("necesitamos una API REST") | Las NB son de negocio, no de arquitectura |
| NB sin criterios de éxito medibles | No se puede validar si se resolvió |
| NB duplicadas que describen el mismo problema | Confunde la trazabilidad |
| Stakeholders genéricos ("el cliente") | Sin nombre/rol no hay responsable |

---

## 5. Criterios de calidad antes de pasar a SA-02

- [ ] ¿Cada NB tiene problema, impacto y criterio de éxito?
- [ ] ¿Cada RF del intake traza a al menos una NB?
- [ ] ¿Ninguna NB es técnica?
- [ ] ¿El resumen ejecutivo lista todas las NB con su código y descripción?
