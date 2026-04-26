**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-05 — Asignar colaboradores y gestionar ciclo de vida del relevamiento

**Código:** CU-05
**Actor primario:** Relevador (dueño) o Jefe de área
**Frente:** Web (asignación, listado, filtros, etiquetas), Móvil (cerrar/reabrir, eliminar si dueño)

## Precondiciones

- Existe el Relevamiento.
- El usuario está autenticado y tiene permisos sobre el relevamiento (dueño, colaborador, jefe del área).

## Postcondiciones

- El relevamiento tiene la asignación de colaboradores actualizada.
- Las transiciones de estado (`abierto` ↔ `cerrado`, `eliminado_logico`) están reflejadas en la entidad y en eventos de auditoría.

## Flujo principal — Asignar colaborador

1. El dueño (o jefe de área) abre el detalle del relevamiento.
2. Selecciona "Asignar colaborador".
3. El sistema lista los relevadores `activos` del área.
4. El usuario selecciona uno o varios.
5. El sistema persiste las asignaciones y notifica a los colaboradores.

## Flujo alternativo — Cerrar relevamiento

1a. El dueño (o jefe) selecciona "Cerrar".
2a. El sistema solicita confirmación.
3a. El sistema marca el relevamiento como `cerrado`, registra fecha de cierre y emite evento.
4a. Capturas posteriores al cierre quedan rechazadas si llegan tarde por sync ([RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md), parte de [CU-08](CU-08-sincronizar-relevamiento_v1.0.md)).

## Flujo alternativo — Reabrir relevamiento

1b. El dueño (o jefe) selecciona "Reabrir" sobre un relevamiento `cerrado`.
2b. El sistema marca como `abierto` nuevamente y emite evento.

## Flujo alternativo — Eliminar relevamiento

1c. El dueño selecciona "Eliminar".
2c. El sistema solicita confirmación con advertencia.
3c. El sistema marca como `eliminado_logico` y emite evento.

## Flujo alternativo — Listar y filtrar

1d. El usuario abre la pantalla de relevamientos en web.
2d. Filtra por área (jefes ven la suya; relevadores ven solo en los que figuran como dueño o colaborador), estado, fecha, etiquetas.
3d. La lista muestra metadata: dueño, colaboradores, cantidad de puntos, conflictos pendientes.

## Flujos de error

- E1. Colaborador intenta eliminar el relevamiento → rechazo ([RN-02](../reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md)).
- E2. Colaborador intenta asignar a otros → rechazo (sólo dueño o jefe pueden asignar).
- E3. Cerrar un relevamiento ya cerrado o reabrir uno abierto → operación idempotente sin error.

## Reglas de negocio relacionadas

- [RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md) — Permisos por punto (gobierna también qué puede hacer un colaborador).
- [RN-02](../reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md) — Restricciones de eliminación.
- [RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md) — Capturas post-cierre.

## Trazabilidad

- Origen: [NB-04](../../01_necesidades_negocio/necesidades-de-negocio/NB-04-gestion-ciclo-vida-relevamiento_v1.0.md), [NB-10](../../01_necesidades_negocio/necesidades-de-negocio/NB-10-gestion-jerarquica-de-usuarios_v1.0.md).
- RFs cubiertos: RF-02, RF-03, RF-04, RF-05, RF-06, RF-57, RF-58.

## Criterios de aceptación

- **CA-05.1** — *Given* un relevamiento del que soy dueño, *when* asigno dos colaboradores activos del área, *then* ambos figuran en la lista de colaboradores y reciben notificación.
- **CA-05.2** — *Given* un relevamiento `cerrado`, *when* el dueño lo reabre desde el móvil, *then* el estado pasa a `abierto` y puede capturar nuevos puntos.
- **CA-05.3** — *Given* un colaborador asignado, *when* intenta eliminar el relevamiento, *then* el sistema rechaza con E1.
- **CA-05.4** — *Given* un jefe de área, *when* lista los relevamientos filtrando por su área y estado `abierto`, *then* ve solo los que cumplen ambas condiciones.
- **CA-05.5** — *Given* el listado web, *when* aplico filtros por etiqueta, *then* la lista filtra correctamente.

---

**Fin del documento — CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md**
