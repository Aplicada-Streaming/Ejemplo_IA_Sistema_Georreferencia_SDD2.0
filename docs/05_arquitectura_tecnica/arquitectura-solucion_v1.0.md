**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** arquitectura-solucion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-05 via orquestador

---

# Arquitectura de Solución

Documento de arquitectura técnica del sistema. Materializa todas las decisiones tomadas en `PROJECT-BRIEF` Sec. 4 (DD-01 a DD-24) y agrega los detalles necesarios para que el equipo pueda implementar.

---

## 1. Visión técnica

**Tipo:** Monolito modular en backend, con workers separados, expuesto vía API REST a dos clientes independientes (web Blazor Server y móvil MAUI Blazor Hybrid). Storage de fotos abstracto detrás de puertos hexagonales. SQL Server como almacén de dominio.

**Decisión de fondo (DD-01):** la portabilidad y los frentes desacoplados se obtienen con un contrato REST estable, no con microservicios. El monolito modular preserva costo operativo bajo y permite extraer un módulo cuando alguno justifique escalabilidad heterogénea.

---

## 2. Componentes del sistema

| Componente | Responsabilidad | Tecnología | Comunicación |
|---|---|---|---|
| **API REST (monolito)** | Expone el contrato público; orquesta los módulos internos; emite eventos al outbox del backend para los workers. | .NET 8 + ASP.NET Core | HTTPS · OpenAPI v1 |
| **Worker de imágenes** | Normalización (resize, compresión), thumbnails, manejo de EXIF, indexación de metadata. Drena cola de trabajos `image_processing`. | .NET Worker Service | Lectura de cola en DB (table-driven) o Channels in-process según despliegue |
| **Worker de sincronización** | Aplica eventos del outbox del backend, calcula candidatos a fusión, propaga notificaciones post-sync. | .NET Worker Service | Lectura de cola en DB |
| **Frontend web** | Login, listado, edición, paneles de conflicto, wizard, plantillas. | Blazor Server + MudBlazor | Cliente HTTP del API REST |
| **Frontend móvil** | Captura offline-first, mapa colaborativo, sync. | MAUI Blazor Hybrid + MudBlazor + SQLite local | Cliente HTTP del API REST + DB local + outbox |
| **DB principal** | Persistencia del modelo de dominio + log de eventos + outbox del backend. | SQL Server 2019+ | TCP local en dev, gestionado en producción |
| **DB local del móvil** | Persistencia local del relevamiento, puntos, fotos pendientes, outbox del cliente. | SQLite via EF Core | Acceso local |
| **Storage adapter** | Persistencia de fotos. Puerto + 4 adaptadores intercambiables. | Hexagonal: Local FS / S3 / FTP / SFTP | Llamadas al puerto desde módulo `Photos` y `Storage` |
| **Mapa** | Tiles del mapa en web y móvil. | OpenStreetMap (provider externo) | HTTPS GET |

---

## 3. Módulos internos del backend

Aplican arquitectura hexagonal: cada módulo expone **puertos** (interfaces de dominio) y consume **adaptadores** concretos (DB, storage, JWT). La regla dura es que ningún módulo accede a tablas de otros módulos: la comunicación cruza por interfaces de dominio o eventos internos.

```
Backend Monolítico
├── Identity        — usuarios, roles, áreas, registro/aceptación, JWT (ROPC)
├── Templates       — plantillas, herencia, versionado, resolución dinámica
├── Surveys         — relevamientos, asignaciones, ciclo de vida, etiquetas
├── Points          — puntos georreferenciados, fusión, EAV de valores
├── Photos          — catálogos, comentarios, metadata, asociación con storage
├── Sync            — outbox del backend, eventos, LWW, candidatos a fusión
├── Storage         — puerto + adaptadores local/S3/FTP/SFTP
└── SystemConfig    — config inicial (storage), gestión por admin raíz
```

### 3.1. Reglas de dependencia entre módulos

