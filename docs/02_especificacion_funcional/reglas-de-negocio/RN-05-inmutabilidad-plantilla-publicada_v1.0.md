**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-05-inmutabilidad-plantilla-publicada_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-05 — Inmutabilidad de plantilla publicada (versionado)

## Descripción

Una `VersiónDePlantilla` con estado `publicada` es **inmutable**: no puede modificarse en ningún campo (definiciones de campos, parámetros de captura, etiquetas). Cualquier cambio sobre una plantilla publicada genera **una nueva versión** con número incrementado y estado `borrador`. La nueva versión se publica cuando esté lista; la vieja sigue válida para los Relevamientos que la usaban.

Cada Relevamiento queda atado a una `VersiónDePlantilla` específica al momento de su creación; no migra automáticamente cuando aparece una versión nueva. Esto preserva la legibilidad de relevamientos históricos.

## Origen

- [NB-03](../../01_necesidades_negocio/necesidades-de-negocio/NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md).
- RF-10, RF-11 (`PROJECT-README` Sec. 5.2).

## CUs afectados

- [CU-03](../casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md), [CU-04](../casos-de-uso/CU-04-crear-relevamiento_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Versión 1 publicada de "Inspección de pavimento" se usa en 30 relevamientos.
- Se descubre que falta un campo. Se crea v2 con el campo nuevo, se publica.
- Los 30 relevamientos antiguos siguen leyéndose bajo v1; los nuevos relevamientos eligen v1 o v2 explícitamente.

**Violaciones a detectar y rechazar**
- Cualquier rol intenta editar una `VersiónDePlantilla` con estado `publicada` → 409 con mensaje "esta versión ya está publicada; cree una nueva".
- Sistema migra automáticamente un Relevamiento de v1 a v2 sin acción explícita → no debe pasar.

---

**Fin del documento — RN-05-inmutabilidad-plantilla-publicada_v1.0.md**
