**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-12-storage-datos-previos_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-12 — Storage: datos previos siguen en su adaptador original

## Descripción

Cuando el admin raíz cambia el adaptador de storage configurado (de `local` a `s3`, por ejemplo), las Fotos previamente persistidas **conservan su referencia** al adaptador con el que fueron creadas. Cada Foto guarda explícitamente:

- El identificador del archivo dentro del adaptador.
- El nombre del adaptador con el que fue creada (`local` / `s3` / `ftp` / `sftp`).

Las **nuevas** Fotos van al adaptador activo en el momento de su creación.

La **migración masiva** entre adaptadores está explícitamente fuera del MVP ([alcance EX-02](../../00_contexto/alcance-proyecto_v1.0.md)). Se evaluará en una fase posterior si el cliente lo prioriza.

## Origen

- [NB-11](../../01_necesidades_negocio/necesidades-de-negocio/NB-11-portabilidad-storage-y-config-inicial_v1.0.md).
- RF-62 (`PROJECT-README` Sec. 5.11).
- RNF-04 (`PROJECT-README` Sec. 6).

## CUs afectados

- [CU-02](../casos-de-uso/CU-02-configurar-storage_v1.0.md), [CU-06](../casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md), [CU-09](../casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md), [CU-10](../casos-de-uso/CU-10-revisar-y-editar-relevamiento-web_v1.0.md).

## Prioridad

Media.

## Ejemplos

**Aplicación correcta**
- Sistema configurado con `local`. Foto F1 creada → referencia local.
- Admin cambia a `s3`. Foto F2 creada → referencia S3.
- Al consultar las dos fotos: F1 se lee del filesystem local, F2 se lee de S3. Ambas funcionan.

**Violaciones a detectar y rechazar**
- El sistema asume que todas las fotos están en el adaptador activo y falla al leer las antiguas → no debe pasar.
- Migración silenciosa de archivos al cambiar adaptador → no debe pasar (el alcance lo excluye).

---

**Fin del documento — RN-12-storage-datos-previos_v1.0.md**
