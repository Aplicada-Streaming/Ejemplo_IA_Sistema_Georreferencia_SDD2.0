**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** alcance-proyecto_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-00 via orquestador

---

# Alcance del Proyecto

Este documento delimita qué incluye y qué excluye el MVP del sistema, y establece criterios para futuras inclusiones. La separación entre incluido y excluido no es caprichosa: cada exclusión tiene un motivo (tiempo, presupuesto, complejidad o dependencia externa) explícitamente declarado.

---

## 1. Alcance del MVP

### 1.1. Componentes incluidos

| Componente | Estado en MVP | Descripción funcional |
|---|---|---|
| Backend API REST (.NET monolito modular) | Incluido | Módulos Identity, Templates, Surveys, Points, Photos, Sync, Storage, SystemConfig. Expone OpenAPI versionado. |
| Worker de procesamiento de imágenes | Incluido | Normalización, thumbnails, manejo de EXIF, indexación de metadata. |
| Worker de sincronización | Incluido | Aplicación de eventos del outbox, resolución LWW por campo, detección de candidatos a fusión. |
| Frontend web (Blazor Server + MudBlazor) | Incluido | Login, listado/filtrado de relevamientos, edición de catálogos, asignación de colaboradores, panel de conflictos, carga manual desde EXIF, wizard de primer arranque. |
| Frontend móvil (MAUI Blazor Hybrid + MudBlazor) | Incluido | Login, lista de relevamientos asignados, captura offline-first, mapa colaborativo con marcadores, modos de captura (móvil con radio configurable / detenido), edición de catálogo de punto, panel de estado de sync. |
| Base de datos SQL Server | Incluido | Esquema completo del dominio + log de eventos + outbox + tabla de configuración del sistema. |
| Storage abstraído | Incluido | Adaptadores `local` (sistema de archivos) y `S3`. Adaptadores `FTP` y `SFTP` incluidos como adaptadores funcionales pero opcionalmente verificados según ambiente del cliente. |
| Scripts `.bat` de levantamiento local | Incluido | Cada desarrollador debe poder iniciar todos los servicios en su máquina sin docker. |

### 1.2. Funcionalidades incluidas (organizadas por área)

#### Gestión de relevamientos
- Crear, editar, abrir, cerrar, eliminar relevamientos (con respeto de permisos por punto y por rol).
- Listado y filtrado en web por área, estado, fecha y etiquetas.
- Asignación de colaboradores; reapertura de relevamientos cerrados desde el móvil.
- Etiquetas para búsqueda; metadata visible en la lista (área, dueño, colaboradores, cantidad de puntos, conflictos pendientes).

#### Plantillas de inspección
- Plantilla genérica raíz inmutable + herencia + versionado.
- Restricciones de herencia: una hija no puede cambiar tipo ni eliminar campos heredados, sí marcarlos "no aplica".
- Inmutabilidad por versión publicada; cada relevamiento queda atado a una versión.
- Renderizado dinámico de campos en frontend móvil y web.
- Plantillas iniciales del MVP: **inspección de puente** y **inspección de pavimento**.

#### Captura en móvil
- Diálogo unificado de captura con máquina de estados de permisos + GPS + timeout + reintento.
- Foto con georreferenciación automática + asociación a punto existente / creación de punto nuevo.
- Mapa colaborativo con todos los marcadores del relevamiento, centrado en GPS actual, reubicación manual de marcador antes de la foto.
- Doble-tap sobre marcador → catálogo del punto: previsualización, edición de comentarios y descripción de fotos individuales.
- Modo móvil con radio configurable y modo detenido; conmutación entre modos en cualquier momento.

#### Carga manual desde web
- Subida en lote de fotos previas con extracción de coordenadas EXIF.
- Cola de fotos pendientes de georreferenciar (ingreso manual de lat/lng o picker en mapa).
- Selección de modo de agrupación (móvil/detenido) antes del procesamiento.
- Generación de comentarios genéricos iniciales editables por el usuario.

