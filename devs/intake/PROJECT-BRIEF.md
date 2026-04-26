# PROJECT-BRIEF — Delineamientos Técnicos y Metodológicos

**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** PROJECT-BRIEF.md
**Versión:** 1.0
**Fecha:** 2026-04-26
**Estado:** Borrador
**Audiencia:** Equipo técnico, arquitectos, futuros desarrolladores que se incorporen

---

## Índice

1. [Arquitectura propuesta](#1-arquitectura-propuesta)
2. [Capturas de pantalla de referencia](#2-capturas-de-pantalla-de-referencia)
3. [Esquema de aplicaciones](#3-esquema-de-aplicaciones)
4. [Decisiones de diseño tomadas](#4-decisiones-de-diseño-tomadas)
5. [Diseño de la sincronización multi-colaborador](#5-diseño-de-la-sincronización-multi-colaborador)
6. [Política de manejo de fotos](#6-política-de-manejo-de-fotos)
7. [Política de captura en móvil — permisos y GPS](#7-política-de-captura-en-móvil--permisos-y-gps)
8. [Metodología de ejecución](#8-metodología-de-ejecución)
9. [Infraestructura y deploy](#9-infraestructura-y-deploy)
10. [Restricciones técnicas adicionales](#10-restricciones-técnicas-adicionales)
11. [Referencias y assets](#11-referencias-y-assets)

---

## 1. Arquitectura propuesta

### 1.1. Tipo de arquitectura

**Monolito modular** en el backend, con **workers separados** para tareas asíncronas. **Frontend web y aplicación móvil** son procesos/aplicaciones independientes que consumen la API REST del backend.

### 1.2. Justificación de la decisión

La arquitectura inicialmente preferida por el cliente era microservicios completos. Durante la conversación de relevamiento se aclaró que la justificación de fondo era *"poder cambiar el frontend a React en el futuro"*. Esa portabilidad **no depende de la granularidad del backend**: se obtiene con una API REST limpia y bien diseñada (con OpenAPI versionado), independientemente de si el backend es uno o varios procesos.

Sin esa justificación, el alcance del proyecto (una organización, un dominio coherente, un equipo único) no presenta condiciones que justifiquen el costo operativo de microservicios:

- Deploy coordinado entre servicios.
- Observabilidad distribuida.
- Transacciones cross-service.
- Complejidad de testing local con scripts `.bat`.

Se eligió **monolito modular + workers** como mejor balance entre simplicidad operativa y flexibilidad para futuros crecimientos. Si en el futuro un módulo crece o presenta requisitos de escalabilidad heterogéneos, puede extraerse a microservicio sin reescribir el dominio.

### 1.3. Componentes del sistema

| Componente | Responsabilidad | Tecnología | Nuevo o existente |
|---|---|---|---|
| Backend API | Monolito modular con módulos separados (Identity, Templates, Surveys, Points, Photos, Sync, Storage, SystemConfig). Expone API REST. | .NET, ASP.NET Core | Nuevo |
| Worker de procesamiento de imágenes | Normalización, generación de thumbnails, manejo de EXIF | .NET Worker Service | Nuevo |
| Worker de sincronización | Aplicación de eventos del outbox, resolución de conflictos, detección de candidatos a fusión | .NET Worker Service | Nuevo |
| Frontend web | Revisión, edición, asignación, carga manual, panel de conflictos | Blazor Server + MudBlazor | Nuevo |
| Frontend móvil | Captura en campo offline-first | MAUI Blazor Hybrid + MudBlazor | Nuevo |
| Base de datos | Persistencia del modelo de dominio | SQL Server | Nuevo |
| Storage de fotos | Persistencia de archivos. Adaptadores intercambiables. | Local / S3 / FTP / SFTP | Nuevo |
| Mapa | Tiles y geolocalización | OpenStreetMap | Externo existente |

### 1.4. Módulos internos del backend

El monolito está organizado internamente en módulos con responsabilidades acotadas. Cada módulo respeta la regla de no acceder directamente a las tablas de otros módulos; la comunicación entre módulos pasa por interfaces de dominio (puertos) o eventos internos.

```
Backend Monolítico
├── Identity         ← usuarios, roles, áreas, autenticación, JWT
├── Templates        ← plantillas, herencia, versionado, resolución
├── Surveys          ← relevamientos, asignaciones, ciclo de vida
├── Points           ← puntos georreferenciados, fusión, eventos
├── Photos           ← catálogos, comentarios, metadata
├── Sync             ← outbox, eventos, resolución de conflictos
├── Storage          ← abstracción + adaptadores (local/S3/FTP/SFTP)
└── SystemConfig     ← config inicial, gestión por admin raíz
```

Cada módulo aplica **arquitectura hexagonal** (puertos y adaptadores) internamente. Es especialmente crítica en `Storage` para cumplir la abstracción del proveedor.

### 1.5. Servicios externos

| Servicio | Uso | Obligatorio |
|---|---|---|
| OpenStreetMap | Tiles del mapa en web y móvil | Sí (decisión del cliente) |
| Provider S3 | Storage de fotos (uno de los adaptadores) | No (configurable) |
| Servidor FTP/SFTP | Storage de fotos (uno de los adaptadores) | No (configurable) |

### 1.6. Persistencia de datos por punto

El modelo de datos por punto usa un esquema **EAV (entity-attribute-value)** para almacenar los valores de los campos definidos por la plantilla. Esta elección prioriza flexibilidad sobre performance de queries complejas.

```
PointFieldValue
─────────────────────────────────────
point_id        UNIQUEIDENTIFIER
field_key       NVARCHAR(100)
value_text      NVARCHAR(MAX)   NULL
value_number    DECIMAL         NULL
value_date      DATETIME        NULL
value_bool      BIT             NULL
```

Para MVP es suficiente. Si más adelante la analítica intensiva justifica vistas materializadas o tablas específicas por tipo de plantilla, se evalúan a su tiempo sin afectar al MVP.

---

## 2. Capturas de pantalla de referencia

[REQUIERE_INFO] No se compartieron capturas de pantalla durante la conversación de relevamiento. Si el cliente provee referencias visuales (mockups, sistemas previos, prototipos en Figma), se documentarán en esta sección con la convención SS-XX y se almacenarán en `/devs/assets/screenshots/`.

---

## 3. Esquema de aplicaciones

### 3.1. Tipo de sistema

Ecosistema de aplicaciones desplegadas como **procesos separados** que comparten un único backend.

### 3.2. Mapa de aplicaciones

```
[ App Móvil MAUI ]   [ Browser con app Blazor Server ]
        │                          │
        │                          ▼
        │                [ Servidor Frontend Web ]
        │                (Blazor Server, proceso .NET)
        │                          │
        └──────────────┬───────────┘
                       ▼
              [ Servidor Backend ]   ← monolito modular
              (API REST, proceso .NET)
                       │
         ┌─────────────┼─────────────┐
         ▼             ▼             ▼
    [SQL Server]  [Storage]    [Workers]
                  (local/S3/   (procesamiento
                   FTP/SFTP)    imágenes, sync)
```

La app móvil consume la API REST **directamente**, sin pasar por el frontend web. El frontend web es un cliente HTTP más, igual que el móvil. Si en el futuro se reemplaza el frontend web por React u otra tecnología, ocupa el mismo lugar en este diagrama: cliente del backend.

### 3.3. Flujos de datos principales

| Flujo | Origen | Destino | Descripción |
|---|---|---|---|
| Captura en campo | App móvil | DB local + outbox local | Punto, fotos y valores de plantilla persistidos localmente. |
| Sync push | Outbox móvil | API backend | Drena operaciones pendientes; el worker de sync las aplica al modelo central. |
| Sync pull | API backend | App móvil | Cliente pide cambios desde el último timestamp; recibe puntos/fotos/eventos de otros colaboradores. |
| Carga manual web | Frontend web | API backend | Lote de fotos con extracción de EXIF o coordenadas manuales. |
| Revisión web | Frontend web | API backend | Listado, filtros, edición de catálogos y campos. |
| Procesamiento de imagen | API backend | Worker de imágenes (cola) | Normalización, thumbnails, indexación de metadata. |
| Configuración inicial | Frontend web (admin raíz) | API backend → tabla de config | Wizard de storage en primer arranque. |
| Detección de fusión | Worker de sync | DB → panel de conflictos | Al recibir punto nuevo, comparar con vecinos del mismo relevamiento; marcar candidatos a fusión. |

---

## 4. Decisiones de diseño tomadas

| ID | Decisión | Justificación | Alternativa descartada |
|---|---|---|---|
| DD-01 | Monolito modular + workers en backend | Cumple los requisitos del cliente (API REST como contrato, frontends portables, deploy independientes) sin el overhead operativo de microservicios. | Microservicios completos. Descartada por overhead injustificado para el alcance. |
| DD-02 | Workers separados para imágenes y sync | Cargas asíncronas que no deben mezclarse con el ciclo request/response del API. | Procesar todo dentro del API. Descartada por riesgo de timeouts y bloqueo. |
| DD-03 | Frontend web y backend como procesos físicamente separados | Cumple requisito explícito del cliente y habilita reemplazo futuro del frontend por otra tecnología. | Acoplar en un solo proceso. Descartada por requisito de portabilidad. |
| DD-04 | Plantillas con herencia y versionado | Necesario para soportar tipos diversos de inspección sin proliferar código y para preservar relevamientos históricos. | Hardcodear cada tipo de relevamiento. Descartada por inflexibilidad y costo de extensión. |
| DD-05 | Plantilla genérica raíz como base obligatoria | Toda inspección comparte campos comunes (fecha, ubicación, condición general, observaciones, prioridad). Habilita análisis transversal entre tipos. | Plantillas independientes sin raíz común. Descartada por pérdida de capacidad analítica transversal. |
| DD-06 | Restricciones a la herencia de campos | Una hija puede agregar y sobrescribir atributos visuales/de validación, pero no puede cambiar tipo ni eliminar campos heredados (sí marcarlos "no aplica"). | Herencia totalmente libre. Descartada por riesgo de romper invariantes y análisis. |
| DD-07 | Persistencia de valores en EAV | Flexibilidad ante plantillas dinámicas con costo aceptable de queries en MVP. | Tabla por tipo de plantilla. Descartada para MVP por rigidez; reservada para si la analítica lo demanda. |
| DD-08 | Identificadores GUID generados en cliente | Permite trabajo offline y sincronización idempotente sin coordinación con el backend. | IDs autogenerados por DB. Descartada por incompatibilidad con offline. |
| DD-09 | Outbox local en el móvil | Sobrevive a cierres de app, reinicios y conexión intermitente. | Llamadas directas al API en tiempo real. Descartada por incompatibilidad con offline. |
| DD-10 | Sincronización bidireccional (push + pull diferencial) | Múltiples colaboradores deben converger en el mismo relevamiento y verse mutuamente. | Solo push del cliente al servidor. Descartada por insuficiente para multi-colaborador. |
| DD-11 | Resolución de conflictos LWW por campo + alertas + panel de revisión manual | Predecible y eficiente para resolución automática de la mayoría de conflictos. El panel atiende los casos donde el usuario quiere intervenir. | Solo automático sin notificación. Descartada por riesgo de pérdida silenciosa de información. |
| DD-12 | Log de eventos por entidad | Trazabilidad técnica + sync diferencial + resolución de conflictos en una sola estructura. | Auditoría como log lateral separado. Descartada por duplicar el modelo y desincronizarse. |
| DD-13 | Permisos por punto (dueño edita todo, colaborador solo lo suyo) | Expresa correctamente el requisito original del cliente para multi-colaborador. | Permisos solo por relevamiento. Descartada por insuficiente. |
| DD-14 | Storage de fotos con arquitectura hexagonal | Cumple la abstracción "transparente al backend" entre los adaptadores. | Acoplar a un proveedor concreto. Descartada por requisito explícito. |
| DD-15 | Wizard de primer arranque para storage | Requisito del admin raíz. | Configuración hardcodeada en `appsettings.json`. Descartada por requisito explícito. |
| DD-16 | API REST con OpenAPI versionado como contrato público | Habilita reemplazo futuro de frontend sin tocar backend. | API ad-hoc sin documentación formal. Descartada por riesgo de filtrar detalles de implementación. |
| DD-17 | ROPC con JWT bearer (deuda técnica DT-01) | Decisión heredada del cliente. Asumida explícitamente como deuda. | OAuth 2.1 con code+PKCE. Reservada para revisión futura. |
| DD-18 | Carga manual desde web con EXIF + fallback manual | Permite ingresar relevamientos hechos sin la app móvil. | Solo permitir captura desde móvil. Descartada por requisito explícito del cliente. |
| DD-19 | Walking skeleton previo a slices verticales | Necesidad de columna vertebral mínima (auth, storage, sync, scripts) antes de slices funcionales. | Empezar slices verticales directamente. Descartada por riesgo de reinventar infraestructura por slice. |
| DD-20 | Spike de una semana sobre sincronización antes de slices reales | La sincronización multi-colaborador es la complejidad de diseño dominante. | Resolver sync dentro del primer slice funcional. Descartada por magnitud del riesgo de no validar el protocolo. |
| DD-21 | Detección de candidatos a fusión + revisión manual (no fusión automática) | Dos colaboradores en el mismo lugar pueden ser duplicados o defectos cercanos genuinos. La decisión humana evita pérdida silenciosa de información. | Fusión automática por proximidad. Descartada por riesgo de fusionar puntos legítimamente distintos. |
| DD-22 | Admin raíz puede dar de baja o inhabilitar jefes de área | Requisito explícito del cliente. La inhabilitación es reversible; la baja es definitiva. | Solo dar de baja. Descartada porque la inhabilitación reversible es más útil operativamente. |
| DD-23 | Compresión de fotos configurable por plantilla con defaults sensatos | Equilibrio entre tamaño de upload y calidad. Tratamientos avanzados quedan para futuro escalamiento. | Tamaño y calidad fijos. Descartada por inflexibilidad ante distintos tipos de inspección. |
| DD-24 | Diálogo unificado de captura (permisos + GPS) con máquina de estados | UX consistente y predecible. Un solo punto de manejo de fallas de hardware/permisos. | Diálogos separados para cada caso (permiso, espera, error). Descartada por inconsistencia. |

---

## 5. Diseño de la sincronización multi-colaborador

La sincronización multi-colaborador es la complejidad central del sistema. Esta sección documenta los conflictos posibles y los mecanismos de resolución, automáticos y manuales.

### 5.1. Identificadores y orígenes

- Toda entidad creada en cualquier dispositivo (móvil o web) recibe un **GUID generado en cliente**. El backend acepta el GUID como ID definitivo.
- Toda operación se acompaña de su **timestamp de origen**: cuando ocurrió en el dispositivo, no cuando llegó al servidor.
- Toda operación carga su **origen** (`mobile_capture` / `mobile_edit` / `web_edit` / `web_manual_upload`) y el `device_id` cuando aplica.

### 5.2. Outbox y reintentos

- El móvil escribe simultáneamente en su DB local y en una tabla `pending_operations` (outbox).
- Un proceso background drena el outbox cuando hay conexión disponible.
- Los reintentos son exponenciales: 5s, 15s, 1m, 5m, 15m, etc.
- Las operaciones son idempotentes: un reenvío del mismo evento (mismo GUID y timestamp) no se aplica dos veces.

### 5.3. Sincronización pull diferencial

Endpoint conceptual:

```
GET /api/v1/surveys/{id}/changes?since={timestamp}
```

El cliente persiste el timestamp del último pull exitoso. Cada sync trae los eventos posteriores a ese timestamp. El cliente actualiza su DB local con los cambios recibidos antes de reanudar la captura.

### 5.4. Catálogo de conflictos posibles y resolución

| ID | Conflicto | Resolución automática | UX expuesta al usuario |
|---|---|---|---|
| C-01 | Edición concurrente del mismo campo del mismo punto | Last-write-wins por timestamp del evento original | Notificación post-sync + panel de conflictos con opción de revertir |
| C-02 | Movimiento concurrente de coordenadas del mismo marcador | LWW (mismo patrón que C-01) | Notificación + indicador "marcador movido recientemente" sobre el punto durante 24h |
| C-03 | Eliminación de un punto que recibió ediciones/fotos posteriores en otro dispositivo | Soft-delete con timestamp; gana el evento más reciente. Si la edición es posterior, el punto se "resucita". | Panel "Puntos con actividad post-eliminación" para decisión humana |
| C-04 | Creación duplicada del mismo punto | Imposible (los GUIDs en cliente lo eliminan por construcción) | Ninguna |
| C-05 | Colaborador edita su propio punto + dueño edita el mismo punto | Precedencia del dueño, no LWW: la última escritura del dueño gana incondicionalmente | Notificación al colaborador: "El dueño sobrescribió tu edición de [campo]" |
| C-06 | Cierre del relevamiento con capturas offline pendientes en otro dispositivo | Capturas con timestamp anterior al cierre se aceptan; las posteriores quedan en estado "rechazadas" | Notificación al dueño: "Hay N capturas posteriores al cierre. ¿Reabrir?" |
| C-07 | Sincronización parcial (subió N de M fotos, se cortó) | Reintentos por foto (idempotente por GUID); sync resumible | Panel de estado de sync con progreso por entidad y botón de reintentar |
| C-08 | Plantilla con versión nueva publicada durante captura offline | Cada relevamiento queda atado a su versión de plantilla | Ninguna |
| C-09 | Puntos cercanos creados por distintos colaboradores | Detección de candidato a fusión, **no fusión automática** | Panel "Candidatos a fusión" con UI de revisión y merge manual |

### 5.5. Mecanismo de detección de candidatos a fusión

Cuando un punto nuevo llega al backend durante el sync:

1. El sistema calcula la distancia geodésica al resto de los puntos del mismo relevamiento.
2. Si encuentra puntos creados por **distintos colaboradores** dentro de un threshold configurable (default: el mismo radio de captura del modo móvil, típicamente 10m) y con timestamps cercanos (default: dentro de 24h del primero), los marca como **candidatos a fusión**.
3. **No fusiona automáticamente.** El candidato queda visible en el panel de conflictos para revisión humana.

> Decisión deliberada: la fusión automática perdería información cuando los puntos cercanos son defectos genuinamente distintos (por ejemplo, dos baches a 5m de distancia). La revisión humana es barata (un click confirma o descarta) y evita pérdidas silenciosas.

### 5.6. UI de revisión de candidatos a fusión

La pantalla de revisión muestra:

- **Mapa con ambos puntos resaltados**, líneas indicando la cercanía y la distancia exacta calculada.
- **Listado lado a lado de fotos** de cada punto.
- **Comparación de campos de plantilla**: cada campo donde haya divergencia se muestra con ambos valores y un selector de cuál prevalece.
- **Acciones disponibles:**
  - **Fusionar** → el usuario elige posición resultante (centroide de ambas, A o B), valor por campo donde haya divergencia. Las fotos se unifican en un único catálogo. Se crea un evento `PointMerge`.
  - **Mantener separados** → los puntos quedan marcados como "no duplicados" y no se proponen entre sí en futuros chequeos.

### 5.7. Panel de conflictos

Disponible en la web para jefes de área y dueños de relevamientos. Lista todos los conflictos pendientes de revisión, agrupados por tipo:

- Sobrescrituras automáticas que el usuario afectado puede revertir
- Candidatos a fusión pendientes
- Puntos eliminados con actividad posterior
- Capturas rechazadas por relevamiento cerrado

Cada conflicto tiene su UI específica de resolución manual.

### 5.8. Notificaciones post-sync

Después de cada sincronización, los usuarios afectados reciben una notificación pasiva (badge en la barra superior):

```
Sincronizado · 12 cambios · 2 conflictos para revisar
```

Click sobre el badge abre el panel de conflictos.

---

## 6. Política de manejo de fotos

### 6.1. Parámetros configurables por plantilla

| Parámetro | Default | Descripción |
|---|---|---|
| `photo_max_long_side_px` | 2048 | Lado más largo en píxeles. Redimensiona manteniendo aspect ratio. |
| `photo_jpeg_quality` | 85 | Calidad JPEG (0-100). |
| `photo_target_format` | `jpg` | `jpg` \| `webp` \| `original` |
| `photo_keep_original` | `false` | Si true, también guarda el archivo original sin procesar. |
| `photo_generate_thumbnail` | `true` | Genera thumb de 256px de lado largo. |
| `photo_strip_sensitive_exif` | `false` | Si true, conserva GPS + timestamp y elimina el resto del EXIF (modelo de cámara, número de serie, etc.). |

> La plantilla genérica raíz provee estos valores como defaults iniciales. Plantillas hijas pueden sobrescribirlos según la necesidad del tipo de inspección.

### 6.2. Pipeline de procesamiento

```
[Captura en móvil]
       │
       ▼
[Procesamiento local con params de la plantilla del relevamiento]
       │
       ├── normalized.jpg  (siempre)
       ├── original.jpg    (si photo_keep_original = true)
       └── thumb.jpg       (siempre)
       │
       ▼
[Outbox local]
       │
       ▼ (al sincronizar)
[Backend] → [Storage adapter] → [Worker re-valida e indexa metadata]
```

### 6.3. Estimación de volumen con defaults

Para un relevamiento típico de 100 puntos × 5 fotos cada uno:

- **Sin originales** (default): ~250-350 MB total, vs. ~1.5-4 GB del original sin procesar.
- **Con originales**: ~2-5 GB.

### 6.4. Trabajo diferido para futuro escalamiento

Documentado para no abordar en MVP:

- Detección automática de fotos borrosas o con mala exposición.
- Compresión adaptativa según ancho de banda disponible al sincronizar.
- Pipeline ML de pre-clasificación de defectos en pavimento o estructuras.
- Estrategia de archivado frío de fotos de relevamientos históricos.

---

## 7. Política de captura en móvil — permisos y GPS

### 7.1. Diálogo unificado de captura

Un único componente de UI gestiona los flujos de permisos, obtención de GPS, timeout y reintento. Aparece al accionar el botón de cámara en la app móvil. Los distintos estados se muestran en el mismo diálogo, evitando la inconsistencia de tener varios modales para casos relacionados.

### 7.2. Máquina de estados

```
[Tap en botón de cámara]
        │
        ▼
   ┌─────────────────────┐
   │  S0 Verificando     │ (~200ms, automático)
   │     permisos        │
   └──────────┬──────────┘
              │
       ┌──────┴──────┐
       │             │
       ▼             ▼
  S1-CAM-DENY    S1-LOC-DENY
       │             │
       └──────┬──────┘
              ▼
   ┌─────────────────────┐
   │ S2 Obteniendo GPS   │ ← spinner + contador
   │  0:00 → timeout     │
   └──────────┬──────────┘
              │
       ┌──────┼──────────┬─────────────┐
       ▼      ▼          ▼             ▼
   S3-OK  S3-LOWACC  S3-TIMEOUT   S3-NOSIGNAL
                                     (GPS apagado)
```

### 7.3. Estados detallados

| Estado | Mensaje al usuario | Botones |
|---|---|---|
| S0 — Verificando permisos | "Verificando permisos..." | (ninguno; transición automática) |
| S1-CAM-DENY — Cámara denegada | "Para tomar fotos necesitamos permiso de cámara." | **Ir a configuración** · Cancelar |
| S1-LOC-DENY — Ubicación denegada | "Para georreferenciar las fotos necesitamos permiso de ubicación." | **Ir a configuración** · Cancelar |
| S1-BOTH-DENY — Ambos denegados | "Necesitamos permiso de cámara y ubicación para continuar." | **Ir a configuración** · Cancelar |
| S2 — Obteniendo GPS | "Obteniendo posición GPS... 0:12" (contador en vivo) | Cancelar |
| S3-OK — Fix aceptable | (no se muestra; el diálogo se cierra y abre la cámara) | — |
| S3-LOWACC — Precisión baja | "La precisión es baja: ±X metros." | Reintentar · **Continuar igual** · Cancelar |
| S3-TIMEOUT — Sin fix | "No pudimos obtener la posición en X segundos." | **Reintentar** · Cancelar |
| S3-NOSIGNAL — GPS desactivado | "La ubicación está desactivada en el dispositivo." | **Abrir ajustes de ubicación** · Cancelar |

### 7.4. Parámetros configurables por plantilla

| Parámetro | Default | Rango | Descripción |
|---|---|---|---|
| `gps_timeout_seconds` | 30 | 10–120 | Tiempo máximo para obtener fix antes de mostrar S3-TIMEOUT |
| `gps_accuracy_threshold_m` | 50 | 5–500 | Si el fix supera este valor (peor precisión), pasa a S3-LOWACC |
| `allow_continue_with_low_accuracy` | true | bool | Si false, S3-LOWACC no permite "Continuar igual" |
| `allow_manual_coordinates_entry_mobile` | false | bool | Si true, S3-TIMEOUT incluye opción "Ingresar manualmente" |

> Esto permite que una plantilla de "inspección de puente" exija precisión estricta (`gps_accuracy_threshold_m=20`, `allow_continue_with_low_accuracy=false`), mientras que una plantilla de "inspección general" sea más laxa.

### 7.5. Notas técnicas para implementación

- En MAUI, los permisos se gestionan con `Permissions.RequestAsync<>` y el deep link a configuración con `AppInfo.ShowSettingsUI()`.
- El GPS se solicita en modo one-shot con `GeolocationRequest`, no listener continuo, para evitar consumo de batería innecesario.
- El valor de `accuracy` (en metros) debe filtrarse contra `gps_accuracy_threshold_m` antes de aceptar el fix.

---

## 8. Metodología de ejecución

### 8.1. Marco metodológico

**Scrum con vertical slicing**. Cada sprint entrega una funcionalidad completa end-to-end aunque limitada en alcance, atravesando todas las capas (móvil/web + backend + DB + tests).

### 8.2. Justificación

El proyecto tiene múltiples componentes (móvil + web + backend + sync + storage) y muchas decisiones por validar contra el cliente. El vertical slicing permite que el cliente vea (y critique) algo funcionando desde el primer sprint, lo que detecta cambios estructurales temprano cuando son baratos.

### 8.3. Estructura propuesta

| Etapa | Foco |
|---|---|
| Sprint 0 — Walking skeleton | Infraestructura transversal mínima: auth, abstracción de storage, esqueleto de sync, scripts `.bat`, pipeline de logs/errores/migraciones. Slice trivial end-to-end: login + crear relevamiento + un punto vacío + persistir + verlo en web. |
| Spike de sincronización (1 semana) | Validación del protocolo offline-first multi-colaborador con dos dispositivos físicos. Riesgo dominante; conviene resolverlo antes de comprometer slices. |
| Slice 1 | Dos dispositivos creando puntos offline en el mismo relevamiento, sincronizando, viendo los puntos del otro. |
| Slice 2 | Captura en modo detenido con plantilla genérica, persistencia local y subida. |
| Slice 3 | Modo móvil con radio configurable. |
| Slice 4 | Edición desde web (catálogo, comentarios, agrupación por punto, mapa). |
| Slice 5 | Plantillas de inspección de puente y pavimento + renderizado dinámico. |
| Slice 6 | Roles, áreas, asignación de colaboradores, permisos por punto. |
| Slice 7 | Carga manual desde web con EXIF. |
| Slice 8 | Storage configurable real (S3/FTP) + wizard de primer arranque. |
| Slice 9 | Panel de conflictos y mecanismos de resolución manual. |
| Slice 10 | Detección y UI de fusión de puntos cercanos. |

### 8.4. Información pendiente

[REQUIERE_INFO] Velocidad estimada del equipo (story points por sprint).

[REQUIERE_INFO] Duración del sprint (típicamente 2 semanas; confirmar con el equipo).

[REQUIERE_INFO] Cantidad total de sprints estimados.

---

## 9. Infraestructura y deploy

### 9.1. Ambiente local

Obligatorio para desarrollo. Todo el sistema debe poder levantarse en la máquina del desarrollador con scripts `.bat`. Esto implica:

- SQL Server local (Express o Developer Edition)
- Storage local (sistema de archivos)
- Backend, frontend web y workers como procesos .NET corriendo en localhost en distintos puertos
- App móvil en emulador Android/iOS apuntando al backend local

### 9.2. Ambientes superiores

[REQUIERE_INFO] Definir si habrá ambiente de Desarrollo compartido, Staging y Producción, o solo local + Producción.

[REQUIERE_INFO] Ambiente de destino: ¿on-premise de Vialidad? ¿Nube pública? ¿Híbrido?

### 9.3. Estrategia de deploy

[REQUIERE_INFO] No definida en la conversación. Inferencia razonable: cada uno de los procesos del sistema (backend API, frontend web, workers) se despliega como un servicio independiente, aunque compartan el repositorio y el código del monolito modular. La app móvil se distribuye vía store interno o sideload.

### 9.4. Estrategia de versionado

- **API**: versionada en URL (`/api/v1/...`).
- **Plantillas de inspección**: versión interna inmutable; cada relevamiento queda atado a una versión específica.
- **App móvil, backend, frontend, workers**: [REQUIERE_INFO] esquema de versiones (SemVer recomendado) y compatibilidad mínima requerida entre versiones de app móvil y backend.

---

## 10. Restricciones técnicas adicionales

### 10.1. Seguridad

- ROPC con JWT bearer (deuda técnica DT-01 documentada en PROJECT-README Sección 9.3).
- Trazabilidad técnica de cambios sobre puntos, fotos y relevamientos vía log de eventos.
- Manejo de EXIF: las fotos pueden contener metadata sensible (modelo de cámara, número de serie del dispositivo). Política configurable por plantilla (`photo_strip_sensitive_exif`) preservando GPS y timestamp.
- [REQUIERE_INFO] Política de cifrado en reposo de la DB y del storage.
- [REQUIERE_INFO] Política de respaldo.

### 10.2. Performance

- Volumen previsto de fotos por relevamiento: ~250-350 MB con defaults; hasta varios GB si se conservan originales.
- Sincronización debe tolerar conexiones intermitentes y de baja calidad: chunked / resumable upload de fotos.
- **Blazor Server bajo redes inestables: aplica solo a la app web** (no a móvil). La app móvil corre Blazor Hybrid local, sin conexión continua con el servidor; solo necesita la API REST cuando sincroniza. Validar UX de reconexión SignalR para usuarios web en oficinas con red inestable.

### 10.3. Compatibilidad

- App móvil: [REQUIERE_INFO] Android e iOS. Validar plataformas obligatorias.
- Web: [REQUIERE_INFO] navegadores soportados. Asumir modernos (Chrome, Edge, Firefox, Safari recientes).
- Calidad de fix GPS: filtros de accuracy / HDOP para descartar fixes pobres antes de crear puntos. Valor configurable por plantilla.

### 10.4. Operativas

- Levantamiento local con scripts `.bat` obligatorio.
- Configuración de storage cambiable sin redeploy (vía panel de admin raíz).

---

## 11. Referencias y assets

### 11.1. Repositorio

[REQUIERE_INFO] No se mencionó nombre concreto del repositorio del proyecto. El documento de contexto inicial provino de un repositorio referido como "Ejemplo_IA_Contextualizaciones".

### 11.2. Documentación existente referenciada

- `Georeferencia/0_contextualizacion/contexto-generado-claudeia.md` — documento de contexto inicial provisto por el cliente, base de la conversación de relevamiento.

### 11.3. Capturas de pantalla

Ninguna compartida durante el relevamiento.

Ruta sugerida para capturas futuras: `/devs/assets/screenshots/`.

---

**Fin del documento — PROJECT-BRIEF.md**
