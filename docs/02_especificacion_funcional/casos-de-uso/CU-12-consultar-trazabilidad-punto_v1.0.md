**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-12-consultar-trazabilidad-punto_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-12 — Consultar trazabilidad histórica de un punto

**Código:** CU-12
**Actor primario:** Jefe de área, Relevador (dueño), Colaborador
**Frente:** Web

## Precondiciones

- Existe el Punto.
- El usuario tiene permiso de lectura sobre el relevamiento.

## Postcondiciones

- El usuario ve la metadata de origen y el histórico completo de cambios del Punto.

## Flujo principal

1. El usuario abre un Punto desde la web (CU-10).
2. Selecciona la pestaña "Trazabilidad" o "Histórico".
3. El sistema muestra:
   - **Metadata de origen**: creador, fecha, frente (móvil/web), modo de captura, device_id.
   - **Histórico cronológico de eventos**: para cada evento, quién, cuándo, qué campo, valor anterior y nuevo, origen.
   - **Eventos especiales**: fusiones (`merged`) con referencia a los Puntos originales y los valores resultantes; eliminaciones lógicas y restauraciones (`deleted` / `restored`).
4. El usuario puede filtrar el histórico por autor, por tipo de evento, por rango de fechas.

## Flujos alternativos

- 1a. Si el Punto fue resultado de una fusión, el histórico muestra también los eventos heredados de los Puntos originales con marca clara de "previo a la fusión".
- 1b. La consulta se exporta opcionalmente a CSV para análisis externo (`[REQUIERE_INFO]` si el cliente lo pide; en MVP es solo visual).

## Flujos de error

- E1. El Punto fue eliminado lógicamente → el sistema muestra el histórico hasta el evento `deleted` con marca visual.

## Reglas de negocio relacionadas

- [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md) — Eventos append-only.

## Trazabilidad

- Origen: [NB-08](../../01_necesidades_negocio/necesidades-de-negocio/NB-08-trazabilidad-tecnica-de-cambios_v1.0.md).
- RFs cubiertos: RF-49, RF-50, RF-51.

## Criterios de aceptación

- **CA-12.1** — *Given* un Punto con 5 eventos de edición, *when* abro Trazabilidad, *then* veo los 5 eventos en orden cronológico con autor, timestamp, campo y valores.
- **CA-12.2** — *Given* un Punto fusionado, *when* abro Trazabilidad, *then* veo los eventos previos a la fusión, el evento `merged` y los eventos posteriores.
- **CA-12.3** — *Given* un Punto recién creado, *when* abro Trazabilidad, *then* veo solo el evento `created` con metadata de origen completa.
- **CA-12.4** — *Given* un Punto con eventos de varios autores, *when* filtro por autor X, *then* solo veo los eventos de X.
- **CA-12.5** — *Given* un evento de auditoría persistido, *when* alguien intenta editarlo o eliminarlo, *then* el sistema lo rechaza ([RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md)).

---

**Fin del documento — CU-12-consultar-trazabilidad-punto_v1.0.md**
