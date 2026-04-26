**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** definition-of-done_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# Definition of Done (DoD) Canónico

DoD canónico del proyecto, que extiende el DoD de alto nivel del [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) Sec. 5. Una US o BT solo se reporta como `done` cuando cumple **todos** los criterios de las tres dimensiones aplicables.

---

## 1. Dimensión "Código"

- [ ] **PR mergeado** a la rama principal con al menos un revisor distinto del autor (dos revisores para módulos críticos: Sync, Storage, Identity).
- [ ] **Build pasa en CI** (sin warnings nuevos respecto al baseline).
- [ ] **Linting pasa** (.NET analyzers + dotnet format) sin reglas suprimidas sin justificación.
- [ ] **Tests automatizados pasan en CI**:
  - Unit tests del módulo / componente afectado.
  - Integration tests cuando aplique.
  - Architecture tests (si la US toca módulos: validan que no se violen las reglas de dependencia).
- [ ] **Cobertura de tests no degradada** respecto al baseline (líneas y ramas), salvo justificación documentada en el PR.
- [ ] **Sin TODOs sin issue asociado** en el código nuevo.
- [ ] **Sin secretos hardcodeados** ni en código ni en archivos de configuración.

---

## 2. Dimensión "Funcional"

- [ ] **Todos los criterios de aceptación** de la US (CA-XX.Y) están **verificados**:
  - Mediante test automatizado cuando es posible.
  - Mediante validación manual documentada en el PR cuando no.
- [ ] **Validación funcional manual** sobre el slice end-to-end ejecutada al menos una vez.
- [ ] **Logs / errores observables** en consola del backend con correlation id.
- [ ] **Trazabilidad mínima** del flujo de la US (eventos relevantes generan registros en `AuditEvents`).

### 2.1. Para US que tocan **sincronización**:
- [ ] Prueba con **dos clientes simulados o dispositivos físicos** ejecutada y registrada.
- [ ] Idempotencia validada: reenvío del mismo evento no produce duplicación.
- [ ] LWW por campo verificado con timestamps controlados.
- [ ] Si aplica: precedencia del dueño verificada con escenario realista.

### 2.2. Para US que tocan **plantillas**:
- [ ] Prueba con **al menos dos plantillas distintas** (raíz + una hija) ejecutada.
- [ ] Renderizado dinámico validado en web y móvil.
- [ ] Validaciones de plantilla aplicadas correctamente.

### 2.3. Para US que tocan **roles o permisos**:
- [ ] **Matriz de permisos** ejecutada con al menos los roles afectados (admin raíz, jefe, dueño, colaborador, otro usuario).
- [ ] Negaciones (403) verificadas explícitamente, no solo los happy paths.

### 2.4. Para US que tocan **storage**:
- [ ] Tests de integración del adaptador correspondiente pasan (si es nuevo o se modificó).
- [ ] Persistencia de `adapter_name` en `Photos` validada.
- [ ] Lectura tras cambio de adaptador validada (compatibilidad con datos previos).

### 2.5. Para US que tocan **móvil**:
- [ ] Validación en **emulador** ejecutada.
- [ ] Validación en **dispositivo físico** registrada en Sprint Review.
- [ ] Comportamiento offline verificado para flujos críticos.

---

## 3. Dimensión "Proceso"

- [ ] **Documentación viva actualizada**:
  - OpenAPI del backend (regenerado).
  - README del módulo afectado si cambió la responsabilidad.
  - `/docs/` actualizado si la US toca decisiones de diseño.
- [ ] **Migraciones EF Core revisadas** y aplicadas en local antes de mergear.
- [ ] **Si la US incurrió en deuda técnica**: documentada como `DT-XX` con plan de revisión.
- [ ] **PO acepta la US** según los criterios de aceptación, registrado en la herramienta de gestión.
- [ ] **Issues bloqueantes detectados durante la implementación** registrados en backlog (no se barren bajo la alfombra).

---

## 4. Excepciones documentadas

Una US puede reportarse como `done` con un criterio del DoD **no cumplido** solo si:
1. El criterio no es aplicable (e.g. una BT de docs no tiene tests de integración).
2. El criterio se difiere conscientemente, documentado en el PR + ítem en backlog para resolverlo.
3. El PO + Tech Lead aceptan la excepción.

Las excepciones se registran en una sección del Sprint Retrospective.

---

## 5. DoD por release (R-Alpha, R-Beta, R-MVP)

Antes de cada release del [roadmap](../00_contexto/roadmap-producto_v1.0.md), además del DoD por US:

- [ ] Suite E2E completa pasa.
- [ ] Tests manuales exploratorios sobre el flujo de cada slice incluido en el release.
- [ ] Documentación operativa actualizada (BT-14).
- [ ] Demo al sponsor ejecutada y firmada.

---

## 6. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| [acuerdo-equipo](../00_contexto/acuerdo-equipo_v1.0.md) Sec. 5 | DoD de alto nivel del que este documento extiende |
| [estrategia-testing](estrategia-testing_v1.0.md) | Tipos de pruebas que el DoD exige |
| Casos de uso CU-* | Criterios de aceptación que el DoD obliga a verificar |

---

**Fin del documento — definition-of-done_v1.0.md**
