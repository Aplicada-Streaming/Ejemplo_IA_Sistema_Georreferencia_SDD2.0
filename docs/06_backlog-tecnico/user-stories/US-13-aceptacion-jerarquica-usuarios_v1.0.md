**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-13-aceptacion-jerarquica-usuarios_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-13 — Aceptación jerárquica admin → jefe → relevador

**Épica:** EP-02.3 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Slice 6

> Como **admin raíz o jefe de área**,
> quiero **aceptar (o rechazar) las solicitudes de registro de mi nivel jerárquico inferior**,
> para **gestionar quién puede operar en el sistema y respetar la estructura de la organización**.

## CUs y RNs relacionados
- CU: [CU-01](../../02_especificacion_funcional/casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md)
- RN: [RN-11](../../02_especificacion_funcional/reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md)

## Alcance
- Pantallas de "Solicitudes pendientes" para admin y jefe.
- Endpoints `POST /users/{id}/accept`, `POST /users/{id}/disable`, `POST /users/{id}/enable`, `POST /users/{id}/delete`.
- Aplicación de transiciones de estado.
- Notificaciones por email u otra vía (`[REQUIERE_INFO]` canal de notificación; default email del usuario).

## Criterios de aceptación
- **CA-13.1** Jefe se registra → admin ve solicitud → admin acepta → jefe pasa a `activo`.
- **CA-13.2** Relevador se registra → jefe del área ve solicitud → jefe acepta → relevador pasa a `activo`.
- **CA-13.3** Admin rechaza jefe → estado `dado_de_baja`, no puede loguear.
- **CA-13.4** Admin inhabilita jefe → estado `inhabilitado`, sin posibilidad de operar; admin puede rehabilitar.
- **CA-13.5** Jefe intenta aceptar relevador de otra área → 403.

## Dependencias
- US-01.

## DoR — checklist
- [x] Atada a EP-02.3.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-13**
