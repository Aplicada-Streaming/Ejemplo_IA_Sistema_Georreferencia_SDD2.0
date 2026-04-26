**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-08-modo-movil-radio_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-08 — Modo móvil con radio configurable

**Épica:** EP-01.3 · **MoSCoW:** Must · **SP:** 8 · **Sprint sugerido:** Slice 3

> Como **relevador recorriendo un tramo a pie**,
> quiero **que las fotos sucesivas se asocien automáticamente al mismo punto mientras esté dentro de un radio configurable, y que se cree un nuevo punto al salir del radio**,
> para **capturar rápido sin tener que confirmar punto por punto**.

## CUs y RNs relacionados
- CU: [CU-06](../../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md)

## Alcance
- Toggle de modo `detenido` / `movil` siempre visible en mapa.
- Lectura del radio desde la versión de plantilla del relevamiento.
- Lógica: dentro del radio del último punto creado en sesión de modo móvil → asocia; fuera → crea nuevo punto.
- Conmutación entre modos en cualquier momento sin perder contexto.
- Indicador visual del radio del modo activo.

## Criterios de aceptación
- **CA-8.1** Modo móvil con radio 10m: foto a 5m del punto → asocia. Foto a 15m → crea nuevo punto.
- **CA-8.2** Cambio a modo `detenido` durante captura no resetea el marcador actual.
- **CA-8.3** Plantilla con radio distinto al default → comportamiento usa el valor de la plantilla.
- **CA-8.4** Indicador del radio dibuja un círculo alrededor del punto activo.

## Dependencias
- US-07, US-06.

## DoR — checklist
- [x] Atada a EP-01.3.
- [x] Criterios verificables.
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-08**
