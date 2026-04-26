**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-01-permisos-por-punto_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-01 — Permisos por punto: dueño edita todo, colaborador solo lo suyo

## Descripción

Las operaciones de edición y eliminación a nivel de Punto y de Foto están gobernadas por dos atributos del actor: su rol y su relación con el relevamiento y con el punto.

| Actor | Crear punto en relevamiento | Editar punto creado por sí mismo | Editar punto creado por otro | Eliminar punto |
|---|---|---|---|---|
| Dueño del relevamiento | Sí | Sí | **Sí** (todos los del relevamiento) | Sí |
| Colaborador asignado | Sí | Sí | **No** | No |
| Jefe de área | Sí | Sí | Sí | Sí |
| Otro usuario (sin asignación) | No | N/A | No | No |

A nivel de **foto individual**, el creador de la foto puede editar su comentario; el dueño del relevamiento también; el colaborador no puede editar comentarios de fotos de otros.

## Origen

- [NB-10](../../01_necesidades_negocio/necesidades-de-negocio/NB-10-gestion-jerarquica-de-usuarios_v1.0.md).
- DD-13 (`PROJECT-BRIEF` Sec. 4): Permisos por punto.
- RF-58 (`PROJECT-README` Sec. 5.10).

## CUs afectados

- [CU-06](../casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md), [CU-07](../casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md), [CU-10](../casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md), [CU-05](../casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- El relevador A es dueño del relevamiento R1. El colaborador B fue asignado y crea el Punto P1. El dueño A edita el título de P1 — permitido.
- El colaborador B intenta editar el Punto P0 que fue creado por A — bloqueado.
- El colaborador B edita el comentario de su propia foto en P1 — permitido.

**Violaciones a detectar y rechazar**
- Un colaborador intenta editar un campo de un Punto que no creó → 403 + log.
- Un colaborador intenta eliminar un Punto → 403.
- Un usuario sin asignación intenta editar — 403.

---

**Fin del documento — RN-01-permisos-por-punto_v1.0.md**
