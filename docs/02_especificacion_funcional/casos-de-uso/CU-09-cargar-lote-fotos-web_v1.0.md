**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-09-cargar-lote-fotos-web_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-09 — Cargar lote de fotos previas desde web (EXIF + georreferenciación manual)

**Código:** CU-09
**Actor primario:** Jefe de área o Relevador (dueño)
**Frente:** Web

## Precondiciones

- El usuario está autenticado y es dueño del relevamiento o jefe del área del relevamiento.
- El relevamiento está `abierto`.
- Existe `ConfiguraciónSistema` con storage activo válido.

## Postcondiciones

- Las fotos del lote quedan asociadas al relevamiento, agrupadas en Puntos según el modo elegido.
- Las fotos sin EXIF GPS quedan en cola de "fotos pendientes de georreferenciar" hasta que el usuario les asigne coordenadas.

## Flujo principal

1. El usuario accede al detalle del relevamiento y selecciona "Subir lote de fotos".
2. El sistema solicita el **modo de agrupación** (`detenido` vs. `movil` con radio configurable).
3. El usuario sube N archivos de imagen.
4. El backend procesa cada foto:
   - Extrae EXIF (GPS, timestamp).
   - Si **tiene GPS**: agrupa según el modo seleccionado por proximidad espacial y temporal, creando o reutilizando Puntos.
   - Si **no tiene GPS**: la foto queda en cola con el lote, asociada al relevamiento pero sin Punto.
5. Las fotos con coordenadas generan Puntos nuevos (con GUID generado en el backend) y/o se asocian a Puntos cercanos según modo.
6. El sistema genera comentarios genéricos iniciales: "Cargado el [fecha] desde web".
7. El usuario puede entrar a editar el comentario después.

## Flujo alternativo — Georreferenciar fotos sin EXIF

1a. El usuario abre la cola de "fotos pendientes de georreferenciar".
2a. Para cada foto: ingresa lat/lng manualmente o selecciona la posición en un picker en mapa.
3a. Al confirmar: se crea un Punto nuevo o se asocia a uno existente según proximidad.

## Flujos de error

- E1. La foto no es un formato soportado → se reporta y se descarta del lote.
- E2. EXIF inválido o corrupto → la foto va a la cola de pendientes.
- E3. Storage caído al subir → se reintenta; si falla persistentemente, el lote queda en estado "fallido" y el usuario reintenta.
- E4. Relevamiento cerrado durante el proceso → el lote queda rechazado.

## Reglas de negocio relacionadas

- [RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md), [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md), [RN-12](../reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md).

## Trazabilidad

- Origen: [NB-05](../../01_necesidades_negocio/necesidades-de-negocio/NB-05-onboarding-relevamientos-previos_v1.0.md).
- RFs cubiertos: RF-24, RF-25, RF-26, RF-27, RF-28.

## Criterios de aceptación

- **CA-09.1** — *Given* lote de 50 fotos con EXIF GPS, modo `movil` con radio 10m, *when* se procesan, *then* se crean Puntos por proximidad espacial y temporal, agrupando fotos cercanas.
- **CA-09.2** — *Given* 5 fotos del lote sin EXIF GPS, *when* el procesamiento termina, *then* las 5 quedan en cola de "pendientes de georreferenciar".
- **CA-09.3** — *Given* una foto pendiente, *when* el usuario fija coordenadas en el picker en mapa, *then* se crea un Punto en esa posición y la foto se asocia.
- **CA-09.4** — *Given* el origen `web_manual_upload` en cada Foto, *when* se sincroniza con el móvil, *then* la app móvil muestra esas fotos como creadas desde web.
- **CA-09.5** — *Given* un comentario genérico inicial "Cargado el 2026-04-26 desde web", *when* el usuario lo edita, *then* el sistema persiste el nuevo valor con evento `field_updated`.

---

**Fin del documento — CU-09-cargar-lote-fotos-web_v1.0.md