**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** CU-04-crear-relevamiento_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# CU-04 — Crear relevamiento

**Código:** CU-04
**Actor primario:** Relevador (dueño) o Jefe de área
**Frente:** Web o Móvil

## Precondiciones

- El usuario está autenticado y pertenece a una Área.
- Existe al menos una versión publicada de Plantilla disponible.
- Existe `ConfiguraciónSistema` válida (sino, [CU-02](CU-02-configurar-storage_v1.0.md) primero).

## Postcondiciones

- Se crea un Relevamiento con un GUID generado en cliente, en estado `abierto`, atado a la versión de plantilla seleccionada y al área del usuario.
- Se genera un EventoDeAuditoría `created` para la entidad Relevamiento.

## Flujo principal

1. El usuario selecciona "Nuevo relevamiento".
2. El sistema solicita: nombre, descripción, plantilla (con su versión publicada), etiquetas opcionales.
3. El frontend genera un GUID local para el relevamiento.
4. El usuario confirma.
5. El sistema persiste el Relevamiento en estado `abierto` y emite el evento `created`.
6. Si el origen es móvil offline: el evento queda en la outbox local y el relevamiento es visible y operable inmediatamente; se sincroniza en próximo sync.
7. El sistema redirige al detalle del relevamiento, listo para capturar puntos.

## Flujos alternativos

- 1a. Si el usuario es jefe de área, puede crear el relevamiento y asignarse otro relevador como dueño en el mismo paso (continúa con [CU-05](CU-05-asignar-colaboradores-y-ciclo-vida_v1.0.md)).

## Flujos de error

- E1. No hay plantillas publicadas disponibles → mensaje y bloqueo de creación.
- E2. Storage no configurado en backend → al sincronizar fallará el evento; el sistema avisa al admin raíz.
- E3. Conexión perdida en web durante creación → el sistema reintenta o avisa.

## Reglas de negocio relacionadas

- [RN-06](../reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md) — GUIDs en cliente para idempotencia.
- [RN-10](../reglas-de-negocio/RN-10-eventos-append-only_v1.0.md) — Eventos append-only.

## Trazabilidad

- Origen: [NB-04](../../01_necesidades_negocio/necesidades-de-negocio/NB-04-gestion-ciclo-vida-relevamiento_v1.0.md).
- RFs cubiertos: RF-01, RF-11.

## Criterios de aceptación

- **CA-04.1** — *Given* un relevador autenticado, *when* crea un relevamiento eligiendo plantilla "Inspección de puente v1", *then* el relevamiento queda persistido con GUID generado en cliente y referencia a esa versión.
- **CA-04.2** — *Given* un relevador offline, *when* crea un relevamiento desde el móvil, *then* el relevamiento es operable de inmediato y el evento queda en outbox.
- **CA-04.3** — *Given* un relevamiento creado, *when* se consulta el log de eventos, *then* aparece un evento `created` con autor, timestamp y origen.
- **CA-04.4** — *Given* dos sincronizaciones del mismo evento `created` (reenvío forzado), *when* el backend recibe el segundo, *then* no se duplica (idempotencia por GUID).
- **CA-04.5** — *Given* no hay plantillas publicadas, *when* el usuario intenta crear relevamiento, *then* el sistema lo bloquea con E1.

---

**Fin del documento — CU-04-crear-relevamiento_v1.0.md**
