**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** TC-04-fusion-puntos-mismo-colaborador-no-aplica_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# TC-04 — Fusión de puntos: regla de "distintos colaboradores"

**ID:** TC-04
**CU relacionado:** [CU-11](../../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md), [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md)
**RN aplicada:** [RN-09](../../02_especificacion_funcional/reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md)
**Tipo:** Integración (worker de sync)
**Prioridad:** Crítica

## Precondiciones

- Backend corriendo limpio.
- Plantilla raíz publicada con `merge_radius_m = 10` y `merge_time_window = 24h`.
- Relevamiento "Test Fusión" con dos colaboradores asignados (A y B).
- Posición P0 = (lat0, lng0).

## Datos de entrada y casos

| Caso | Punto X (autor, posición, timestamp) | Punto Y (autor, posición, timestamp) | Distancia | Δt | ¿Candidato? |
|---|---|---|---|---|---|
| 1 | A, P0, 10:00 | B, P0+~7m, 11:30 | 7m | 1h30 | **Sí** |
| 2 | A, P0, 10:00 | A, P0+~7m, 11:30 | 7m | 1h30 | **No** (mismo autor) |
| 3 | A, P0, 10:00 | B, P0+~12m, 11:30 | 12m | 1h30 | **No** (excede radio) |
| 4 | A, P0, 10:00 | B, P0+~7m, 11:00 día siguiente | 7m | 25h | **No** (excede ventana) |
| 5 | A, P0, 10:00 | B, P0+~7m, 11:30 (par previamente "mantenido_separado") | 7m | 1h30 | **No** (par excluido) |

## Pasos

1. Para cada caso, generar los dos eventos `point.created` con los autores, posiciones y timestamps indicados.
2. Pushear al backend.
3. Tras procesamiento del worker de sync, consultar `MergeCandidates` filtrando por el relevamiento.
4. Verificar la presencia o ausencia del par según la columna "¿Candidato?".
5. Para el caso 1: validar que el panel de conflictos del jefe muestra el candidato en estado `pendiente`.

## Resultado obtenido

(Se completa al ejecutar.)

## Estado

Pendiente.

## Notas

- Test base para la suite de detección de fusiones.
- TC-aux para la **resolución manual** (Fusionar / Mantener separados) se documenta a continuación de US-22.

---

**Fin del documento — TC-04-fusion-puntos-mismo-colaborador-no-aplica_v1.0.md**
