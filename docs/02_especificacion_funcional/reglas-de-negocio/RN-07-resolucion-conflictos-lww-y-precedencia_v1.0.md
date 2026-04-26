**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-07 — Resolución LWW por campo + precedencia del dueño

## Descripción

Cuando dos eventos compiten por el mismo campo de la misma entidad (Punto, Foto o Relevamiento), la resolución es:

1. **Last-write-wins por campo individual** basada en el `timestamp original` del evento (no el de llegada al servidor).
2. **Precedencia del dueño**: si uno de los eventos en conflicto es del dueño del relevamiento y el otro de un colaborador, **gana el del dueño incondicionalmente**, aunque su timestamp sea anterior. Esta excepción se aplica solo en el conflicto entre dueño y colaborador.

La aplicación es **por campo**, no por entidad: dos cambios sobre campos distintos del mismo Punto no se consideran conflicto.

Cuando se resuelve un conflicto automáticamente, los usuarios afectados reciben **notificación post-sync** con la opción de revertir desde el panel de conflictos.

## Origen

- [NB-02](../../01_necesidades_negocio/necesidades-de-negocio/NB-02-trabajo-offline-y-colaborativo_v1.0.md).
- DD-11 (`PROJECT-BRIEF` Sec. 4); Conflicto C-01, C-05 (`PROJECT-BRIEF` Sec. 5.4).
- RF-39, RF-41, RF-42, RF-43 (`PROJECT-README` Sec. 5.6).

## CUs afectados

- [CU-08](../casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Colaborador edita "título" del Punto a las 10:00 a "Bache izquierdo". Dueño lo edita a las 10:05 a "Bache lado izquierdo". Tras sync, el título queda "Bache lado izquierdo" (ganó por timestamp + por ser dueño).
- Colaborador edita "título" a las 10:10 a "Hueco". Dueño edita "descripción" a las 10:12. Tras sync, ambos cambios se aplican (no hay conflicto de campo).
- Colaborador edita "título" a las 10:30 (timestamp posterior). Dueño edita "título" a las 10:00 (timestamp anterior). Tras sync, **gana el del dueño** por la regla de precedencia, aunque su timestamp sea anterior. El colaborador recibe notificación.

**Violaciones a detectar y rechazar**
- LWW basado en timestamp de llegada al servidor en lugar del original → no debe pasar.
- Conflicto resuelto sobre la entidad completa pisando campos no en disputa → no debe pasar.

---

**Fin del documento — RN-07-resolucion-conflictos-lww-y-precedencia_v1.0.md**
