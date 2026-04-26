**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** TC-05-plantilla-restricciones-herencia_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# TC-05 — Restricciones de herencia y versionado de plantillas

**ID:** TC-05
**CU relacionado:** [CU-03](../../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md)
**RN aplicada:** [RN-03](../../02_especificacion_funcional/reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md), [RN-04](../../02_especificacion_funcional/reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md), [RN-05](../../02_especificacion_funcional/reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md)
**Tipo:** Integración (backend) + UI manual (web)
**Prioridad:** Alta

## Precondiciones

- Backend corriendo limpio con seeds: plantilla raíz publicada (v1) con campos `fecha (fecha)`, `condición_general (texto)`, `prioridad (selección)`.
- Usuario `jefe.test@vialidad` activo.

## Datos de entrada y casos

| Caso | Acción | Resultado esperado |
|---|---|---|
| 1 | Crear hija "Inspección de puente" con campo nuevo `longitud_m (número)` | 201 Created; plantilla en estado `borrador` |
| 2 | En la hija, intentar cambiar tipo de `condición_general` de `texto` a `número` | 422 con mensaje "no se puede cambiar el tipo de un campo heredado" |
| 3 | En la hija, marcar `prioridad` como `no aplica` | 200; al renderizar relevamiento sobre la hija, `prioridad` no aparece |
| 4 | En la hija, intentar eliminar el campo `fecha` | 422 con mensaje "use 'no aplica' para ocultar campos heredados" |
| 5 | Publicar la hija (versión 1) | 200; estado pasa a `publicada` |
| 6 | Intentar editar la hija publicada (e.g. cambiar etiqueta) | 409 con sugerencia "cree una nueva versión" |
| 7 | Crear nueva versión 2 a partir de v1, agregar campo `año_construccion (número)`, publicar | 200; v1 sigue válida; v2 disponible |
| 8 | Eliminar plantilla raíz | 409 / 403 ([RN-03](../../02_especificacion_funcional/reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md)) |

## Pasos

1. Loguear como jefe.
2. Para cada caso, ejecutar la acción correspondiente vía API o UI web W-W10.
3. Verificar status code y mensaje del error.
4. Para casos 5 y 7: verificar que un relevamiento sobre la hija renderiza correctamente los campos heredados + propios.

## Resultado obtenido

(Se completa al ejecutar.)

## Estado

Pendiente.

## Notas

- TC base para la **suite de plantillas** del DoD.
- Cubre los anti-patrones declarados en [RN-04](../../02_especificacion_funcional/reglas-de-negocio/RN-04-restricciones-herencia-plantillas_v1.0.md).

---

**Fin del documento — TC-05-plantilla-restricciones-herencia_v1.0.md**
