**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-03-crear-versionar-plantilla_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-03 — Crear y versionar plantilla con herencia

**Código:** CU-03
**Actor primario:** Jefe de área
**Frente:** Web

## Precondiciones

- Existe la plantilla genérica raíz creada por defecto en el sistema.
- El jefe de área está autenticado.

## Postcondiciones

- Una nueva plantilla queda creada como hija de otra existente, con campos heredados aplicados.
- Una nueva versión publicada queda inmutable y disponible para nuevos relevamientos.

## Flujo principal — Crear plantilla hija

1. El jefe accede al módulo de plantillas.
2. Selecciona "Nueva plantilla" y elige una plantilla padre (mínimo: la raíz).
3. El sistema muestra los campos heredados de la padre (tipo, etiqueta, validación).
4. El jefe puede:
   - **Agregar campos nuevos** propios de la hija.
   - **Sobrescribir atributos visuales o de validación** de campos heredados (etiqueta, hint, min/max, requerido).
   - **Marcar como "no aplica"** un campo heredado para que no se renderice en relevamientos sobre esta plantilla.
5. El jefe configura los **parámetros de captura** específicos: timeout GPS, accuracy threshold, radio del modo móvil, parámetros de compresión, threshold de fusión.
6. El jefe guarda como `borrador` (editable) o **publica** (inmutable).
7. Al publicar: el sistema incrementa el número de versión y deja la plantilla disponible para crear relevamientos.

## Flujo alternativo — Editar plantilla en borrador

1a. El jefe abre una plantilla en estado `borrador`.
2a. Repite los pasos 4 y 5.
3a. Vuelve a guardar como borrador o publica.

## Flujo alternativo — Versionar una plantilla publicada

1b. El jefe abre una plantilla con versión `publicada`.
1b. El sistema le ofrece "Crear nueva versión".
3b. El sistema clona los campos definidos como nuevo `borrador` con número de versión incrementado.
4b. El jefe edita y publica.
5b. La versión anterior **sigue siendo válida** para relevamientos que ya la usaban.

## Flujos de error

- E1. Intentar cambiar el tipo de un campo heredado → el sistema rechaza con mensaje claro (regla [RN-04](../reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md)).
- E2. Intentar eliminar un campo heredado (no marcar "no aplica", sino borrarlo) → el sistema rechaza ([RN-04](../reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md)).
- E3. Intentar editar una plantilla `publicada` → el sistema rechaza con sugerencia de crear nueva versión ([RN-05](../reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md)).
- E4. Intentar eliminar la plantilla raíz → el sistema rechaza ([RN-03](../reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md)).
- E5. Validaciones inconsistentes (e.g. min > max) → rechazo en validación de formulario.

## Reglas de negocio relacionadas

- [RN-03](../reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md) — Plantilla raíz inmutable y no eliminable.
- [RN-04](../reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md) — Restricciones de herencia.
- [RN-05](../reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md) — Inmutabilidad de plantilla publicada.

## Trazabilidad

- Origen: [NB-03](../../01_necesidades_negocio/necesidades-de-negocio/NB-03-tipos-diversos-de-inspeccion-sin-codigo_v1.0.md).
- RFs cubiertos: RF-07, RF-08, RF-09, RF-10, RF-11, RF-12, RF-13.

## Criterios de aceptación

- **CA-03.1** — *Given* la plantilla raíz publicada, *when* un jefe crea una hija con dos campos nuevos, *then* la hija expone los campos heredados + los dos nuevos.
- **CA-03.2** — *Given* una plantilla hija, *when* el jefe intenta cambiar el tipo de un campo heredado de `texto` a `número`, *then* el sistema rechaza con mensaje claro.
- **CA-03.3** — *Given* una plantilla hija con campo heredado marcado como "no aplica", *when* se renderiza un relevamiento sobre la hija, *then* el campo no se muestra.
- **CA-03.4** — *Given* una plantilla `publicada`, *when* el jefe intenta editar campos, *then* el sistema rechaza y le ofrece crear nueva versión.
- **CA-03.5** — *Given* dos versiones publicadas (v1 y v2) de la misma plantilla, *when* un relevamiento del pasado fue creado con v1, *then* sigue siendo legible bajo v1 aunque exista v2.
- **CA-03.6** — *Given* un usuario con rol distinto de `jefe_area`, *when* intenta crear plantilla, *then* el sistema rechaza con 403.

---

**Fin del documento — CU-03-crear-versionar-plantilla_v1.0.md**