#### Edición y revisión
- Catálogo completo agrupable por punto o vista plana.
- Edición de título y descripción de cada punto.
- Edición de comentario individual de cada foto.
- Agregar/eliminar fotos respetando permisos por punto.

#### Sincronización
- Trabajo offline pleno con persistencia local de datos y fotos.
- Sincronización manual y automática, push y pull diferencial.
- GUIDs en cliente para idempotencia.
- Outbox local con reintentos exponenciales (5s, 15s, 1m, 5m, 15m).
- Resolución LWW por campo basada en timestamp de evento original.
- Panel de estado de sincronización con detalle por entidad y reintento manual.
- Notificación post-sync de conflictos resueltos automáticamente y candidatos pendientes.

#### Panel de conflictos y resolución manual
- Panel web listando: sobrescrituras a revertir, candidatos a fusión, eliminaciones con actividad posterior, capturas rechazadas por relevamiento cerrado.
- UI de merge manual con valores lado a lado.
- UI de revisión de candidatos a fusión con mapa, fotos y comparación de campos.

#### Fusión de puntos cercanos
- Detección automática durante sync de puntos del mismo relevamiento creados por colaboradores distintos dentro de threshold geográfico y temporal.
- Acciones: **Fusionar** (con elección de posición resultante, valor por campo divergente, unificación de catálogos) o **Mantener separados** (los puntos quedan marcados como "no duplicados" y no se vuelven a proponer entre sí).
- Evento `PointMerge` preserva la historia.

#### Trazabilidad
- Metadata de origen por punto y por foto (creador, timestamp, frente, modo, device_id).
- Log de eventos por entidad con quién, cuándo, qué campo, valor anterior y nuevo, origen.
- UI de consulta de trazabilidad por punto.

#### Mapa colaborativo
- Diferenciación visual de puntos por colaborador.
- Filtros "ver solo mis puntos" / "ver todos".
- Indicador visual sobre puntos editados recientemente o con actividad.

#### Usuarios y permisos
- Registro con correo y contraseña para todos los roles excepto admin raíz (inicializado en primer arranque).
- Aceptación jerárquica: admin raíz acepta jefes de área; jefes aceptan relevadores de su área.
- Admin raíz puede dar de baja o inhabilitar jefes (inhabilitar es reversible).
- Permisos por punto: dueño edita todo del relevamiento, colaborador solo edita sus propios puntos.
- Login web disponible para todos los roles; móvil restringido a relevadores.

#### Configuración del sistema
- Wizard de primer arranque: admin raíz configura tipo de storage y credenciales.
- Tabla de configuración persistida; reconfigurable por admin raíz desde la web.
- Cambio de storage: datos previos siguen apuntando al adaptador con que fueron creados; sin migración masiva.

#### Autenticación
- ROPC con JWT bearer (deuda DT-01 documentada y asumida).

### 1.3. Calidad y operación incluidas en el MVP

- Tests unitarios y de integración del backend con cobertura objetivo según [calidad y pruebas](../08_calidad_y_pruebas/) (definido por SA-08).
- Tests end-to-end del flujo crítico de sincronización con dos dispositivos.
- Pipeline de logs/errores/migraciones operativo en local.
- Documentación OpenAPI viva del backend.
- Levantamiento local end-to-end con scripts `.bat`.

---

## 2. Exclusiones explícitas

Cada exclusión declara su justificación. Si una exclusión deja de tener motivo, se reabre la conversación de alcance con el sponsor.

