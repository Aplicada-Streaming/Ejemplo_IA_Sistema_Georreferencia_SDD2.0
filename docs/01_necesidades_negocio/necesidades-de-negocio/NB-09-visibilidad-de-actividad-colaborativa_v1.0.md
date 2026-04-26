**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** NB-09-visibilidad-de-actividad-colaborativa_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-01 via orquestador

---

## NB-09 — Visibilidad de la actividad de cada colaborador en la campaña

### Problema específico

Cuando varios relevadores trabajan sobre el mismo relevamiento, cada uno necesita saber qué ya cubrió el otro y qué quedó por hacer, sin tener que hacer una consolidación manual. Si el mapa muestra todos los puntos iguales, no se distingue qué hizo cada quién, ni qué se editó recientemente, ni qué partes del puente o tramo siguen sin cubrir. La consecuencia práctica es duplicación de trabajo y pérdida de tiempo en coordinación informal por teléfono o radio.

### Impacto si no se resuelve

- Dos colaboradores cubren el mismo lugar dos veces sin saberlo.
- Otras zonas quedan sin cubrir sin que el equipo lo detecte hasta el regreso.
- Coordinación se realiza fuera del sistema (radio, mensajes), con costo y errores.
- Pérdida del beneficio de la colaboración multi-dispositivo.

### Criterios de éxito

| Métrica | Baseline | Target | Plazo | Cómo se mide |
|---|---|---|---|---|
| Diferenciación visual de puntos por colaborador | N/A | Cada colaborador con color/ícono distinto | Slice 4 | Validación funcional |
| Filtro "ver solo mis puntos / ver todos" disponible en mapa | N/A | Operativo | Slice 4 | Validación funcional |
| Indicador de actividad reciente sobre un punto | N/A | Visible durante 24h tras el último cambio | Slice 4 | Validación funcional |
| Aceptación por relevadores en piloto | N/A | ≥ 80% reporta menor coordinación informal | Final del MVP | Encuesta |

### Stakeholders

- **Relevador (dueño)** — necesita saber qué hizo el colaborador y dónde.
- **Colaborador asignado** — necesita saber dónde ya intervino el dueño u otro colaborador.
- **Jefe de área** — usa la visualización para gestionar la cobertura.

### RFs y RNFs cubiertos

RF-52, RF-53, RF-54.

### Trazabilidad a Casos de Uso

| CU | Nombre | Estado |
|---|---|---|
| `[A completar por SA-02]` | — | Pendiente |

### Dependencias con otras NB

- **Depende de:** [NB-02](NB-02-trabajo-offline-y-colaborativo_v1.0.md) (la visualización requiere haber sincronizado los puntos del otro), [NB-08](NB-08-trazabilidad-tecnica-de-cambios_v1.0.md) (el indicador de actividad usa los timestamps del log).
- **Habilitada por:** [NB-04](NB-04-gestion-ciclo-vida-relevamiento_v1.0.md) (los puntos pertenecen al relevamiento).

---

**Fin del documento — NB-09-visibilidad-de-actividad-colaborativa_v1.0.md**
