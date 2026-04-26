**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** modelo-datos-logico_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-05 via orquestador

---

# Modelo de Datos Lógico (SQL Server)

Materialización del [modelo conceptual](../02_especificacion_funcional/modelo-datos-conceptual_v1.0.md) en SQL Server. Tipos, índices clave y restricciones. Las definiciones DDL exactas son responsabilidad de las migraciones EF Core; este documento es el contrato.

---

## 1. Convenciones

- **PKs:** `UNIQUEIDENTIFIER` (GUID v4) generados en cliente cuando aplica ([RN-06](../02_especificacion_funcional/reglas-de-negocio/RN-06-guids-cliente-idempotencia_v1.0.md)).
- **Timestamps:** `DATETIME2(3)` UTC. Cada entidad relevante tiene `created_at`, `updated_at`. Los eventos llevan además `timestamp_original` y `applied_at`.
- **Estados:** columnas enumeradas como `NVARCHAR(32)` con `CHECK CONSTRAINT` (más simple que tablas de catálogo, suficiente para MVP).
- **Soft delete:** flag `is_deleted BIT` + `deleted_at`. No se hace DELETE físico salvo configuración explícita.
- **Coordenadas:** `GEOGRAPHY` con SRID 4326 + columnas `latitude DECIMAL(9,6)`, `longitude DECIMAL(9,6)` redundantes para queries planas.

---

## 2. Tablas

### 2.1. `Areas`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| name | NVARCHAR(150) | UNIQUE NOT NULL |
| description | NVARCHAR(500) | NULL |
| created_at | DATETIME2 NOT NULL | — |

### 2.2. `Users`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| email | NVARCHAR(254) | UNIQUE NOT NULL |
| password_hash | NVARCHAR(256) | NOT NULL |
| full_name | NVARCHAR(200) | NOT NULL |
| role | NVARCHAR(32) | CHECK IN ('admin_raiz','jefe_area','relevador') |
| status | NVARCHAR(32) | CHECK IN ('pendiente_aceptacion','activo','inhabilitado','dado_de_baja') |
| area_id | UNIQUEIDENTIFIER FK Areas | NULL para `admin_raiz` |
| created_at | DATETIME2 | NOT NULL |
| accepted_at | DATETIME2 | NULL |

Índices: `IX_Users_email` (unique), `IX_Users_area_role_status`.

### 2.3. `Templates`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| name | NVARCHAR(150) | NOT NULL |
| parent_template_id | UNIQUEIDENTIFIER FK Templates | NULL en raíz |
| is_root | BIT | DEFAULT 0; UNIQUE filtrado donde `is_root=1` |
| is_deletable | BIT | DEFAULT 1; FALSE en raíz ([RN-03](../02_especificacion_funcional/reglas-de-negocio/RN-03-plantilla-raiz-inmutable_v1.0.md)) |
| created_at | DATETIME2 | NOT NULL |

### 2.4. `TemplateVersions`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| template_id | UNIQUEIDENTIFIER FK Templates | NOT NULL |
| version_number | INT | NOT NULL; UNIQUE por template |
| status | NVARCHAR(16) | CHECK IN ('borrador','publicada') |
| field_definitions_json | NVARCHAR(MAX) | NOT NULL — schema-validado por la app |
| capture_params_json | NVARCHAR(MAX) | NOT NULL — gps_timeout, accuracy, radio, compresión, fusion threshold |
| published_at | DATETIME2 | NULL |
| created_at | DATETIME2 | NOT NULL |

> Una vez `published`, no se permite UPDATE: el ORM aplica un guard a nivel código y la DB un trigger que rechaza ([RN-05](../02_especificacion_funcional/reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md)).

### 2.5. `Surveys`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| name | NVARCHAR(200) | NOT NULL |
| description | NVARCHAR(1000) | NULL |
| area_id | UNIQUEIDENTIFIER FK Areas | NOT NULL |
| owner_id | UNIQUEIDENTIFIER FK Users | NOT NULL |
| template_version_id | UNIQUEIDENTIFIER FK TemplateVersions | NOT NULL |
| status | NVARCHAR(16) | CHECK IN ('abierto','cerrado','eliminado_logico') |
| tags | NVARCHAR(500) | NULL — CSV simple para MVP |
| closed_at | DATETIME2 | NULL |
| is_deleted | BIT | DEFAULT 0 |
| deleted_at | DATETIME2 | NULL |
| created_at, updated_at | DATETIME2 | NOT NULL |

