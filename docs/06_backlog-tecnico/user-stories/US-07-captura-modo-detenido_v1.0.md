**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-07-captura-modo-detenido_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-07 — Captura modo detenido con diálogo unificado

**Épica:** EP-01.2 · **MoSCoW:** Must · **SP:** 13 · **Sprint sugerido:** Slice 2

> Como **relevador en campo**,
> quiero **tomar fotos asociadas a un punto georreferenciado en modo detenido, gestionando permisos, GPS, timeout y reintento en un único diálogo**,
> para **capturar puntos con confianza incluso si los permisos o el GPS fallan**.

## CUs y RNs relacionados
- CU: [CU-06](../../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md), [CU-07](../../02_especificacion_funcional/casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md)
- RN: [RN-06](../../02_especificacion_funcional/reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md), [RN-08](../../02_especificacion_funcional/reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md)

## Alcance
- Diálogo unificado con máquina de estados S0–S3 (PROJECT-BRIEF Sec. 7).
- Asociación de la foto al marcador actual o creación de nuevo punto si no hay marcador.
- Persistencia local + outbox.
- Procesamiento local de la foto (resize, thumb) según parámetros de plantilla.
- Catálogo de punto: doble-tap para edición de título/descripción/comentarios.

## Criterios de aceptación
- **CA-7.1** Tap en cámara → diálogo S0 → S2 → S3-OK → cámara nativa.
- **CA-7.2** Permiso de ubicación denegado → diálogo S1-LOC-DENY con CTA configuración.
- **CA-7.3** GPS sin fix tras timeout → S3-TIMEOUT con reintentar.
- **CA-7.4** Captura con marcador seleccionado → asocia al marcador actual sin crear nuevo punto.
- **CA-7.5** Captura sin marcador → crea nuevo punto.
- **CA-7.6** Doble-tap sobre punto abre catálogo editable.

## Dependencias
- US-03, US-06, BT-09.

## DoR — checklist
- [x] Atada a EP-01.2.
- [x] Criterios verificables (testing en emulador).
- [x] Estimada.
- [x] Cabe en un sprint.
- [x] PO confirma.

---
**Fin — US-07**
