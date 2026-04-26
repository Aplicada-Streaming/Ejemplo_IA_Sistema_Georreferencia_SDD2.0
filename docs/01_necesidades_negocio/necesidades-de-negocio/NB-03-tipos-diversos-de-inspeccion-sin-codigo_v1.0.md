**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-03 — Soporte de tipos diversos de inspección sin tocar código

### Problema específico

Vialidad inspecciona distintos tipos de activos: pavimento, puentes, alcantarillas, señalización, drenajes. Cada tipo tiene campos propios (estado de la carpeta, longitud del puente, severidad de fisuras, etc.) y muchos campos comunes (fecha, ubicación, condición general, observaciones, prioridad). Si el sistema requiere cambios de código y release nuevo cada vez que aparece un tipo de inspección o se ajusta un campo, el costo y latencia de adopción de nuevos casos lo hacen impracticable. Además, la organización planteó explícitamente vocación de extender el sistema a otras inspecciones de obra pública.

### Impacto si no se resuelve

- Cada tipo de inspección nuevo se vuelve un proyecto de software.
- La organización no puede experimentar con nuevos tipos de inspección rápido.
- Datos históricos quedan inconsistentes ante cambios de campos.
- Pérdida de la capacidad de análisis transversal entre tipos (no comparten campos).
- El sistema se vuelve obsoleto en cuanto el cliente cambia su forma de inspeccionar.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Tiempo desde la solicitud de un nuevo tipo de inspección hasta que está operativa para los relevadores | N/A | ≤ 5 días hábiles para una plantilla derivada sin campos custom de UI (alineado con [MET-04](../../00_contexto/vision-producto_v1.0.md#4-métricas-de-éxito-smart)) | A partir de Slice 5 | Registro de tickets de plantillas |
| Cambios en frontend requeridos para incorporar una plantilla nueva | N/A | 0 cambios para plantillas con campos estándar | Permanente desde Slice 5 | Auditoría de PRs por plantilla nueva |
| Relevamientos históricos siguen siendo legibles tras evolución de la plantilla | N/A | 100% legibles | Permanente desde Slice 5 | Test de regresión con plantilla v1 y v2 |
| Análisis transversal entre tipos sobre campos comunes | N/A | Funcional para los campos de la plantilla raíz | A partir de Slice 5 | Query de ejemplo sobre campos comunes |

### Stakeholders

- **Cliente / Sponsor** — la vocación de extensión es decisión estratégica del cliente.
- **Jefe de área** — solicita y valida nuevas plantillas.
- **Equipo de desarrollo** — beneficiario operativo: menos proyectos por tipo nuevo.

### RFs y RNFs cubiertos

RF-07, RF-08, RF-09, RF-10, RF-11, RF-12, RF-13, RNF-07.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Habilita:** [NB-01](NB-01-captura-georreferenciada-en-campo_v1.0.md) (los campos a capturar dependen de la plantilla), [NB-04](NB-04-gestion-ciclo-vida-relevamiento_v1.0.md) (el relevamiento se asocia a una plantilla), [NB-06](NB-06-revision-y-consolidacion-en-gabinete_v1.0.md) (la revisión renderiza dinámicamente).
- **Independiente** del resto.

> Decisión de diseño relevante: DD-04, DD-05, DD-06, DD-07 en `PROJECT-BRIEF` Sec. 4. Define herencia, plantilla raíz, restricciones de herencia y persistencia EAV.

---

**Fin del documento — NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md**
