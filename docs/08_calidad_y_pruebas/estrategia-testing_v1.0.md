**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** estrategia-testing_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-08 via orquestador

---

# Estrategia de Testing

Define la pirámide de pruebas, las herramientas, los criterios de cobertura y la responsabilidad por capa. Esta estrategia se aplica desde el Sprint 0 a través de la pipeline CI (BT-02) y se exige en cada DoD.

---

## 1. Pirámide de testing

Distribución objetivo de cantidad de tests:

| Capa | Proporción | Tipo | Frecuencia de ejecución |
|---|---|---|---|
| **Unit** | 65% | Unidades de dominio (módulos), validaciones, reglas de negocio puras | Cada commit (CI), local antes de PR |
| **Integration** | 20% | Backend con DB real (testcontainers), workers con outbox, adaptadores de storage con servicios reales | Cada PR + nightly |
| **Contract** | 5% | OpenAPI conformance entre cliente y backend | Cada commit que toca contrato |
| **E2E** | 7% | Flujos completos atravesando móvil/web + backend + DB | Pre-release de cada slice |
| **Manual exploratoria** | 3% | UX en móvil real, cámara, GPS, escenarios de campo | Sprint Review + pre-release |

> Esta distribución prioriza tests rápidos donde el volumen es alto y reserva los costosos para flujos críticos. La sincronización multi-colaborador es la complejidad central y concentra una fracción mayor de tests E2E que la proporción típica.

### Justificación
- **Unit alto** porque las reglas de negocio (LWW, permisos por punto, validaciones de plantilla) son pura lógica y costo bajo.
- **Integration moderado** para validar el comportamiento real de los adaptadores de storage, EF Core con SQL Server y los workers.
- **E2E focalizado en sync** porque es el riesgo dominante; no buscamos cobertura E2E completa de toda la UI.

---

## 2. Tipos de pruebas y herramientas

### 2.1. Backend
- **Unit:** xUnit + FluentAssertions + Moq.
- **Integration:** xUnit + Testcontainers (SQL Server, Minio para S3, vsftpd para FTP/SFTP).
- **Contract:** validación de OpenAPI generado contra schemas snapshot.
- **Architecture tests:** NetArchTest verificando que módulos no acceden tablas ajenas (ADR-01).

### 2.2. Frontend web (Blazor Server)
- **Unit / Component:** bUnit para componentes Blazor.
- **E2E:** Playwright sobre el navegador, ejecutado en CI.

### 2.3. Frontend móvil (MAUI Blazor Hybrid)
- **Unit:** xUnit con la lógica del proyecto compartido (no MAUI-dependiente).
- **UI tests:** Xamarin.UITest o equivalente — alcance limitado en MVP por costo de mantenimiento.
- **Manual exploratoria:** dispositivos físicos en cada Sprint Review para flujos críticos (captura, sync, permisos).

### 2.4. Sincronización multi-colaborador
- **Tests E2E con dos clientes simulados** (procesos paralelos) atravesando el API.
- **Suite "race scenarios"** específica con escenarios LWW, precedencia del dueño, idempotencia, post-cierre, candidatos a fusión.
- **Tests con red simulada** (latencia, cortes) para validar el outbox.

---

## 3. Cobertura objetivo

| Métrica | Objetivo | Cómo se mide |
|---|---|---|
| Cobertura de líneas (módulos de dominio) | ≥ 80% | Coverlet en CI |
| Cobertura de ramas (lógica de sync y permisos) | ≥ 90% | Coverlet en CI |
| % de criterios de aceptación de CUs cubiertos por tests automatizados | ≥ 80% para CUs críticos (CU-06, CU-07, CU-08, CU-11), ≥ 60% para el resto | Matriz de cobertura |
| Cobertura E2E de flujos del roadmap | 100% para los 5 flujos críticos de sync | Suite Playwright |

> El % de cobertura no es un fin en sí. Una US no se reporta como `done` solo por superar 80% si los criterios de aceptación no están cubiertos por tests significativos.

