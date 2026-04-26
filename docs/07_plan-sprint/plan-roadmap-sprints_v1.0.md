**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** plan-roadmap-sprints_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-07 via orquestador

---

# Plan Roadmap de Sprints — Visión global del MVP

Plan tentativo de los sprints que conforman el MVP. Solo los **Sprint 0** y **Sprint 1** tienen plan detallado al inicio del proyecto; los demás se planifican formalmente conforme avanzan los anteriores y con la velocidad real medida. Este documento da el panorama y el compromiso indicativo.

> Asume sprints de 2 semanas + ~30-40 SP de velocidad. Estos números son **calibrables** y se actualizan tras cada Sprint Review.

---

## 1. Cronograma indicativo

| Sprint | Slice / Fase | Foco | US comprometidas (estim) | SP estim | Demo |
|---|---|---|---|---|---|
| **Sprint 0** | EP-00.1 + Spike | Walking skeleton + spike sync | US-01, US-02 + 10 BTs | 64 (BT-pesado) | Slice trivial end-to-end + spike validado |
| **Sprint 1** | EP-01.1 — Slice 1 | Sync entre dos dispositivos | US-03, US-04, US-05 | 34 | Dos dispositivos sincronizan puntos vacíos |
| **Sprint 2** | EP-01.2 — Slice 2 | Captura modo detenido + plantilla raíz | US-06, US-07 | 21 | Captura real con foto + GPS + comentarios |
| **Sprint 3** | EP-01.3 — Slice 3 | Modo móvil con radio | US-08 | 8 (+ buffer) | Modo móvil funcionando con radio |
| **Sprint 4** | EP-02.1 — Slice 4 | Edición desde web + mapa colaborativo | US-09, US-10 | 21 | Revisión completa desde web |
| **Sprint 5** | EP-02.2 — Slice 5 | Plantillas hijas + renderizado dinámico | US-11, US-12 | 21 | Inspección de puente y pavimento operativas |
| **Sprint 6** | EP-02.3 — Slice 6 | Roles, áreas, permisos por punto | US-13, US-14 | 16 (+ buffer) | Roles y permisos validados |
| **Sprint 7** | EP-03.1 — Slice 7 | Carga manual web con EXIF | US-15, US-16 | 21 | Carga lote + georreferenciación manual |
| **Sprint 8** | EP-03.2 — Slice 8 | Storage configurable + wizard | US-17, US-18, BT-12, BT-14 | 27 | Wizard + S3/FTP/SFTP funcionando |
| **Sprint 9** | EP-04.1 — Slice 9 | Panel de conflictos + merge manual | US-19, US-20, BT-13 | 26 | Panel y resolución manual |
| **Sprint 10** | EP-04.2 — Slice 10 | Detección + UI fusión de puntos | US-21, US-22 | 26 | Fusión de puntos completa |
| **Sprint 11** | Estabilización + R-MVP | Bugfixes, hardening, demo final | — | (capacidad para issues) | Release MVP |

**Total acumulado:** ~285 SP útiles (sin contar el sprint de estabilización), distribuidos en ~10-11 sprints reales (~5-6 meses si los sprints son de 2 semanas).

---

## 2. Hitos de validación con cliente

Alineados con los releases del [roadmap](../00_contexto/roadmap-producto_v1.0.md):

| Tras Sprint | Release | Validación esperada |
|---|---|---|
| Sprint 1 | R-Skeleton + R-Sync Spike | Decisión de avanzar con el diseño de sync |
| Sprint 3 | R-Alpha Multi-colab | Multi-colaborador funcionando con captura real |
| Sprint 6 | R-Beta Operativo | Plantillas + roles + permisos; piloto interno |
| Sprint 8 | (intermedio) | Carga manual + storage productivo |
| Sprint 11 | R-MVP | Aceptación del MVP |

---

## 3. Reglas de re-planning

- **Final de cada sprint:** medir velocidad real (SP completados / SP comprometidos) y ajustar la velocidad estimada de los siguientes.
- **Si la velocidad real es < 70% de la estimada en 2 sprints consecutivos**: ampliar el plan o renegociar Should Have / Could Have con el sponsor.
- **Cualquier US Must Have comprometida que no termine en su sprint** queda como prioridad #1 del siguiente sprint.
- **Should Have y Could Have** son los amortiguadores naturales de recortes si la fecha de MVP aprieta. US-22 (UI revisión fusión) es Could Have y puede diferirse.

---

## 4. Recortes posibles si el plazo se ajusta

Si el sponsor confirma una fecha de MVP que no permite los 11 sprints, los recortes ordenados por menor pérdida de valor:

1. **Diferir US-22** (UI revisión fusión) a v2. La detección queda (US-21) y los candidatos se acumulan en backlog operativo.
2. **Diferir US-10** (mapa colaborativo) a v2; el mapa básico (sin filtros ni colorización por colaborador) ya viene en US-09.
3. **Diferir US-12** (renderizado dinámico) hasta que aparezca una plantilla nueva real más allá de las dos iniciales (US-11).
4. **Mantener storage solo Local** y diferir US-18 (S3/FTP/SFTP) si el cliente acepta storage local en producción inicial.

> Cada recorte requiere conformidad del PO y se documenta en el plan del sprint correspondiente.

---

## 5. Pendientes a confirmar con el sponsor

- [ ] Velocidad real del equipo (calibrada tras Sprint 0).
- [ ] Duración del sprint (2 semanas asumido).
- [ ] Fecha objetivo del MVP.
- [ ] Tamaño y composición del equipo.
- [ ] Política de demos al cliente (¿cada sprint o cada release?).

---

## 6. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [product-backlog](../06_backlog-tecnico/product-backlog_v1.0.md) | Lista de US a distribuir entre sprints |
| [backlog-tecnico](../06_backlog-tecnico/backlog-tecnico_v1.0.md) | BTs |
| [roadmap-producto](../00_contexto/roadmap-producto_v1.0.md) | Fases y releases que el plan de sprints concreta |
| `devs/intake/PROJECT-BRIEF.md` Sec. 8 | Metodología scrum con vertical slicing |

---

**Fin del documento — plan-roadmap-sprints_v1.0.md**
