**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** tracking-velocity_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-07 via orquestador

---

# Tracking de Velocidad — Template

Plantilla para registrar la velocidad real del equipo sprint por sprint y proyectar fechas. Se actualiza en cada **Sprint Retrospective** con los datos del sprint cerrado.

---

## 1. Tabla de seguimiento

| Sprint | Fechas | SP comprometidos | SP completados | Velocidad real | % cumplimiento | Notas / aprendizajes |
|---|---|---|---|---|---|---|
| Sprint 0 | [TBD]–[TBD] | 64 | — | — | — | — |
| Spike 1w | [TBD]–[TBD] | 13 timeboxed | — | — | — | Salida: protocolo de sync validado |
| Sprint 1 | [TBD]–[TBD] | 34 | — | — | — | — |
| Sprint 2 | [TBD]–[TBD] | 21 | — | — | — | — |
| Sprint 3 | [TBD]–[TBD] | 8 + buffer | — | — | — | — |
| Sprint 4 | [TBD]–[TBD] | 21 | — | — | — | — |
| Sprint 5 | [TBD]–[TBD] | 21 | — | — | — | — |
| Sprint 6 | [TBD]–[TBD] | 16 + buffer | — | — | — | — |
| Sprint 7 | [TBD]–[TBD] | 21 | — | — | — | — |
| Sprint 8 | [TBD]–[TBD] | 27 | — | — | — | — |
| Sprint 9 | [TBD]–[TBD] | 26 | — | — | — | — |
| Sprint 10 | [TBD]–[TBD] | 26 | — | — | — | — |
| Sprint 11 estab. | [TBD]–[TBD] | (capacidad) | — | — | — | — |

**Velocidad media móvil (3 últimos sprints):** [TBD] SP/sprint.
**Velocidad estable (post calibración Sprint 0):** [TBD] SP/sprint.

---

## 2. Métricas adicionales por sprint

Registrar tras cada Retrospective:

| Métrica | Definición | Sprint 0 | Sprint 1 | ... |
|---|---|---|---|---|
| % de US completadas vs comprometidas | (US done / US comprometidas) | — | — | |
| Bugs reportados | Bugs creados durante el sprint sobre features que se decían done | — | — | |
| Bugs resueltos | Bugs cerrados durante el sprint | — | — | |
| Tiempo medio de PR | Horas desde apertura del PR hasta merge | — | — | |
| Cantidad de US bloqueadas | US que no avanzaron por blocker externo | — | — | |
| US ingresadas mid-sprint | Ítems no planificados que entraron | — | — | |

---

## 3. Reglas de proyección

Tras cada Sprint:

1. Calcular **velocidad real** (`SP completados / SP comprometidos`).
2. Si la velocidad real está **dentro de ±15%** de la estimada → mantener la estimación.
3. Si está **> 15% por debajo** → ajustar la velocidad estimada de los siguientes a la media móvil de los últimos 2 sprints.
4. Si está **> 15% por encima** → considerar ampliar el alcance del próximo sprint o adelantar US Should Have.
5. Recalcular fecha de MVP con la velocidad media móvil reciente.

---

## 4. Burn-down acumulado

Tras cada sprint, actualizar el siguiente cuadro (visual mental — herramienta de gestión real lleva el chart):

| Sprint cerrado | SP totales del MVP | SP completados acumulados | SP restantes | Sprints proyectados a fin |
|---|---|---|---|---|
| Sprint 0 | ~285 | — | — | — |
| Sprint 1 | ~285 | — | — | — |
| ... | | | | |

---

## 5. Reglas de comunicación

- **Velocidad y proyección de fecha de MVP** se comparten con el sponsor en cada **Sprint Review**.
- Si la proyección actual sale del marco temporal acordado, **abrir conversación de alcance** con el sponsor en lugar de estirar plazos en silencio.
- El equipo no maquilla la velocidad real para "verse mejor": el dato sirve para planificar, no para presionar.

---

## 6. Trazabilidad

| Documento | Aporte |
|---|---|
| [plan-roadmap-sprints](plan-roadmap-sprints_v1.0.md) | Estimaciones que esta tabla compara con la realidad |
| [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) Sec. 3 | Cadencia de Retrospective donde esta tabla se actualiza |

---

**Fin del documento — tracking-velocity_v1.0.md**
