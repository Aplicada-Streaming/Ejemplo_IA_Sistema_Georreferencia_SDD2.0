**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** TC-03-sync-lww-con-precedencia-dueno_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# TC-03 — Sync LWW por campo + precedencia del dueño

**ID:** TC-03
**CU relacionado:** [CU-08](../../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md)
**RN aplicada:** [RN-07](../../02_especificacion_funcional/reglas-de-negocio/RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md)
**Tipo:** Integración + E2E con dos clientes simulados
**Prioridad:** Crítica

## Precondiciones

- Backend corriendo limpio (DB con seeds básicos).
- Cliente A (dueño del relevamiento "Test Sync") con outbox vacío.
- Cliente B (colaborador) con outbox vacío.
- Ambos ya sincronizaron y tienen el mismo Punto P1 con campo `título = "Original"`.

## Datos de entrada

Tres escenarios paralelos:

### Escenario 1 — LWW por timestamp original
- A edita `título` a "Texto A" con `timestamp_original = 10:00`.
- B edita `título` a "Texto B" con `timestamp_original = 10:05`.
- Resultado esperado tras ambos pushes: `título = "Texto B"`.

### Escenario 2 — Edición concurrente en campos distintos
- A edita `título` a "T-A" con timestamp 10:00.
- B edita `descripción` a "D-B" con timestamp 10:00.
- Resultado esperado tras ambos pushes: ambos cambios aplican; sin conflicto.

### Escenario 3 — Precedencia del dueño
- A (dueño) edita `título` a "Versión Dueño" con `timestamp_original = 10:00`.
- B (colaborador) edita `título` a "Versión Colab" con `timestamp_original = 10:30`.
- Resultado esperado tras ambos pushes: `título = "Versión Dueño"` (gana dueño aunque su timestamp sea anterior).

## Pasos

1. Para cada escenario, ejecutar los pushes en orden inverso al timestamp original (forzar que el "ganador" llegue primero o segundo, según el caso) para validar que el orden de llegada no afecta el resultado.
2. Tras los pushes, hacer pull desde un tercer cliente "Observador".
3. Comparar el estado final del Punto contra el resultado esperado.
4. Verificar que `AuditEvents` contiene los eventos en orden por `timestamp_original`.
5. Verificar que el cliente "perdedor" recibe notificación post-sync con conflicto.

## Resultado obtenido

(Se completa al ejecutar.)

## Estado

Pendiente.

## Notas

- Test base para la suite de **race scenarios** descrita en la estrategia de testing.
- Si se descubren más escenarios durante el spike BT-07, se agregan como TCs derivados.

---

**Fin del documento — TC-03-sync-lww-con-precedencia-dueno_v1.0.md**
