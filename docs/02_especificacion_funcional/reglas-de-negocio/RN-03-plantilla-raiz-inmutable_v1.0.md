**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-03-plantilla-raiz-inmutable_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-03 — Plantilla genérica raíz inmutable y no eliminable

## Descripción

El sistema mantiene una **plantilla genérica raíz** con valores iniciales que sirve como base para todas las demás plantillas. Esta plantilla:
1. **No es eliminable** bajo ningún caso ni por ningún rol.
2. **No tiene padre.**
3. Contiene los campos comunes a toda inspección (fecha, ubicación, condición general, observaciones, prioridad).
4. Habilita análisis transversal entre tipos de inspección porque comparten esos campos.

Las versiones de la plantilla raíz pueden ser actualizadas (creando nuevas versiones), pero la plantilla como entidad no se puede eliminar.

## Origen

- [NB-03](../../01_necesidades_negocio/necesidades-de-negocio/NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md).
- DD-05 (`PROJECT-BRIEF` Sec. 4): Plantilla genérica raíz como base obligatoria.
- RF-07 (`PROJECT-README` Sec. 5.2).

## CUs afectados

- [CU-03](../casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Cualquier nueva plantilla puede heredar directamente de la raíz o de una hija de la raíz.
- Un jefe puede publicar una nueva versión de la plantilla raíz que agregue un campo común nuevo.

**Violaciones a detectar y rechazar**
- Cualquier rol intenta eliminar la plantilla raíz → 409 / 403 con mensaje claro.
- Cualquier intento de crear una plantilla sin plantilla padre que no sea la raíz → 422.

---

**Fin del documento — RN-03-plantilla-raiz-inmutable_v1.0.md**
