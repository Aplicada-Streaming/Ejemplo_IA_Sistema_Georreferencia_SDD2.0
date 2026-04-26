**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** US-01-login-autenticacion-end-to-end_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-06 via orquestador

---

# US-01 — Login y autenticación end-to-end

**Épica:** EP-00.1 Walking Skeleton · **MoSCoW:** Must · **SP:** 5 · **Sprint sugerido:** Sprint 0

> Como **usuario del sistema (cualquier rol)**,
> quiero **poder loguear con email y contraseña en web (todos los roles activos) o en móvil (solo relevadores activos)**,
> para **acceder a mi sesión autenticada y operar según mi rol y área**.

## CUs y RNs relacionados
- CU: [CU-01](../../02_especificacion_funcional/casos-de-uso/CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md)
- RN: [RN-11](../../02_especificacion_funcional/reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md)

## Alcance de la US
- Endpoint `POST /api/v1/auth/login` con ROPC + JWT bearer.
- Endpoint `POST /api/v1/auth/refresh`.
- Validación de rol vs. frente (móvil restringido a relevador).
- Persistencia y validación de tokens.
- Pantallas de login en web y móvil.
- Manejo de estados (`pendiente_aceptacion`, `inhabilitado`, `dado_de_baja`).
- Datos seed mínimos: admin raíz inicial, una área, un relevador `activo`, un jefe `activo`.

## Criterios de aceptación (Given/When/Then)
- **CA-1.1** Login válido en móvil con relevador activo → JWT emitido + redirige a lista de relevamientos.
- **CA-1.2** Login en móvil con jefe `activo` → 403 con mensaje E4.
- **CA-1.3** Login con usuario `pendiente_aceptacion` → 403 con mensaje E2.
- **CA-1.4** Credenciales inválidas → 401 con mensaje genérico (E1).
- **CA-1.5** Token JWT contiene claims `user_id`, `role`, `area_id` (si aplica).

## Dependencias
- BT-01 (setup repo), BT-04 (OpenAPI), BT-05 (logging), BT-06 (migraciones).

## DoR — checklist
- [x] Atada a EP-00.1 y CU-01.
- [x] Criterios de aceptación verificables.
- [x] Estimada (5 SP).
- [x] No depende de `[REQUIERE_INFO]` bloqueante.
- [x] Cabe en un sprint con 3 capas (DB, backend, ambos frentes).
- [x] Sin impacto en plantillas / sync / storage.
- [x] PO confirma valor.

---
**Fin — US-01**
