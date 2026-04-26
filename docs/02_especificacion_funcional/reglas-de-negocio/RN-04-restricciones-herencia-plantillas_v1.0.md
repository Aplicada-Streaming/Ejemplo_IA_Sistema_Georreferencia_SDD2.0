**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-04-restricciones-herencia-plantillas_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-04 — Restricciones de herencia de plantillas

## Descripción

Una plantilla hija puede:

- **Agregar** campos nuevos no presentes en la plantilla padre.
- **Sobrescribir atributos visuales y de validación** de campos heredados (etiqueta visible, hint, validación min/max, requerido sí/no, opciones de selección, atributos visuales de orden y agrupación).
- **Marcar como "no aplica"** un campo heredado para que no se renderice en relevamientos sobre la hija.

Una plantilla hija **no puede**:

- **Cambiar el tipo** de un campo heredado (de `texto` a `número`, etc.). El tipo es contractual: garantiza el análisis transversal.
- **Eliminar** un campo heredado del modelo. Solo puede marcarlo "no aplica" (ocultarlo). Eliminarlo rompería relevamientos históricos sobre versiones anteriores.

## Origen

- [NB-03](../../01_necesidades_negocio/necesidades-de-negocio/NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md).
- DD-06 (`PROJECT-BRIEF` Sec. 4).
- RF-09 (`PROJECT-README` Sec. 5.2).

## CUs afectados

- [CU-03](../casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Plantilla hija "Inspección de puente" sobrescribe `gps_accuracy_threshold_m` heredado para exigir 20m.
- Plantilla hija marca el campo heredado "altura del bordillo" como "no aplica" para inspecciones de pavimento.

**Violaciones a detectar y rechazar**
- Hija intenta cambiar tipo de un campo de `texto` a `número` → 422 con mensaje "no se puede cambiar el tipo de un campo heredado".
- Hija intenta eliminar el campo del modelo → 422 con sugerencia de marcar "no aplica".

---

**Fin del documento — RN-04-restricciones-herencia-plantillas_v1.0.md**