Índices: `IX_Surveys_area_status`, `IX_Surveys_owner`.

### 2.6. `SurveyCollaborators`

| Columna | Tipo | Restricciones |
|---|---|---|
| survey_id | UNIQUEIDENTIFIER FK Surveys | PK compuesta |
| user_id | UNIQUEIDENTIFIER FK Users | PK compuesta |
| assigned_at | DATETIME2 | NOT NULL |
| assigned_by | UNIQUEIDENTIFIER FK Users | NOT NULL |

### 2.7. `Points`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| survey_id | UNIQUEIDENTIFIER FK Surveys | NOT NULL |
| coords | GEOGRAPHY | NOT NULL — SRID 4326 |
| latitude | DECIMAL(9,6) | NOT NULL |
| longitude | DECIMAL(9,6) | NOT NULL |
| accuracy_m | DECIMAL(7,2) | NULL |
| title | NVARCHAR(200) | NULL |
| description | NVARCHAR(MAX) | NULL |
| created_by | UNIQUEIDENTIFIER FK Users | NOT NULL |
| origin | NVARCHAR(32) | CHECK IN ('mobile_capture','mobile_edit','web_edit','web_manual_upload') |
| capture_mode | NVARCHAR(16) | CHECK IN ('detenido','movil','web') |
| device_id | NVARCHAR(64) | NULL |
| created_at | DATETIME2 | NOT NULL — timestamp_original del evento `created` |
| updated_at | DATETIME2 | NOT NULL |
| is_deleted | BIT | DEFAULT 0 |
| deleted_at | DATETIME2 | NULL |

Índices: `IX_Points_survey`, `SPATIAL_IX_Points_coords` (índice espacial sobre `coords`), `IX_Points_survey_creator` (para detección de candidatos).

### 2.8. `PointFieldValues` (EAV — DD-07)

| Columna | Tipo | Restricciones |
|---|---|---|
| point_id | UNIQUEIDENTIFIER FK Points | PK compuesta con `field_key` |
| field_key | NVARCHAR(100) | PK compuesta |
| value_text | NVARCHAR(MAX) | NULL |
| value_number | DECIMAL(18,6) | NULL |
| value_date | DATETIME2 | NULL |
| value_bool | BIT | NULL |
| updated_at | DATETIME2 | NOT NULL |

> Solo una de las 4 columnas `value_*` se rellena por fila, según el tipo declarado en la versión de plantilla del relevamiento. La validación se hace en código.

### 2.9. `Photos`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| point_id | UNIQUEIDENTIFIER FK Points | NOT NULL |
| comment | NVARCHAR(MAX) | NULL |
| adapter_ref | NVARCHAR(500) | NOT NULL — opaque para el adapter |
| adapter_name | NVARCHAR(16) | CHECK IN ('local','s3','ftp','sftp') ([RN-12](../02_especificacion_funcional/reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md)) |
| size_bytes | BIGINT | NOT NULL |
| content_hash | CHAR(64) | NOT NULL — SHA-256 |
| metadata_json | NVARCHAR(MAX) | NOT NULL — EXIF resumido, dimensiones, thumb |
| created_by | UNIQUEIDENTIFIER FK Users | NOT NULL |
| origin | NVARCHAR(32) | mismo CHECK que Points.origin |
| created_at | DATETIME2 | NOT NULL |
| is_deleted | BIT | DEFAULT 0 |
| deleted_at | DATETIME2 | NULL |

Índices: `IX_Photos_point`, `IX_Photos_content_hash` (para detectar duplicados exactos).

### 2.10. `AuditEvents` (append-only)

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| entity_type | NVARCHAR(16) | CHECK IN ('survey','point','photo') |
| entity_id | UNIQUEIDENTIFIER | NOT NULL |
| event_type | NVARCHAR(32) | CHECK IN ('created','field_updated','deleted','restored','merged') |
| field_key | NVARCHAR(100) | NULL si no aplica |
| old_value_json | NVARCHAR(MAX) | NULL |
| new_value_json | NVARCHAR(MAX) | NULL |
| author_id | UNIQUEIDENTIFIER FK Users | NOT NULL |
| origin | NVARCHAR(32) | CHECK como Points.origin |
| device_id | NVARCHAR(64) | NULL |
| timestamp_original | DATETIME2 | NOT NULL — del cliente |
| applied_at | DATETIME2 | NOT NULL — del backend al aplicarlo |

