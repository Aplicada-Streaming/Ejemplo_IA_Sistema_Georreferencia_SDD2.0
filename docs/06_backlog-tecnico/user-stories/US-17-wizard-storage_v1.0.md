**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-17-wizard-storage_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-17 — Wizard de primer arranque para configurar storage

**Épica:** EP-03.2 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Slice 8

> Como **admin raíz**,
> quiero **configurar el adaptador de storage al primer arranque del sistema con un wizard guiado, validando la conexión antes de persistir**,
> para **arrancar el sistema sin tocar configuración del despliegue**.

## CUs y RNs relacionados
- CU: [CU-02](../../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md)
- RN: [RN-12](../../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)

## Alcance
- Detección "primer arranque" si `SystemConfig.storage.active_adapter` está vacío.
- Pantalla W-W02 wizard.
- Endpoints `POST /system-config/storage/test` y `POST /system-config/storage`.
- Cifrado de credenciales en `SystemConfig`.
- Bloqueo de operaciones que requieran storage hasta que esté configurado.

## Criterios de aceptación
- **CA-17.1** Primer login del admin sin config → redirige al wizard.
- **CA-17.2** Selección de Local con path inválido → 422 con mensaje específico.
- **CA-17.3** Selección de S3 con credenciales válidas → test escribe/lee/borra archivo dummy y persiste.
- **CA-17.4** Tras guardar, redirige al dashboard.
- **CA-17.5** Reconfiguración: admin cambia de Local a S3 → datos previos siguen accesibles ([RN-12](../../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)).

## Dependencias
- US-13, US-18 (al menos un adaptador funcional para test).

## DoR — checklist
- [x] Atada a EP-03.2.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-17**