| Módulo | Puede depender de | NO debe depender de |
|---|---|---|
| Identity | (núcleo de dominio) | Cualquier otro módulo |
| Templates | Identity (autores) | Surveys, Points, Photos |
| Surveys | Identity, Templates | Points, Photos directamente (usa eventos) |
| Points | Identity, Templates, Surveys (vía interfaces) | Photos, Sync directamente |
| Photos | Storage, Points (vía interfaces) | Sync directamente |
| Sync | Todos los anteriores (lee eventos) | Es invocado, no invoca lógica de negocio (excepto crear `CandidatoAFusión`) |
| Storage | (núcleo) | Cualquier otro |
| SystemConfig | Identity, Storage | Resto |

---

## 4. Pipeline de procesamiento principal

```
[Móvil offline]
   │
   │ Captura local: Punto + Foto + ValorDeCampo + Evento (en SQLite)
   │ + entry en OperaciónPendiente (outbox local)
   │
   │── (cuando hay conexión)
   ▼
[POST /api/v1/sync/push]
   │
   ▼
[Sync module valida idempotencia por (GUID, timestamp original)]
   │
   ├── duplicado → 200 con marca "ya aplicado" (idempotente)
   ├── conflicto LWW por campo → resuelve, registra notificación
   ├── post-cierre → marca rechazado, agrega al panel
   └── nuevo → aplica al modelo central, emite evento al outbox del backend
   │
   ▼
[Outbox del backend]
   │
   ├── Worker imágenes → procesa fotos pendientes (normalización, thumb)
   └── Worker sync → recalcula candidatos a fusión, dispara notificaciones
   │
   ▼
[GET /api/v1/surveys/{id}/changes?since={ts}]
   │
   ▼
[Móvil aplica eventos remotos a SQLite local]
```

---

## 5. Diagrama lógico de despliegue (local-dev y productivo)

```
┌─────────────────────────────────────────────────┐
│  Estación del relevador (Android/iOS)           │
│  - App MAUI Blazor Hybrid                       │
│  - SQLite local + outbox                        │
└──────────────────────┬──────────────────────────┘
                       │ HTTPS
                       ▼
┌─────────────────────────────────────────────────┐
│  Servidor de aplicación (proceso .NET)          │
│  - Frontend web (Blazor Server)                 │
└──────────────────────┬──────────────────────────┘
                       │ HTTPS
                       ▼
┌─────────────────────────────────────────────────┐
│  API Backend (proceso .NET)                     │
│  - Monolito modular                             │
└─────┬───────────────────────────────────┬───────┘
      │                                   │
      ▼                                   ▼
┌──────────────┐                ┌────────────────────┐
│ SQL Server   │                │ Workers (procesos) │
│ - Dominio    │ ◄──────────────│ - Imágenes         │
│ - Outbox     │                │ - Sync             │
│ - Auditoría  │                └────────────────────┘
└──────────────┘
      │
      ▼
┌─────────────────────────────────────────────────┐
│  Storage de fotos (adaptador activo)            │
│  Local FS / S3 / FTP / SFTP                     │
└─────────────────────────────────────────────────┘
```

En **local-dev**, todos los procesos corren en `localhost` con puertos distintos según `PROJECT-BRIEF` Sec. 9.1, lanzados por scripts `.bat`.

En **producción**, cada proceso es un servicio independiente. La estructura concreta de despliegue depende de `[REQUIERE_INFO]` ambientes superiores (`PROJECT-BRIEF` Sec. 9.2).

---

## 6. Decisiones de arquitectura clave

Las decisiones ya tomadas en `PROJECT-BRIEF` Sec. 4 (DD-01 a DD-24) son la base. Los ADRs en `adr/` formalizan tres decisiones donde la arquitectura agrega detalle propio:

