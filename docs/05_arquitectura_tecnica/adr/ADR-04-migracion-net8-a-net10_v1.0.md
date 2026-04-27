**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** ADR-04-migracion-net8-a-net10_v1.0.md
**Versión:** 1.0
**Estado:** Aceptado
**Fecha:** 2026-04-26
**Autor:** Generado por SA-05 via orquestador

---

# ADR-04 — Migración de .NET 8 a .NET 10

**Estado:** Aceptado.

## Contexto

El equipo arrancó la implementación sobre **.NET 8 LTS** (alineado con la versión LTS estable al momento del intake). Durante el setup del frente móvil (BT-09 — MAUI Blazor Hybrid) apareció un bug bloqueante en el flujo de instalación de workloads del SDK 8:

- Al ejecutar `dotnet workload install maui-android` sobre el feature band `8.0.100`, el instalador falla con `NullReferenceException` (HRESULT `0x80004003`).
- El stack apunta a `Microsoft.DotNet.Workloads.Workload.Install.NetSdkMsiInstallerClient.GetCachedMsiPayload`, que retorna `null` por inconsistencias del **MSI cache** del sistema.
- El bug es **reproducible** en la estación de desarrollo y bloquea el deploy a un dispositivo Android físico ya disponible para la validación de US-01/US-02 en móvil (Motorola moto g42, serial USB `ZY32GSJ88S`).

En paralelo, el SDK **.NET 10** ya está instalado en el entorno con los workloads MAUI **completos y funcionales**:

- `maui-android` versiones `10.0.20` y `10.0.100` disponibles sin necesidad de admin ni de tocar el MSI cache.
- Resto de workloads transversales operativos.

Esto convierte la migración a .NET 10 en un **camino más corto a tener Walking Skeleton verificable en dispositivo real** que cualquier intento de remediación del cache MSI sobre .NET 8.

Adicionalmente, el equipo reconoce un trade-off explícito en términos de soporte: **.NET 10 es STS (Standard Term Support, 18 meses)**, no LTS. La próxima versión LTS será **.NET 12** (noviembre 2026). Esta decisión asume entonces una **deuda técnica nominada `DT-net10-sts`**, con plan de re-evaluación al próximo LTS.

## Decisión

Migrar el código y la documentación arquitectónica de `.NET 8` a `.NET 10`. Concretamente:

1. **`global.json`** apuntando a SDK `10.0.203` (último feature band disponible en el entorno).
2. **`<TargetFramework>` de los 10 proyectos** de la solución pasa a `net10.0`. Los proyectos móviles agregan los TFM específicos:
   - `net10.0-android` para la app MAUI.
   - `net10.0-windows10.0.19041.0` para el target Windows del MAUI Hybrid (cuando aplique para debug local).
3. **PackageReferences a versiones 10.x** o equivalentes más recientes compatibles: EF Core 10, ASP.NET Core 10, MAUI 10, etc.
4. **Decisiones de diseño y arquitectura intactas:** DD-01 a DD-24, ADR-01 (monolito modular), ADR-02 (storage hexagonal multi-adaptador) y ADR-03 (sync outbox + LWW) **no cambian**. La migración es estrictamente de versión de runtime + paquetes; no toca topología, módulos, contratos ni reglas de negocio.

## Consecuencias positivas

- **Workloads MAUI funcionan inmediatamente** sin requerir privilegios de admin, reinstalar VS Installer ni reparar el MSI cache. Desbloquea BT-09 y todo el frente móvil.
- **EF Core 10** trae mejoras de performance, *compiled models* maduros y refinamientos en el provider de SQL Server que aprovechamos sin esfuerzo extra.
- **Alineación con la versión latest del entorno de desarrollo** del cliente: menos fricción al pasar el repositorio entre estaciones.
- **Habilita el deploy a dispositivo Android real** (Motorola moto g42 ZY32GSJ88S) para la validación temprana de US-01 y US-02 en móvil, en lugar de depender exclusivamente de emuladores.

## Consecuencias negativas

- **Soporte STS en lugar de LTS.** .NET 10 tiene 18 meses de soporte vs. los 3 años que ofrecía .NET 8 LTS. Se asume la deuda técnica **DT-net10-sts**: re-evaluar la migración al próximo LTS (.NET 12, noviembre 2026) y planificar el upgrade dentro de la ventana de soporte.
- **Actualización de 24 PackageReferences.** Algunos paquetes tienen breaking changes menores que requieren validación manual:
  - `Microsoft.IdentityModel.Tokens` 7.x → 8.x (cambios de API en validadores y handlers).
  - `MudBlazor` 7.x → 8.x (renames y ajustes de componentes — ver R-Migration.1).
- **FluentAssertions queda anclado en 7.x.** La versión 8.x cambió a licencia comercial; mantenemos la última 7.x con licencia MIT. Asumido como decisión consciente, no como deuda.
- **Tiempo del equipo dedicado a la migración** que no produce funcionalidad nueva. Mitigado por el hecho de que se ejecuta antes del primer slice funcional, en paralelo al setup del Sprint 0.

