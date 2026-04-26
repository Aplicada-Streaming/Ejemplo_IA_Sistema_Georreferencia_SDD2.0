**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-01 — Iniciar sesión y registrar usuario con aceptación jerárquica

**Código:** CU-01
**Actor primario:** Usuario (cualquier rol del sistema)
**Actores secundarios:** Admin raíz (acepta jefes), Jefe de área (acepta relevadores)
**Frente:** Web (todos los roles), Móvil (solo relevadores)

---

## Precondiciones

- En primera ejecución, el sistema tiene credenciales del admin raíz inicializadas (no hay registro para el admin).
- El sistema cuenta con al menos una Área dada de alta para que un jefe pueda registrarse.

## Postcondiciones

- En éxito de login: el usuario tiene un token JWT activo con su rol y área (cuando aplica).
- En éxito de registro: el usuario queda en estado `pendiente_aceptacion` hasta que su nivel jerárquico superior lo acepte.
- En aceptación: el usuario pasa a estado `activo`.

---

## Flujo principal — Login

1. El usuario accede al frontend (web o móvil).
2. El sistema le solicita email y contraseña.
3. El usuario ingresa sus credenciales.
4. El sistema valida las credenciales contra el módulo Identity.
5. El sistema valida que el rol esté permitido en este frente:
   - Móvil → solo `relevador` con estado `activo`.
   - Web → cualquier rol con estado `activo`.
6. El sistema emite un token JWT con claims: `user_id`, `role`, `area_id` (cuando aplica), `device_id` (cuando aplica).
7. El frontend redirige a la pantalla principal según el rol.

## Flujo alternativo — Registro de jefe de área

1a. Un nuevo jefe accede al frontend web y elige "Registrarme".
2a. Selecciona su Área, ingresa email, nombre completo y contraseña.
3a. El sistema crea el usuario con rol `jefe_area` y estado `pendiente_aceptacion`.
4a. El admin raíz, al ingresar al panel correspondiente, ve la solicitud y la acepta o rechaza.
5a. En aceptación: el usuario pasa a `activo` y puede loguear.
6a. En rechazo: el usuario queda en `dado_de_baja` y no puede loguear.

## Flujo alternativo — Registro de relevador

1b. Un nuevo relevador accede al frontend web y elige "Registrarme".
2b. Selecciona su Área, ingresa email, nombre completo y contraseña.
3b. El sistema crea el usuario con rol `relevador` y estado `pendiente_aceptacion`.
4b. El jefe de área correspondiente ve la solicitud y la acepta o rechaza.
5b. En aceptación: el relevador pasa a `activo` y puede loguear en web y móvil.

## Flujos de error

- E1. Credenciales inválidas → mensaje genérico ("usuario o contraseña incorrectos") sin distinguir cuál falló.
- E2. Estado `pendiente_aceptacion` → mensaje "tu cuenta está pendiente de aceptación".
- E3. Estado `inhabilitado` o `dado_de_baja` → mensaje correspondiente; bloquear login.
- E4. Login en móvil con rol no relevador → "el acceso móvil está restringido a relevadores".
- E5. Token vencido durante sesión → redirigir a login y solicitar re-autenticación.

---

## Reglas de negocio relacionadas

- [RN-11](../reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md) — Aceptación jerárquica y móvil restringido a relevadores.

## Trazabilidad

- Origen: [NB-10](../../01_necesidades_negocio/necesidades-de-negocio/NB-10-gestion-jerarquica-de-usuarios_v1.0.md).
- RFs cubiertos: RF-55, RF-56, RF-59.

## Criterios de aceptación

- **CA-01.1** — *Given* un relevador con estado `activo`, *when* ingresa email y contraseña válidos en el móvil, *then* recibe un JWT con claim `role=relevador` y accede a la pantalla principal.
- **CA-01.2** — *Given* un jefe de área con estado `activo`, *when* ingresa al móvil, *then* el sistema rechaza el login con mensaje E4.
- **CA-01.3** — *Given* un nuevo jefe en estado `pendiente_aceptacion`, *when* intenta loguear, *then* el sistema responde E2.
- **CA-01.4** — *Given* un nuevo relevador `pendiente_aceptacion`, *when* el jefe de su área lo acepta, *then* el relevador pasa a `activo` y puede loguear.
- **CA-01.5** — *Given* un usuario `inhabilitado`, *when* intenta loguear, *then* el sistema responde E3 y registra el intento en logs.
- **CA-01.6** — *Given* credenciales incorrectas, *when* se intenta login, *then* el sistema responde E1 sin distinguir cuál de los dos campos falló.

---

**Fin del documento — CU-01-iniciar-sesion-y-registro-jerarquico_v1.0.md**
