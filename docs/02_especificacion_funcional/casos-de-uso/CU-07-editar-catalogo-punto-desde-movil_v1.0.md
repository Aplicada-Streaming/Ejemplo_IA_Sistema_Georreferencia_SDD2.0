**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-07-editar-catalogo-punto-desde-movil_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-07 — Editar catálogo de punto desde móvil

**Código:** CU-07
**Actor primario:** Relevador (dueño) o Colaborador asignado
**Frente:** Móvil

## Precondiciones

- Existe un Punto en un relevamiento `abierto` al que el usuario tiene permiso (dueño del relevamiento, o creador del punto si es colaborador).
- El usuario está autenticado.

## Postcondiciones

- Se actualizan título y descripción del Punto y/o comentario de fotos.
- Se generan eventos `field_updated` por cada campo modificado.

## Flujo principal

1. El usuario hace doble-tap sobre un marcador en el mapa.
2. El sistema abre la pantalla de catálogo del Punto: previsualización de fotos, título, descripción, valores de campos definidos por la plantilla.
3. El usuario edita: título, descripción, comentario individual de cada foto, valores de campos.
4. El sistema valida según las reglas de la plantilla.
5. El usuario guarda.
6. El sistema persiste localmente y emite eventos por cada campo modificado.

## Flujos alternativos

- 1a. El usuario es colaborador y el Punto fue creado por otro → el sistema muestra el catálogo en modo lectura ([RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md)).
- 1b. El usuario es dueño del relevamiento → puede editar cualquier Punto del relevamiento, incluso los creados por colaboradores.

## Flujos de error

- E1. Validación de plantilla falla (e.g. campo requerido vacío) → el sistema impide guardar y resalta el campo.
- E2. Almacenamiento local lleno → mensaje y bloqueo.
- E3. El relevamiento se cerró desde otro dispositivo entre cargar y guardar → al sincronizar quedará en estado de revisión por [RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md).

## Reglas de negocio relacionadas

- [RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md), [RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md), [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md).

## Trazabilidad

- Origen: [NB-01](../../01_necesidades_negocio/necesidades-de-negocio/NB-01-captura-georreferenciada-en-campo_v1.0.md).
- RFs cubiertos: RF-19.

## Criterios de aceptación

- **CA-07.1** — *Given* un punto del que soy dueño, *when* doble-tap, edito comentario de foto y guardo, *then* la edición se persiste localmente y queda en outbox.
- **CA-07.2** — *Given* un punto creado por un colaborador, *when* el mismo colaborador hace doble-tap, *then* puede editarlo.
- **CA-07.3** — *Given* un punto creado por otro colaborador, *when* abro el catálogo, *then* lo veo en modo lectura.
- **CA-07.4** — *Given* dueño del relevamiento (no del punto), *when* edita el punto, *then* el sistema lo permite.

---

**Fin del documento — CU-07-editar-catalogo-punto-desde-movil_v1.0.md**