> Tabla **append-only** ([RN-10](../02_especificacion_funcional/reglas-de-negocio/RN-10-eventos-append-only_v1.0.md)). Trigger en DB rechaza UPDATE/DELETE; el rol de aplicación tiene permiso solo INSERT/SELECT.

Índices: `IX_AuditEvents_entity` (entity_type, entity_id, applied_at), `IX_AuditEvents_author`, `IX_AuditEvents_timestamp_original`.

### 2.11. `MergeCandidates`

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| point_a_id, point_b_id | UNIQUEIDENTIFIER FK Points | UNIQUE pair (a < b) |
| distance_m | DECIMAL(10,2) | NOT NULL |
| time_diff_s | INT | NOT NULL |
| status | NVARCHAR(24) | CHECK IN ('pendiente','fusionado','mantenido_separado') |
| resolved_by | UNIQUEIDENTIFIER FK Users | NULL |
| resolved_at | DATETIME2 | NULL |
| merge_event_id | UNIQUEIDENTIFIER FK AuditEvents | NULL — el evento `merged` |
| created_at | DATETIME2 | NOT NULL |

### 2.12. `BackendOutbox`

Cola de eventos para los workers del backend.

| Columna | Tipo | Restricciones |
|---|---|---|
| id | UNIQUEIDENTIFIER PK | — |
| event_type | NVARCHAR(64) | NOT NULL |
| payload_json | NVARCHAR(MAX) | NOT NULL |
| status | NVARCHAR(16) | CHECK IN ('pendiente','en_proceso','procesado','error','terminal_error') |
| attempts | INT | DEFAULT 0 |
| next_retry_at | DATETIME2 | NULL |
| last_error | NVARCHAR(MAX) | NULL |
| correlation_id | UNIQUEIDENTIFIER | NULL |
| created_at | DATETIME2 | NOT NULL |
| processed_at | DATETIME2 | NULL |

Índices: `IX_Outbox_status_next_retry` para drenar eficientemente.

### 2.13. `SystemConfig`

| Columna | Tipo | Restricciones |
|---|---|---|
| key | NVARCHAR(64) PK | — |
| value_json | NVARCHAR(MAX) | NOT NULL |
| updated_by | UNIQUEIDENTIFIER FK Users | NOT NULL |
| updated_at | DATETIME2 | NOT NULL |

Filas iniciales: `storage.active_adapter`, `storage.credentials_encrypted`. Las credenciales se encriptan con DPAPI o equivalente (proveedor de cifrado pluggable).

### 2.14. Tablas locales del móvil (SQLite)

Mismo modelo lógico de Surveys/Points/Photos/AuditEvents, simplificado a las columnas que el cliente necesita, **más** la tabla `PendingOperations` (outbox del cliente):

| Columna | Tipo |
|---|---|
| id | TEXT PK |
| serialized_event | TEXT |
| status | TEXT (`pendiente`, `en_envio`, `enviado`, `error`, `terminal_error`) |
| attempts | INT |
| last_error | TEXT NULL |
| next_retry_at | TEXT |
| created_at | TEXT |

---

## 3. Trazabilidad conceptual ↔ lógico

| Entidad conceptual | Tabla(s) |
|---|---|
| Usuario | `Users` |
| Área | `Areas` |
| Plantilla | `Templates` |
| VersiónDePlantilla | `TemplateVersions` |
| DefiniciónDeCampo | `TemplateVersions.field_definitions_json` |
| Relevamiento | `Surveys` + `SurveyCollaborators` |
| Punto | `Points` |
| Foto | `Photos` |
| ValorDeCampo | `PointFieldValues` |
| EventoDeAuditoría | `AuditEvents` |
| CandidatoAFusión | `MergeCandidates` |
| ConfiguraciónSistema | `SystemConfig` |
| OperaciónPendiente (cliente) | `PendingOperations` (SQLite) |

---

## 4. Notas de migración

- Toda alteración de schema debe ser aditiva siempre que sea posible.
- Las nuevas columnas no nullables incluyen DEFAULT para compatibilidad con datos existentes.
- Las tablas append-only (`AuditEvents`, `BackendOutbox`) no se migran en estructura sin estudio explícito.

---

**Fin del documento — modelo-datos-logico_v1.0.md**
