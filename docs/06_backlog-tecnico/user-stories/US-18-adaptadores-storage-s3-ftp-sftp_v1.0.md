**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-18-adaptadores-storage-s3-ftp-sftp_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-18 — Adaptadores de storage S3, FTP y SFTP funcionales

**Épica:** EP-03.2 · **MoSCoW:** Should · **SP:** 13 · **Sprint sugerido:** Slice 8

> Como **admin raíz que opera el sistema en distintos ambientes del cliente**,
> quiero **disponer de adaptadores funcionales para S3, FTP y SFTP además del local, todos detrás del puerto hexagonal**,
> para **elegir el destino de almacenamiento según las condiciones del cliente**.

## CUs y RNs relacionados
- CU: [CU-02](../../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md)
- RN: [RN-12](../../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)

## Alcance
- Implementación de `S3PhotoStorageAdapter` (AWS SDK).
- Implementación de `FtpPhotoStorageAdapter`.
- Implementación de `SftpPhotoStorageAdapter` (SSH.NET).
- Tests de integración por adaptador con servicios reales o testcontainers.
- Documentación de credenciales requeridas por adaptador.

## Criterios de aceptación
- **CA-18.1** Cada adaptador implementa `IPhotoStorageAdapter` y pasa `TestConnectionAsync`.
- **CA-18.2** Subir + leer + borrar foto en cada adaptador funciona.
- **CA-18.3** Lectura de fotos creadas con un adaptador, tras cambiar al otro, sigue funcionando.
- **CA-18.4** Tests de integración pasan en CI con testcontainers / minio para S3, vsftpd para FTP/SFTP.

## Dependencias
- ADR-02 implementado.

## DoR — checklist
- [x] Atada a EP-03.2.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-18**
