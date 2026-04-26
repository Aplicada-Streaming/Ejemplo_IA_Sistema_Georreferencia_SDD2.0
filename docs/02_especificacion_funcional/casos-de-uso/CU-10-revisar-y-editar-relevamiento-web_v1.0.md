**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-10-revisar-y-editar-relevamiento-web_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-10 — Revisar y editar relevamiento desde web

**Código:** CU-10
**Actor primario:** Jefe de área, Relevador (dueño), Colaborador asignado
**Frente:** Web

## Precondiciones

- Existe el Relevamiento con al menos un Punto.
- El usuario tiene permiso (jefe del área, dueño o colaborador).

## Postcondiciones

- Se aplican ediciones de título, descripción, comentarios y campos respetando permisos por punto.
- Se generan eventos de auditoría por cada campo modificado.

## Flujo principal — Revisar catálogo

1. El usuario abre el relevamiento desde la lista (CU-05).
2. El sistema muestra un layout con: mapa colaborativo + listado de puntos + catálogo de fotos.
3. El usuario alterna entre **vista por punto** (fotos agrupadas por punto) y **vista plana** (todas las fotos del relevamiento).
4. El mapa muestra los marcadores con diferenciación visual por colaborador (color/ícono distinto por usuario).
5. El usuario aplica filtros: "ver solo mis puntos" o "ver todos los puntos del relevamiento".
6. Los puntos editados o con actividad reciente (≤ 24h) se distinguen con indicador visual.

## Flujo alternativo — Editar punto

1a. El usuario hace click sobre un Punto (en mapa o lista).
2a. El sistema abre el panel del Punto: título, descripción, fotos con comentarios, valores de plantilla.
3a. Si el usuario tiene permiso de edición ([RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md)):
   - Edita título, descripción, comentarios de fotos, valores de campos.
   - Agrega o elimina fotos respetando reglas.
4a. El sistema valida con la versión de plantilla del relevamiento.
5a. Al guardar: persiste y emite eventos `field_updated` por campo modificado.

## Flujo alternativo — Cada foto enlaza al mapa

1b. Bajo cada foto, el sistema muestra su comentario editable y un enlace clickeable a la ubicación de su Punto en el mapa.

## Flujos de error

- E1. Validación de plantilla falla → el sistema impide guardar con mensaje específico.
- E2. Permiso insuficiente para editar el campo → vista en modo lectura ([RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md)).
- E3. El relevamiento se cerró desde otro dispositivo durante la edición → mensaje y oferta de reabrir si el usuario es dueño.

## Reglas de negocio relacionadas

- [RN-01](../reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md), [RN-08](../reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md), [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md).

## Trazabilidad

- Origen: [NB-06](../../01_necesidades_negocio/necesidades-de-negocio/NB-06-revision-y-consolidacion-en-gabinete_v1.0.md), [NB-09](../../01_necesidades_negocio/necesidades-de-negocio/NB-09-visibilidad-de-actividad-colaborativa_v1.0.md).
- RFs cubiertos: RF-29, RF-30, RF-31, RF-32, RF-33, RF-52, RF-53, RF-54.

## Criterios de aceptación

- **CA-10.1** — *Given* un relevamiento con puntos de dos colaboradores, *when* el jefe abre el mapa, *then* los puntos se diferencian visualmente por colaborador.
- **CA-10.2** — *Given* el filtro "ver solo mis puntos" activo, *when* el usuario lo activa, *then* el mapa filtra correctamente.
- **CA-10.3** — *Given* un punto editado hace 1h, *when* el usuario abre el mapa, *then* el indicador de "actividad reciente" está visible.
- **CA-10.4** — *Given* un colaborador, *when* abre un punto creado por otro colaborador, *then* puede ver y agregar fotos propias pero no editar las creadas por otro.
- **CA-10.5** — *Given* el dueño del relevamiento, *when* edita campos de un punto creado por un colaborador, *then* el sistema permite y registra evento.
- **CA-10.6** — *Given* la vista plana, *when* el usuario clickea el enlace de mapa de una foto, *then* el mapa centra y resalta el Punto correspondiente.

---

**Fin del documento — CU-10-revisar-y-editar-relevamiento-web_v1.0.md**
