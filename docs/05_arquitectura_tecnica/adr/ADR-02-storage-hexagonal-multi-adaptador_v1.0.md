**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** ADR-02-storage-hexagonal-multi-adaptador_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-05 via orquestador

---

# ADR-02 — Storage de fotos con arquitectura hexagonal multi-adaptador

**Estado:** Aceptado.

## Contexto

El cliente exige que el destino de almacenamiento de fotos sea **configurable** entre `local` (filesystem), `S3`, `FTP` y `SFTP`, con la posibilidad de cambiar el adaptador activo desde un wizard del admin raíz sin redeploy. Las fotos representan el grueso del volumen del sistema. Una decisión equivocada acopla el dominio a un proveedor concreto y entorpece migraciones futuras.

Las decisiones tomadas en el intake afines son DD-14 (storage hexagonal), DD-15 (wizard de primer arranque), RF-60 a RF-62 y RNF-04. Quedó **explícitamente fuera del MVP** la migración masiva entre adaptadores (EX-02 del alcance).

## Decisión

Implementar un **puerto** `IPhotoStorageAdapter` y **cuatro adaptadores** que lo implementan:

- `LocalFileSystemPhotoStorageAdapter`
- `S3PhotoStorageAdapter`
- `FtpPhotoStorageAdapter`
- `SftpPhotoStorageAdapter`

El módulo `Photos` consume **solo** la interfaz, nunca un adaptador concreto. La selección del adaptador activo se resuelve en runtime a partir de `SystemConfig`, vía `IPhotoStorageAdapterFactory`.

Cada `Photo` persiste su `{adapter_ref, adapter_name}`, lo cual le permite leerse desde el adaptador con que fue creada **aunque el sistema haya cambiado de configuración** ([RN-12](../../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)). Para servir una foto, la lógica resuelve el adaptador correcto por `adapter_name` (no necesariamente el activo).

El wizard de primer arranque y la reconfiguración exponen un endpoint `POST /api/v1/system-config/storage/test` que ejerce la operación completa (escribir / leer / borrar archivo dummy) antes de persistir, para detectar credenciales o permisos inválidos.

## Consecuencias positivas

- **Cambio de adaptador sin tocar código de dominio.** Cumple la abstracción que el cliente requiere.
- **Lecturas de datos previos siguen funcionando** tras un cambio de adaptador, porque cada Foto referencia su origen.
- **Adaptadores testables independientemente.** Tests del dominio usan un fake adapter; tests de integración prueban cada adaptador real.
- **Mocking trivial** en tests del módulo `Photos`.
- **Extensible:** un futuro adaptador (e.g. Azure Blob) se incorpora implementando la misma interfaz.

## Consecuencias negativas

- **Lecturas con adaptadores discontinuados:** si un adaptador es eliminado del binario (e.g. se descontinua FTP), las fotos creadas con él dejan de leerse. Se mitiga manteniendo los adaptadores legacy como solo-lectura.
- **Doble tracking de credenciales:** activas (en `SystemConfig`) y posibles credenciales heredadas si los adaptadores antiguos requieren acceso. La política inicial es que `SystemConfig` mantiene credenciales solo del activo, y cada adaptador legacy tiene su pool separado de credenciales si aplica (la mayoría —local FS y S3 con bucket fijo— no lo requieren).
- **Test de conexión por adaptador no captura todos los escenarios** (latencia, ancho de banda, comportamiento bajo carga). Se mitiga con monitoreo en runtime.

## Alternativas consideradas

1. **Acoplar a un proveedor concreto** (e.g. solo S3). Descartada por requisito explícito del cliente.
2. **Migración automática al cambiar adaptador.** Descartada (EX-02 del alcance) por complejidad y costo. Se podrá implementar en una fase posterior si el cliente lo prioriza.
3. **Service Bus o Object Storage Gateway externo** que abstraiga los adaptadores. Descartada por el costo operativo y porque añade una dependencia de infraestructura no justificada.

## Trazabilidad

- DD-14, DD-15 (`PROJECT-BRIEF` Sec. 4).
- RF-60, RF-61, RF-62 (`PROJECT-README` Sec. 5.11).
- [RN-12](../../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md).
- [NB-11](../../01_necesidades_negocio/necesidades-de-negocio/NB-11-portabilidad-storage-y-config-inicial_v1.0.md).

---

**Fin del documento — ADR-02-storage-hexagonal-multi-adaptador_v1.0.md**
