**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-10-eventos-append-only_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-10 — Eventos del log son append-only e inmutables

## Descripción

Los `EventoDeAuditoría` registrados sobre Relevamiento, Punto y Foto son **append-only**: una vez persistidos no pueden ser editados ni eliminados por ningún rol del sistema. Las correcciones se hacen **agregando nuevos eventos** que reflejen el cambio. Esta inmutabilidad sostiene:

1. La sincronización (los eventos son lo que se sincroniza; reescribir un evento previo rompería el orden de los demás).
2. La trazabilidad técnica para resolución de disputas.
3. Una eventual etapa de auditoría regulatoria si el cliente la incorpora en una fase posterior.

Cada evento registra: id de evento, entidad afectada, id de la entidad, tipo de evento (`created` / `field_updated` / `deleted` / `restored` / `merged`), campo (cuando aplica), valor anterior, valor nuevo, autor, origen, device_id, timestamp original, timestamp de aplicación.

## Origen

- [NB-08](../../01_necesidades_negocio/necesidades-de-negocio/NB-08-trazabilidad-tecnica-de-cambios_v1.0.md).
- DD-12 (`PROJECT-BRIEF` Sec. 4); RNF-03 (`PROJECT-README` Sec. 6).
- RF-49, RF-50 (`PROJECT-README` Sec. 5.8).

## CUs afectados

- Todos los CUs generan eventos.
- Consulta directa: [CU-12](../casos-de-uso/CU-12-consultar-trazabilidad-punto_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- El usuario edita el título de un Punto. El sistema agrega un evento `field_updated` con valores antes/después.
- Si la edición fue incorrecta, el usuario edita nuevamente al valor correcto. El sistema agrega **otro** evento `field_updated`. La historia queda completa.

**Violaciones a detectar y rechazar**
- Cualquier código del backend intenta `UPDATE` o `DELETE` sobre la tabla de eventos → no debe existir camino que lo permita; tests lo verifican.
- Migraciones de DB que reescriban eventos previos → solo si extienden la estructura sin alterar contenido (e.g. agregar columna nullable). Evitar mutaciones de contenido.

---

**Fin del documento — RN-10-eventos-append-only_v1.0.md**
