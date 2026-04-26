**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-02-configurar-storage_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-02 — Configurar storage (primer arranque y reconfiguración)

**Código:** CU-02
**Actor primario:** Admin raíz
**Frente:** Web

## Precondiciones

- El admin raíz está autenticado.
- En primer arranque: la tabla `ConfiguraciónSistema` está vacía y el sistema redirige al wizard al loguearse.

## Postcondiciones

- El sistema tiene `ConfiguraciónSistema` persistida con storage activo y credenciales válidas.
- Los registros previos de Foto conservan su referencia al adaptador con el que fueron creados ([RN-12](../reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)).

## Flujo principal — Wizard de primer arranque

1. El admin raíz ingresa al sistema y el frontend detecta que no hay configuración persistida.
2. El sistema redirige al wizard.
3. El sistema solicita: tipo de storage (`local`, `s3`, `ftp`, `sftp`), credenciales del adaptador elegido (path raíz local, bucket+keys S3, host+credenciales FTP/SFTP).
4. El admin completa los datos.
5. El sistema valida la conexión: hace una operación de prueba (escribir archivo dummy + leer + borrar).
6. Si la prueba pasa: persiste la configuración con cifrado de credenciales.
7. El sistema redirige a la pantalla principal.

## Flujo alternativo — Reconfiguración posterior

1a. El admin raíz accede al panel de "Configuración del sistema".
2a. Ve la configuración actual y selecciona "Cambiar storage".
3a. Repite los pasos 3 a 7 del flujo principal.
4a. Tras guardar: las nuevas Fotos van al nuevo adaptador. Las Fotos previas siguen leyendo desde el adaptador anterior según su referencia almacenada (ver [RN-12](../reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)).

## Flujos de error

- E1. La validación de conexión falla → el sistema muestra el error puntual (credenciales inválidas, host inalcanzable, permisos insuficientes) y no persiste.
- E2. Credenciales sin permisos de escritura → la prueba detecta la falla y la reporta.
- E3. El admin cancela el wizard de primer arranque → el sistema queda sin storage configurado y bloquea creación de relevamientos hasta que se configure.

## Reglas de negocio relacionadas

- [RN-12](../reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md) — Storage: datos previos siguen en su adaptador original.

## Trazabilidad

- Origen: [NB-11](../../01_necesidades_negocio/necesidades-de-negocio/NB-11-portabilidad-storage-y-config-inicial_v1.0.md).
- RFs cubiertos: RF-60, RF-61, RF-62.

## Criterios de aceptación

- **CA-02.1** — *Given* primer arranque del sistema, *when* el admin raíz se loguea, *then* el frontend lo redirige al wizard de configuración.
- **CA-02.2** — *Given* el wizard activo, *when* el admin completa adaptador local con un path inválido (no existe o sin permisos), *then* el sistema responde E1/E2 y no persiste.
- **CA-02.3** — *Given* configuración persistida con `local`, *when* el admin reconfigura a `s3` con credenciales válidas, *then* nuevas fotos suben a S3 y fotos previas siguen leyéndose del path local.
- **CA-02.4** — *Given* admin no admin raíz, *when* intenta acceder al panel de configuración, *then* el sistema rechaza con 403.
- **CA-02.5** — *Given* validación de conexión exitosa, *when* el admin guarda, *then* las credenciales se persisten cifradas.

---

**Fin del documento — CU-02-configurar-storage_v1.0.md**
