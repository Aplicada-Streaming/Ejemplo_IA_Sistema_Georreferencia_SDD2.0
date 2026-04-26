**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-11-plantillas-puente-pavimento_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-11 — Plantillas hijas: inspección de puente y pavimento

**Épica:** EP-02.2 · **MoSCoW:** Must · **SP:** 13 · **Sprint sugerido:** Slice 5

> Como **jefe de área**,
> quiero **crear plantillas hijas "Inspección de puente" y "Inspección de pavimento" derivadas de la raíz, con campos propios y parámetros específicos, y publicarlas como versiones inmutables**,
> para **dar de alta los dos tipos de inspección iniciales del MVP sin tocar código**.

## CUs y RNs relacionados
- CU: [CU-03](../../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md)
- RN: [RN-04](../../02_especificacion_funcional/reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md), [RN-05](../../02_especificacion_funcional/reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md)

## Alcance
- Editor de plantilla en web (W-W10).
- Validaciones de herencia (rechazo de cambio de tipo y eliminación de heredados).
- Sobrescritura de atributos visuales y de validación.
- Marcado "no aplica" sobre campos heredados.
- Configuración de parámetros específicos (puente: GPS estricto; pavimento: laxo).
- Publicación como versión inmutable.

## Criterios de aceptación
- **CA-11.1** Crear "Inspección de puente" como hija de la raíz, con campos propios → guarda como borrador.
- **CA-11.2** Intentar cambiar tipo de campo heredado → 422.
- **CA-11.3** Marcar campo heredado como "no aplica" → no se renderiza en relevamientos sobre la hija.
- **CA-11.4** Publicar versión 1 → estado `publicada`, no editable.
- **CA-11.5** Crear hija "Inspección de pavimento" con parámetros laxos.
- **CA-11.6** Crear nueva versión 2 a partir de v1 publicada → v1 sigue válida para relevamientos pasados.

## Dependencias
- US-06, US-13 (jefe activo).

## DoR — checklist
- [x] Atada a EP-02.2.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-11**
