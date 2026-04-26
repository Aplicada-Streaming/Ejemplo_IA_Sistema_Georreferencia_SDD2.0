**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-02-restricciones-eliminacion-relevamiento_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-02 — Restricciones de eliminación del relevamiento

## Descripción

Solo el **dueño** del Relevamiento o un **jefe de área del área del relevamiento** pueden eliminarlo. Los **colaboradores asignados no pueden eliminar bajo ningún caso**, aunque sean los autores de muchos puntos del relevamiento. La eliminación es lógica (`eliminado_logico`); el evento queda registrado y el relevamiento puede consultarse en histórico para fines de trazabilidad.

## Origen

- [NB-04](../../01_necesidades_negocio/necesidades-de-negocio/NB-04-gestion-ciclo-vida-relevamiento_v1.0.md).
- RF-01 (`PROJECT-README` Sec. 5.1).
- Acuerdo confirmado en `PROJECT-README` Sec. 9.4: "Los relevamientos no pueden ser eliminados por colaboradores asignados".

## CUs afectados

- [CU-05](../casos-de-uso/CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Dueño selecciona "Eliminar" — el sistema confirma y aplica eliminación lógica.

**Violaciones a detectar y rechazar**
- Colaborador asignado intenta eliminar el relevamiento → 403, sin importar la cantidad de puntos que él haya creado.
- Usuario externo intenta eliminar → 403.

---

**Fin del documento — RN-02-restricciones-eliminacion-relevamiento_v1.0.md**