| ID | Excluido | Justificación | Categoría | Reabrir si... |
|---|---|---|---|---|
| EX-01 | Etapa formal de cierre/aprobación del relevamiento por jefe de área | El cliente la mencionó pero no la formalizó durante el relevamiento. Pendiente de definición. | Definición de cliente | El sponsor confirma que la requiere para go-live. |
| EX-02 | Migración masiva de archivos entre adaptadores de storage al cambiar el tipo configurado | Operación costosa y específica de cada provider. No hay caso de uso urgente; los datos previos siguen accesibles por su adaptador original. | Complejidad / valor incremental bajo | El cliente decide consolidar todo el storage histórico bajo un único provider. |
| EX-03 | Pipeline de ML de pre-clasificación de defectos sobre las fotos | Fuera del scope funcional del MVP. Requiere dataset etiquetado y un equipo de ML. | Dependencia externa / costo | Se obtiene dataset etiquetado y el cliente prioriza la inversión. |
| EX-04 | Detección automática de fotos borrosas o con mala exposición | Mejora de UX pero no es bloqueante para capturar. Las fotos pobres pueden corregirse desde la web. | Valor incremental bajo en MVP | Reportes de campo muestran tasa significativa de fotos descartadas. |
| EX-05 | Compresión adaptativa según ancho de banda en sincronización | Los defaults de compresión por plantilla cubren el caso típico. | Complejidad incremental | Métricas reales muestran subidas largas con redes lentas. |
| EX-06 | Estrategia de archivado frío de relevamientos históricos | El volumen del MVP no justifica políticas de retención avanzadas. | Volumen actual | Volumen real supera el umbral acordado de almacenamiento caliente. |
| EX-07 | Migración del flujo de autenticación a OAuth 2.1 con code+PKCE | Decisión del cliente de quedarse en ROPC para apps de primera parte. Documentada como deuda DT-01. | Decisión del cliente / deuda asumida | Se introduce un cliente que no es de primera parte o el cliente decide endurecer postura de seguridad. |
| EX-08 | Integraciones con sistemas existentes de Vialidad (catastro, GIS provincial, otros) | No relevadas durante el intake. `[REQUIERE_INFO]` en PROJECT-README sec. 7.3. | Definición de cliente | El sponsor identifica integraciones obligatorias. |
| EX-09 | Notificaciones push del backend a las apps móviles | El intake no las requiere. La sincronización es activa por iniciativa del cliente. | Sin requisito en intake | El cliente requiere notificación inmediata de eventos del backend al móvil. |
| EX-10 | Modo "solo lectura" en móvil para roles no relevadores (jefe de área en campo) | El móvil queda restringido a rol relevador por requisito explícito. | Decisión del cliente | El cliente reabre la discusión por necesidad operativa concreta. |
| EX-11 | Migración masiva de relevamientos del proceso manual previo al sistema nuevo | No mencionada en intake. El MVP captura relevamientos nuevos. | Sin requisito en intake | El cliente requiere onboarding de datos históricos. |
| EX-12 | Reportería avanzada y dashboards analíticos sobre los datos | Fuera del MVP. La portabilidad de datos vía API REST permite construir reportería externa. | Posterior al MVP | Se identifica como prioridad de fase post-MVP. |
| EX-13 | Soporte de plataformas móviles fuera de Android/iOS modernos | `[REQUIERE_INFO]` plataformas obligatorias; el alcance se asume Android + iOS razonablemente recientes. | Definición de cliente | El cliente exige soporte de plataformas adicionales (Windows tablets, etc.). |
| EX-14 | Cifrado en reposo a nivel aplicación de DB y storage | `[REQUIERE_INFO]` política de cifrado. Asumimos cifrado a nivel infraestructura del provider/disco como suficiente para MVP. | Definición de cliente | Política de seguridad explícita lo exige. |
| EX-15 | Política formal de respaldo automatizado | `[REQUIERE_INFO]` en intake. Se asume estrategia básica del proveedor de DB durante el MVP. | Definición de cliente | El cliente formaliza una política de retención y recuperación. |
| EX-16 | Despliegues productivos en ambientes superiores (staging/prod) | `[REQUIERE_INFO]` ambientes superiores. El MVP cubre desarrollo local; el SA-09 dejará la base preparada. | Definición de cliente | El cliente confirma ambientes destino. |
| EX-17 | Listener de GPS continuo (background tracking) en móvil | El intake decide one-shot por consumo de batería. | Decisión técnica documentada | Aparece un caso de uso que requiera tracking continuo. |
| EX-18 | Reportes de productividad y exportación de datos a Excel/PDF | No relevado. | Sin requisito en intake | El cliente lo requiere para gestión interna. |