| ADR | Decisión |
|---|---|
| [ADR-01](adr/ADR-01-monolito-modular-vs-microservicios_v1.0.md) | Monolito modular + workers vs. microservicios |
| [ADR-02](adr/ADR-02-storage-hexagonal-multi-adaptador_v1.0.md) | Storage de fotos con arquitectura hexagonal multi-adaptador |
| [ADR-03](adr/ADR-03-sincronizacion-outbox-y-lww-por-campo_v1.0.md) | Sincronización con outbox + LWW por campo + detección de candidatos a fusión |

---

## 7. Aspectos transversales

### 7.1. Autenticación y autorización
- ROPC + JWT bearer, deuda DT-01.
- JWT incluye claims `user_id`, `role`, `area_id`, `device_id`, `version`.
- Política de autorización por endpoint con atributos `[Authorize(Policy = "...")]` que mapean a:
  - `RequireAdminRoot`, `RequireJefeArea(area)`, `RequireSurveyOwnerOrJefe`, `RequirePointEditPermission` (ver [RN-01](../02_especificacion_funcional/reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md)).
- Tokens de corta duración (15-30 min) + refresh token con rotación.

### 7.2. Logging y trazabilidad operativa (no auditoría)
- Logging estructurado con Serilog → consola en local; sink remoto pendiente de definición de ambientes (`[REQUIERE_INFO]`).
- Correlación de requests: `X-Correlation-ID` HTTP header propagado a workers.
- Métricas: contadores básicos de operaciones y errores en `/metrics` (Prometheus textfile o equivalente).

### 7.3. Validación
- Validaciones de dominio en cada módulo, expresadas en interfaces explícitas (no en filtros HTTP).
- FluentValidation para DTOs de entrada del API REST.

### 7.4. Manejo de errores
- Errores de dominio → tipos discriminados; el API REST mapea a códigos HTTP (`409` conflicto, `422` validación, `403` autorización, `404` no encontrado).
- Errores de infraestructura → reintentos por la capa cliente (móvil, frontend) según política de outbox.
- Logging de errores con Serilog enriquecido por correlation id.

### 7.5. Migraciones
- EF Core Migrations en una solución dedicada del backend.
- Estrategia: migraciones aditivas y compatibles hacia atrás cuando sea posible, para permitir despliegues escalonados.

### 7.6. Resolución dinámica de plantillas
- Endpoint `GET /api/v1/template-versions/{id}/resolved` devuelve la versión con herencia ya aplicada (campos heredados + propios + parámetros). Frontend renderiza dinámicamente sin conocer la jerarquía.

### 7.7. Detección de candidatos a fusión
- Implementada en el Worker de Sync. Consulta espacial usa `GEOGRAPHY` de SQL Server con índice spatial sobre `Points.coords`.
- Threshold por relevamiento: tomado de la versión de plantilla del relevamiento. Default 10m / 24h.
- Se ejecuta cada vez que llega un evento `point_created` o `point_field_updated` del campo `coordenadas`.

### 7.8. Outbox del backend
- Tabla `BackendOutbox(id, event_type, payload_json, status, attempts, next_retry_at, created_at)`.
- Workers leen con `SELECT TOP N ... ORDER BY created_at FOR UPDATE SKIP LOCKED` para concurrencia segura.

---

## 8. Modelo de datos lógico

Documento separado: [modelo-datos-logico_v1.0.md](modelo-datos-logico_v1.0.md).

## 9. Contratos de API y eventos

Documento separado: [contratos-interfaces_v1.0.md](contratos-interfaces_v1.0.md).

---

## 10. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| `devs/intake/PROJECT-BRIEF.md` Sec. 1, 4, 5 | DDs como base de la arquitectura |
| [especificacion-funcional](../02_especificacion_funcional/especificacion-funcional_v1.0.md) | CUs y RNs que la arquitectura debe soportar |
| [modelo-datos-conceptual](../02_especificacion_funcional/modelo-datos-conceptual_v1.0.md) | Entidades a materializar en el modelo lógico |
| [flujos-de-usuario](../03_ux-ui/flujos-de-usuario_v1.0.md) | Pantallas que el API debe alimentar |

---

**Fin del documento — arquitectura-solucion_v1.0.md**
