**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-09-deteccion-candidatos-fusion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-09 — Detección de candidatos a fusión

## Descripción

Cuando un Punto nuevo llega al backend durante la sincronización, el sistema ejecuta:

1. Calcula la distancia geodésica entre el nuevo Punto y los demás Puntos del **mismo relevamiento**.
2. Identifica como candidatos a fusión los pares que cumplen **todas** las condiciones:
   - Pertenecen al mismo relevamiento.
   - Fueron creados por **colaboradores distintos** (no aplica a Puntos del mismo creador).
   - Distancia geodésica ≤ `merge_radius_m` (default = `radio del modo móvil` de la plantilla, típicamente 10m).
   - Diferencia de timestamps ≤ `merge_time_window` (default = 24h).
   - El par no está marcado previamente como `mantenido_separado`.
3. Marca el par como `CandidatoAFusión` en estado `pendiente`. **No fusiona automáticamente.**

La revisión y resolución es exclusivamente manual ([CU-11](../casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md)).

Esta regla incorpora la decisión deliberada de **no fusionar automáticamente** para evitar pérdida silenciosa de información cuando los puntos cercanos son defectos genuinamente distintos.

## Origen

- [NB-07](../../01_necesidades_negocio/necesidades-de-negocio/NB-07-resolucion-colaborativa-de-duplicados_v1.0.md).
- DD-21 (`PROJECT-BRIEF` Sec. 4); Conflicto C-09 (`PROJECT-BRIEF` Sec. 5.4); detalle en `PROJECT-BRIEF` Sec. 5.5.
- RF-44 (`PROJECT-README` Sec. 5.7).

## CUs afectados

- [CU-08](../casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md) (detección), [CU-11](../casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md) (resolución).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Colaborador A crea Punto a (lat, lng) a las 10:00. Colaborador B crea Punto a 7m del de A a las 11:30 mismo día. Threshold radio=10m, ventana=24h. → Candidato `pendiente`.
- Colaborador A crea dos puntos cercanos en su sesión. → **No** se crea candidato (mismo creador).
- Threshold radio=10m, distancia=12m. → No es candidato.
- Diferencia temporal 30h, ventana=24h. → No es candidato.

**Violaciones a detectar y rechazar**
- Backend fusiona automáticamente sin marcar candidato → contradice la decisión DD-21.
- Sistema vuelve a proponer un par marcado `mantenido_separado` → no debe pasar.

---

**Fin del documento — RN-09-deteccion-candidatos-fusion_v1.0.md**
