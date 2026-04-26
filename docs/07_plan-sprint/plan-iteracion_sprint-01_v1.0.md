**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** plan-iteracion_sprint-01_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-07 via orquestador

---

# Sprint 1 — Slice 1: Sincronización entre dos dispositivos

**Objetivo del sprint:** dos relevadores trabajando offline en el mismo relevamiento crean puntos vacíos, sincronizan al volver a conexión y ven los puntos del otro, con idempotencia, LWW por campo y precedencia del dueño aplicados correctamente.

**Fechas:** [REQUIERE_INFO]
**Duración:** 2 semanas
**Velocidad comprometida:** 34 SP (a calibrar tras Sprint 0).
**Dependencia bloqueante:** Sprint 0 + Spike BT-07 completados.

---

## 1. US comprometidas

| US | Descripción | Puntos | Owner | Estado |
|---|---|---|---|---|
| [US-03](../06_backlog-tecnico/user-stories/US-03-outbox-local-reintentos_v1.0.md) | Outbox local móvil con reintentos exponenciales | 13 | Móvil | Comprometida |
| [US-04](../06_backlog-tecnico/user-stories/US-04-push-eventos-idempotencia_v1.0.md) | Push de eventos al backend con idempotencia | 13 | Backend | Comprometida |
| [US-05](../06_backlog-tecnico/user-stories/US-05-pull-diferencial_v1.0.md) | Pull diferencial de eventos al móvil | 8 | Backend + Móvil | Comprometida |

**Total:** 34 SP.

> Slice 1 es deliberadamente focalizado en sync porque es la complejidad central. Otras capacidades (captura con foto + plantilla) se incorporan en Slice 2 (Sprint 2).

---

## 2. Criterio de éxito del sprint

- ✅ Dos relevadores ofline crean Puntos vacíos (sin foto, solo coordenadas dummy aceptadas) y sincronizan al volver a conexión.
- ✅ Cada uno ve los Puntos del otro tras pull.
- ✅ Reintentos automáticos por outbox son transparentes; un corte de red no pierde datos.
- ✅ Edición concurrente del mismo campo se resuelve LWW por campo + precedencia del dueño.
- ✅ Notificación post-sync con resumen de cambios.

---

## 3. Demo del Sprint Review

Configurar dos dispositivos en aviones (sin red simulado):

1. Dispositivo A (relevador dueño) crea relevamiento + Punto P1 con coordenadas dummy.
2. Dispositivo B (colaborador) recibe el relevamiento (vía sync inicial con conexión) y luego se desconecta.
3. Ambos dispositivos editan el campo `título` de P1 con valores distintos.
4. Ambos dispositivos vuelven a conexión y sincronizan.
5. Mostrar resultado: el valor del dueño prevalece, ambos dispositivos convergen, el log de eventos refleja la trazabilidad.

---

## 4. Riesgos

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| Drift de reloj del cliente afecta LWW | Baja | Validación al push con umbral configurable (ADR-03 Sec. Riesgos). |
| Reintentos exponenciales tienen bug en estado `terminal_error` | Media | Tests unitarios cubriendo el ciclo completo de reintentos. |
| Performance del pull diferencial con histórico grande | Baja en Slice 1 | Para MVP no se anticipa volumen alto; revisar si surge en piloto. |

---

## 5. Dependencias con otros sprints

- **Upstream:** Sprint 0 (auth, scripts, DB) + Spike BT-07 (validación protocolo).
- **Downstream:** Slice 2 (Sprint 2) **depende** de Slice 1 para tener sync funcionando antes de agregar captura real.

---

## 6. Pendientes y supuestos

- Asume dos dispositivos físicos disponibles para testing (o un dispositivo + emulador con red controlable).
- Asume que el spike BT-07 validó el protocolo; cualquier hallazgo del spike se incorpora antes del sprint planning.

---

**Fin del documento — plan-iteracion_sprint-01_v1.0.md**
