**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** RN-06-guids-cliente-idempotencia_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# RN-06 — GUIDs en cliente e idempotencia de operaciones

## Descripción

Toda entidad creada en cualquier dispositivo (móvil o web) recibe un **GUID generado en cliente**. El backend acepta ese GUID como ID definitivo. Toda operación de creación o cambio se acompaña de su **timestamp de origen** (cuando ocurrió en el dispositivo, no cuando llegó al servidor).

La combinación (GUID, timestamp original, tipo de evento) es la clave de idempotencia: un reenvío del mismo evento no produce duplicación ni efecto secundario distinto del original.

## Origen

- [NB-02](../../01_necesidades_negocio/necesidades-de-negocio/NB-02-trabajo-offline-y-colaborativo_v1.0.md).
- DD-08, DD-09 (`PROJECT-BRIEF` Sec. 4).
- RF-37, RNF-02 (`PROJECT-README` Sec. 5.6, 6).

## CUs afectados

- [CU-04](../casos-de-uso/CU-04-crear-relevamiento_v1.0.md), [CU-06](../casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md), [CU-07](../casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md), [CU-08](../casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md), [CU-09](../casos-de-uso/CU-09-cargar-lote-fotos-web_v1.0.md).

## Prioridad

Alta.

## Ejemplos

**Aplicación correcta**
- Móvil offline crea un Punto con GUID `abc-123` a las 10:00. Sincroniza tres veces por reintentos. El backend solo registra el Punto una vez.
- Cliente edita el campo "título" de un Punto a las 11:30. Si reenvía el mismo evento, el resultado es el mismo: el título queda en su nuevo valor.

**Violaciones a detectar y rechazar**
- Backend genera ID propio sustituyendo el del cliente → no debe pasar.
- Eventos sin GUID o sin timestamp original → 400 con mensaje claro.
- Backend procesa dos veces el mismo evento (mismo GUID y timestamp) cambiando estado dos veces → no debe pasar.

---

**Fin del documento — RN-06-guids-cliente-idempotencia_v1.0.md**