---

## 3. Criterios de inclusión y exclusión

Para mantener el alcance disciplinado durante el proyecto, toda solicitud de cambio se evalúa contra estos criterios:

### 3.1. Una funcionalidad **entra al MVP** si:
1. Está mencionada en `PROJECT-README.md` o `PROJECT-BRIEF.md`, o
2. Es una funcionalidad técnica derivada necesaria para que (1) funcione (ejemplo: la trazabilidad técnica deriva de la sincronización), o
3. Su omisión bloquearía un flujo end-to-end que el cliente espera ver funcionando, **y**
4. No supera el costo de implementación que comprometa el plazo del MVP (a fijar con `[REQUIERE_INFO]` sobre fecha objetivo).

### 3.2. Una funcionalidad **queda fuera del MVP** si:
1. No está mencionada en el intake ni deriva técnicamente de algo que sí esté, o
2. Es una mejora cuya ausencia no degrada el flujo crítico, o
3. Depende de información o decisiones que el cliente aún no proveyó (`[REQUIERE_INFO]`).

### 3.3. Cambios al alcance durante el proyecto

Cualquier cambio al alcance documentado aquí debe:
1. Plantearse al sponsor con impacto en plazo y presupuesto.
2. Reflejarse en una nueva versión de este documento (v1.1, v1.2…).
3. Encadenarse a las refinerías de SA-06 (backlog) y SA-07 (sprint plan) correspondientes.

---

## 4. Restricciones que afectan el alcance

| Restricción | Impacto sobre el alcance |
|---|---|
| Stack obligatorio .NET + Blazor + MAUI + SQL Server | Excluye exploración de stacks alternativos en el MVP. |
| Levantamiento local con `.bat` sin docker | Excluye dependencia de infraestructura distribuida en desarrollo. |
| Storage debe ser configurable y abstraído | Obliga a adaptadores y prohíbe acoplar lógica de dominio a un provider. |
| OpenStreetMap como mapa | Excluye Google Maps / Mapbox / proveedores comerciales. |
| ROPC con JWT bearer | Excluye OAuth 2.1 con code+PKCE en el MVP (deuda DT-01). |
| Fecha objetivo del MVP | `[REQUIERE_INFO]` |
| Presupuesto | `[REQUIERE_INFO]` |
| Tamaño y composición del equipo | `[REQUIERE_INFO]` |

---

## 5. Trazabilidad

| Documento upstream | Sección | Aporte al alcance |
|---|---|---|
| `devs/intake/PROJECT-README.md` | 5 — Requerimientos funcionales | Lista RF-01 a RF-62 mapeada a los bloques incluidos |
| `devs/intake/PROJECT-README.md` | 6 — Requerimientos no funcionales | Restricciones técnicas |
| `devs/intake/PROJECT-README.md` | 7 — Restricciones conocidas | Stack obligatorio y restricciones operativas |
| `devs/intake/PROJECT-BRIEF.md` | 1, 3 — Arquitectura y aplicaciones | Componentes incluidos |
| `devs/intake/PROJECT-BRIEF.md` | 8 — Metodología | Reservas para fases posteriores |

## 6. Documentos relacionados (esta sección)

- [Visión del producto](vision-producto_v1.0.md)
- [Roadmap del producto](roadmap-producto_v1.0.md)
- [Acuerdo de equipo](acuerdo-equipo_v1.0.md)

---

**Fin del documento — alcance-proyecto_v1.0.md**
