**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-11 — Aceptación jerárquica de usuarios y móvil restringido a relevadores

## Descripción

**Aceptación jerárquica:**
- El **admin raíz** acepta a los nuevos jefes de área. Mientras no esté aceptado, el jefe está en estado `pendiente_aceptacion` y no puede operar.
- Cada **jefe de área** acepta a los relevadores que se registran en su área. Mismo principio.
- El admin raíz puede además **inhabilitar** (estado reversible) o **dar de baja** (estado terminal) a un jefe de área. La inhabilitación se diseña como acción reversible para casos transitorios (vacaciones extendidas, suspensiones temporales). La baja es definitiva.

**Móvil restringido a relevadores:**
- El frontend **móvil** solo permite login a usuarios con rol `relevador` y estado `activo`. Cualquier otro rol que intente loguear en móvil recibe rechazo explícito.
- El frontend **web** permite login a todos los roles `activo` correspondientes a la organización (admin raíz, jefe de área, relevador, colaborador).

El admin raíz es el único usuario que **no se registra**: existe inicializado desde el primer arranque del sistema.

## Origen

- [NB-10](../../01_necesidades_negocio/necesidades-de-negocio/NB-10-gestion-jerarquica-de-usuarios_v1.0.md).
- RF-55, RF-56, RF-57, RF-59 (`PROJECT-README` Sec. 5.10).

## CUs afectados

- [CU-01](../casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md), [CU-05](../casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Nuevo jefe se registra → admin raíz acepta → jefe pasa a `activo` y puede loguear.
- Jefe inhabilitado pierde capacidad de operar pero sus datos quedan intactos; al rehabilitarlo, recupera operatividad.
- Jefe dado de baja → no puede recuperar el acceso; sus datos asociados quedan en histórico bajo trazabilidad ([RN-10](RN-10-eventos-append-only_v1.0.md)).
- Jefe intenta loguear en móvil → bloqueo con mensaje "el acceso móvil está restringido a relevadores".

**Violaciones a detectar y rechazar**
- Usuario `pendiente_aceptacion` puede operar → no debe pasar.
- Móvil acepta login de un jefe → no debe pasar.
- Inhabilitación se trata como baja definitiva (irreversible) → no debe pasar.

---

**Fin del documento — RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md**