---

## 4. CUs críticos para testing prioritario

| CU | Por qué es crítico |
|---|---|
| [CU-06](../02_especificacion_funcional/casos-de-uso/CU-06-capturar-punto-georreferenciado_v1.0.md) | Captura es el punto de entrada de toda la información. |
| [CU-07](../02_especificacion_funcional/casos-de-uso/CU-07-editar-catalogo-punto-desde-movil_v1.0.md) | Permisos por punto se aplican aquí. |
| [CU-08](../02_especificacion_funcional/casos-de-uso/CU-08-sincronizar-relevamiento_v1.0.md) | Núcleo de la propuesta de valor. Tests intensivos. |
| [CU-11](../02_especificacion_funcional/casos-de-uso/CU-11-resolver-candidato-fusion_v1.0.md) | Manejo correcto evita pérdida de información. |
| [CU-03](../02_especificacion_funcional/casos-de-uso/CU-03-crear-versionar-plantilla_v1.0.md) | Restricciones de herencia y versionado deben ser inviolables. |

Cada uno tiene al menos un TC formal en `casos-de-prueba/`.

---

## 5. Estrategias específicas

### 5.1. Reglas de negocio
Cada RN ([RN-01](../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md) a [RN-12](../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)) tiene una **batería de tests parametrizados** que ejerce sus aplicaciones correctas y las violaciones esperadas.

### 5.2. Idempotencia y reintentos
Tests que reenvían el mismo evento N veces y validan que el estado no diverge. Tests de outbox con red simulada (failure injection).

### 5.3. Adaptadores de storage
Cada adaptador tiene un set de tests "golden" que ejecutan: subir → leer → borrar → confirmar inexistencia. Se ejecuta contra todos los adaptadores en CI (testcontainers).

### 5.4. Permisos por rol
**Matriz de pruebas obligatoria** que recorre `(rol, acción, recurso)` para garantizar que las RNs de autorización ([RN-01](../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md), [RN-02](../02_especificacion_funcional/reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md), [RN-11](../02_especificacion_funcional/reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md)) se cumplan.

### 5.5. Plantillas dinámicas
Tests con al menos **dos plantillas distintas** (raíz + una hija) en cada CU que las toca, según DoD. Validar que el frontend rinde correctamente sin código específico.

### 5.6. GPS y permisos en móvil
Mockeable a nivel de servicios MAUI para tests automatizados; validación final en dispositivos reales en Sprint Review (manual exploratoria).

---

## 6. Pipeline CI (referencia a SA-09)

La pipeline CI (BT-02) ejecuta:
1. Build.
2. Linting (.NET analyzers + dotnet format).
3. Unit + Architecture tests.
4. Integration tests con testcontainers.
5. Contract tests OpenAPI.
6. E2E suite (smoke en cada PR; full suite nightly).
7. Reporte de cobertura.

Falla de cualquier paso bloquea el merge.

---

## 7. Datos de prueba

- **Seeds:** la migración inicial seed crea admin raíz, una área default, plantilla raíz publicada, un usuario relevador y un jefe activos. Estos datos se usan en E2E y en el levantamiento local.
- **Test factories:** clases `TestDataBuilders` para generar User, Survey, Point, Photo con valores reproducibles.
- **Test fixtures:** xUnit `IClassFixture` para preparar DBs limpias por suite.

---

## 8. Reportes y métricas

- Cada Sprint Review reporta: % cobertura actual, número de tests, tests añadidos en el sprint, tests fallidos en CI durante el sprint.
- El equipo establece umbrales y responde si baja la cobertura.

---

## 9. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| Casos de uso CU-* | Criterios de aceptación a cubrir |
| [contratos-interfaces](../05_arquitectura_tecnica/contratos-interfaces_v1.0.md) | Endpoints y schemas a validar |
| [product-backlog](../06_backlog-tecnico/product-backlog_v1.0.md) | US cuyas pruebas se planifican por sprint |

---

**Fin del documento — estrategia-testing_v1.0.md**