## Alternativas consideradas

1. **Mantener .NET 8 LTS y resolver el bug del workload via VS Installer.**
   Más conservador en términos de soporte, pero descartado por:
   - **Costo operativo:** requiere desinstalar/reinstalar componentes de Visual Studio, limpieza manual del MSI cache, eventual reinicio del SO.
   - **Riesgo de reproducción:** el bug del MSI cache puede repetirse en otras estaciones del equipo cada vez que se sume un dev nuevo, generando fricción recurrente.
   - **Sin garantía de éxito:** el reporte upstream del bug en el repositorio del SDK no tiene workaround oficial confirmado.

2. **Migrar a .NET 9.**
   Descartada porque .NET 9 también es STS (no aporta ventaja de soporte vs. .NET 10) y le quedan menos meses de cobertura. Sin diferenciador material frente a .NET 10.

3. **Reemplazar Swashbuckle por `Microsoft.AspNetCore.OpenApi` nativo de .NET 10.**
   Descartada del scope de esta migración para separar concerns. Mantener Swashbuckle 7.x estable. Queda registrada como deuda futura **DT-openapi-native** para evaluar en una iteración posterior.

## Riesgos identificados

- **R-Migration.1** — *MudBlazor 7→8 con breaking changes:* algunos componentes pueden requerir ajustes (renames, cambios de props). **Mitigación:** rollback controlado a `MudBlazor 7.20.x` (última 7.x compatible con net10) si los breaking changes resultan demasiado invasivos para los slices iniciales.
- **R-Migration.2** — *Migraciones EF Core 8→10:* el snapshot de migraciones puede regenerarse, pero el SQL emitido **debe ser idéntico** para no introducir cambios de schema implícitos. **Mitigación:** validar con `dotnet ef migrations script` antes y después; comparar diff.
- **R-Migration.3** — *Tests con FluentAssertions 6.12 → 7.x:* posibles renames menores en aserciones. **Mitigación:** ejecutar la suite completa (41 tests) y aplicar los renames mecánicamente; ningún cambio semántico esperado.

## Plan de validación post-migración

1. **Build limpio** de la solución completa sin warnings nuevos no justificados.
2. **41 tests verdes** (unit + integration + architecture tests).
3. **Smoke con curl** sobre los endpoints clave del backend (auth, surveys, sync push/pull).
4. **Build Android exitoso** del MAUI Hybrid.
5. **Deploy real al dispositivo Motorola moto g42 ZY32GSJ88S** y verificación de arranque + login.

## Trazabilidad

### Paquetes upgradeados (referencia)

Tabla resumida con los paquetes más relevantes; el detalle completo vive en los `.csproj` y queda registrado en el `INFORME-COMPLETITUD` post-migración.

| Paquete | Versión actual (8.x) | Versión target (10.x) |
|---|---|---|
| `Microsoft.AspNetCore.App` | 8.0.x | 10.0.x |
| `Microsoft.EntityFrameworkCore` | 8.0.x | 10.0.x |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.x | 10.0.x |
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.x | 10.0.x |
| `Microsoft.Extensions.Hosting` | 8.0.x | 10.0.x |
| `Microsoft.Maui.Controls` | 8.0.x | 10.0.x |
| `Microsoft.AspNetCore.Components.WebView.Maui` | 8.0.x | 10.0.x |
| `Microsoft.IdentityModel.Tokens` | 7.x | 8.x |
| `MudBlazor` | 7.x | 8.x (con fallback a 7.20.x si R-Migration.1 se materializa) |
| `Swashbuckle.AspNetCore` | 7.x | 7.x (sin cambio — ver alternativa 3) |
| `FluentAssertions` | 6.12.x | 7.x (último MIT — ver consecuencias negativas) |
| `xUnit` | 2.x | 2.x (sin cambio) |
| `Moq` | 4.x | 4.x (sin cambio) |
| `Testcontainers` | 3.x | 4.x |

### Referencias documentales

- Esta decisión se referencia desde [arquitectura-solucion](../arquitectura-solucion_v1.0.md) Sec. 6 ("Decisiones de arquitectura clave").
- Tras la migración del código, se actualiza [INFORME-COMPLETITUD](../../INFORME-COMPLETITUD_v1.0.md) con el resultado del plan de validación.
- ADRs no afectados: [ADR-01](ADR-01-monolito-modular-vs-microservicios_v1.0.md), [ADR-02](ADR-02-storage-hexagonal-multi-adaptador_v1.0.md), [ADR-03](ADR-03-sincronizacion-outbox-y-lww-por-campo_v1.0.md). Las decisiones DD-01 a DD-24 del intake se mantienen intactas.
- Deuda técnica registrada: **DT-net10-sts** (re-evaluar al próximo LTS, .NET 12, noviembre 2026) y **DT-openapi-native** (migrar de Swashbuckle a `Microsoft.AspNetCore.OpenApi` nativo en una iteración posterior).

---

**Fin del documento — ADR-04-migracion-net8-a-net10_v1.0.md**
