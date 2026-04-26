**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-08-capturas-post-cierre_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-08 — Capturas post-cierre del relevamiento

## Descripción

Cuando un Relevamiento se cierra, todos los eventos posteriores al cierre quedan rechazados al sincronizar. La regla concreta:

- Cualquier evento de creación de Punto o Foto cuyo **timestamp original sea posterior** al timestamp de cierre del Relevamiento → estado **rechazado**.
- Eventos cuyo timestamp sea anterior al cierre se aceptan normalmente, aunque lleguen físicamente al servidor después.
- Los eventos rechazados quedan visibles en el **panel de conflictos** del dueño con la opción "Reabrir el relevamiento" (que aplica los eventos pendientes) o "Mantenerlos rechazados".

Si el dueño elige reabrir, las capturas pendientes se aplican y el Relevamiento vuelve a `abierto`.

## Origen

- [NB-02](../../01_necesidades_negocio/necesidades-de-negocio/NB-02-trabajo-offline-y-colaborativo_v1.0.md), [NB-04](../../01_necesidades_negocio/necesidades-de-negocio/NB-04-gestion-ciclo-vida-relevamiento_v1.0.md).
- Conflicto C-06 (`PROJECT-BRIEF` Sec. 5.4).
- RF-04, RF-42 (`PROJECT-README` Sec. 5.1, 5.6).

## CUs afectados

- [CU-05](../casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md), [CU-08](../casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md), [CU-09](../casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Dueño cierra el Relevamiento a las 16:00. Colaborador, offline, captura un Punto a las 15:50. Sincroniza a las 17:00. El evento se acepta porque su timestamp es anterior al cierre.
- Dueño cierra a las 16:00. Colaborador captura a las 16:30. Sincroniza a las 17:00. El evento queda **rechazado** y aparece en el panel como "captura post-cierre".
- Dueño elige "Reabrir" → el evento del colaborador se aplica y el Relevamiento queda `abierto`.

**Violaciones a detectar y rechazar**
- Eventos posteriores al cierre se aplican silenciosamente sin alertar al dueño → no debe pasar.
- Cierre de Relevamiento descarta los eventos en outbox del colaborador con timestamp anterior al cierre → no debe pasar.

---

**Fin del documento — RN-08-capturas-post-cierre_v1.0.md**
