# PROJECT-README — Relevamiento del Cliente

**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** PROJECT-README.md
**Versión:** 1.0
**Fecha:** 2026-04-26
**Estado:** Borrador
**Audiencia:** Equipo del proyecto, sponsor del cliente, futuros desarrolladores que se incorporen

---

## Índice

1. [Identificación del proyecto](#1-identificación-del-proyecto)
2. [El cliente](#2-el-cliente)
3. [El problema actual](#3-el-problema-actual)
4. [Qué se quiere construir](#4-qué-se-quiere-construir)
5. [Requerimientos funcionales](#5-requerimientos-funcionales)
6. [Requerimientos no funcionales](#6-requerimientos-no-funcionales)
7. [Restricciones conocidas](#7-restricciones-conocidas)
8. [Definición de éxito](#8-definición-de-éxito)
9. [Notas adicionales](#9-notas-adicionales)

---

## 1. Identificación del proyecto

Sistema integral de gestión de relevamientos en campo para Vialidad, compuesto por una aplicación móvil orientada al trabajo offline-first en ruta, una aplicación web orientada a la revisión y gestión, y un backend con API REST que provee a ambas.

El núcleo del modelo es el **relevamiento**, que agrupa **puntos georreferenciados** con sus catálogos de fotos y datos estructurados definidos por **plantillas de inspección extensibles**. El proyecto resuelve el caso concreto de inspecciones viales (carpeta asfáltica, puentes), pero se diseña con vocación de generalización a otras inspecciones de obra pública.

---

## 2. El cliente

### 2.1. Organización

**Vialidad** es el cliente directo. La organización gestiona el trazado y mantenimiento de rutas y puentes. Su operación incluye campañas periódicas de inspección de los activos viales para evaluar su estado y planificar intervenciones.

[REQUIERE_INFO] Determinar el alcance institucional concreto (Vialidad nacional, provincial o municipal), ya que esto afecta volúmenes y posibles integraciones con otros sistemas.

### 2.2. Industria y contexto

Sector público, infraestructura vial, obra pública. El contexto trae implicancias específicas que el sistema debe contemplar:

- **Trabajo en campo con conectividad limitada o nula.** Los relevadores operan en rutas y puentes donde la señal de datos suele ser intermitente o inexistente.
- **Coordinación multi-colaborador en una misma campaña.** Un relevamiento puede ser asignado a varios relevadores simultáneamente, cada uno trabajando en un puente o tramo distinto.
- **Necesidad de trazabilidad técnica** de los cambios sobre los datos para resolver conflictos de sincronización y disputas de información, aunque sin requisitos formales de auditoría regulatoria.

### 2.3. Stakeholders identificados

| Stakeholder | Responsabilidad | Nivel de decisión |
|---|---|---|
| Referente del cliente | Definición funcional, validación de plantillas | [REQUIERE_INFO] |
| Sponsor del proyecto | Aprobación, prioridades | [REQUIERE_INFO] |
| Admin raíz (rol del sistema) | Configuración inicial, gestión de jefes de área | Operativo |
| Jefe de área (Supervisor) | Gestión de relevamientos del área, aceptación de relevadores, revisión y edición | Operativo |
| Usuario de área (Relevador) | Trabajo de campo, captura de puntos | Operativo |
| Colaborador asignado | Apoyo en relevamientos ajenos | Operativo (alcance restringido) |

### 2.4. Vocación de extensión

El cliente plantea explícitamente que el sistema debe poder usarse para otras inspecciones de obra pública más allá de los casos iniciales (puente y pavimento). Esto refuerza la decisión arquitectónica de modelar el relevamiento como una entidad agnóstica del tipo de inspección, dejando que las **plantillas configurables** definan los datos específicos a capturar.

---

## 3. El problema actual

### 3.1. Cómo se trabaja hoy

El relevamiento de estado de la infraestructura vial se ejecuta hoy de forma **predominantemente manual**. Los relevadores recorren las rutas y puentes, toman notas y fotografías, y consolidan la información a su regreso a oficina.

[REQUIERE_INFO] Confirmar el detalle del proceso actual: si se usa papel, planillas Excel, fotos en cámaras independientes sin georreferenciar, etc.

### 3.2. Qué no resuelve el proceso manual

- Las observaciones no quedan georreferenciadas sistemáticamente; localizar después un defecto reportado depende de descripciones textuales ("kilómetro 47, lado derecho").
- Las fotografías quedan desconectadas estructuralmente del activo inspeccionado: una foto y un comentario en una planilla son dos artefactos separados que el equipo de gabinete debe reconciliar manualmente.
- No existe mecanismo de colaboración en una misma campaña: si dos relevadores trabajan el mismo puente, sus reportes se consolidan en gabinete con costo de tiempo y posibilidad de pérdidas de información.
- El análisis posterior consolidado es lento porque los datos no están estructurados.

### 3.3. Qué dispara el cambio

El cliente está **replanteando el proceso completo** de captura y documentación del estado de los activos. La iniciativa busca:

- Acelerar el ciclo entre relevamiento de campo y disponibilidad del dato consolidado.
- Estructurar los datos para que sean analizables.
- Permitir colaboración real durante una campaña.

---

## 4. Qué se quiere construir

### 4.1. Visión del sistema

Un sistema integral compuesto por tres componentes principales que comparten un modelo de dominio común:

```
┌──────────────┐         ┌──────────────┐
│  App Móvil   │         │   App Web    │
│  (MAUI)      │         │   (Blazor)   │
│  Captura     │         │   Revisión   │
└──────┬───────┘         └──────┬───────┘
       │                        │
       └────────────┬───────────┘
                    │
                    ▼
           ┌────────────────┐
           │   Backend API  │
           │   (.NET REST)  │
           └────────────────┘
```

### 4.2. Componentes funcionales

**Aplicación móvil — el frente de captura.** Pensada para uso intensivo en campo, sin conectividad. Permite crear relevamientos, capturar puntos georreferenciados, asociar fotos y datos estructurados, y sincronizar cuando aparece señal.

**Aplicación web — el frente de revisión.** Permite a relevadores y jefes de área revisar relevamientos, editar catálogos, asignar colaboradores, cargar manualmente fotos previamente tomadas (con extracción de coordenadas desde EXIF), y resolver conflictos pendientes.

**Backend con API REST.** Provee la lógica de dominio, persistencia, gestión de plantillas, sincronización, almacenamiento de fotos abstraído (configurable entre local, S3, FTP, SFTP) y autenticación.

### 4.3. Actores del sistema

| Actor | Qué hace en el sistema |
|---|---|
| Admin raíz | Configura el sistema en el primer arranque (especialmente storage). Confirma, da de baja o inhabilita jefes de área. Único usuario con acceso total al sistema. |
| Jefe de área | Ve y gestiona todos los relevamientos de su área. Acepta a los relevadores que se registran en su área. Asigna colaboradores a relevamientos. Edita relevamientos para sus fines (acción permitida pero fuera del alcance estricto del sistema). Revisa y resuelve conflictos pendientes. |
| Relevador (dueño) | Crea, edita, abre, cierra y elimina **sus propios** relevamientos. Captura puntos en campo desde el móvil. Recibe alertas de conflictos en sus relevamientos. |
| Colaborador asignado | Trabaja en un relevamiento ajeno. Crea puntos nuevos. Edita los puntos que él mismo creó. **No puede** editar puntos creados por otros, ni eliminar el relevamiento. |
| Sistema (workers) | Procesa imágenes (normalización, thumbnails), drena outbox de sincronización, detecta candidatos a fusión de puntos. |

---

## 5. Requerimientos funcionales

Los requerimientos están organizados temáticamente. Cada uno se identifica con su prefijo (RF-XX) para trazabilidad posterior con casos de uso y user stories.

### 5.1. Gestión de relevamientos

- **RF-01.** Crear, editar, abrir, cerrar y eliminar relevamientos. La eliminación está restringida al dueño; los colaboradores asignados no pueden eliminar bajo ningún caso.
- **RF-02.** Listar y filtrar relevamientos desde la web con criterios de área, estado (abierto/cerrado), fecha y etiquetas.
- **RF-03.** Asignar uno o más colaboradores a un relevamiento existente.
- **RF-04.** Cerrar y reabrir un relevamiento desde el móvil para volver a capturar puntos.
- **RF-05.** Etiquetar relevamientos para facilitar la consulta posterior.
- **RF-06.** Mostrar metadata del relevamiento en la lista: área, dueño, colaboradores, cantidad de puntos, conflictos pendientes.

### 5.2. Plantillas de inspección

El sistema soporta plantillas de inspección con herencia y versionado para permitir tipos de relevamiento diversos sin proliferar código.

- **RF-07.** Provee una **plantilla genérica raíz** con valores iniciales que sirve como base para todas las demás. No es eliminable.
- **RF-08.** Permite crear plantillas hijas que heredan los campos del padre, agregan campos nuevos o sobrescriben atributos visuales/de validación de campos heredados.
- **RF-09.** Restricciones de herencia: una plantilla hija **no puede cambiar** el tipo de un campo heredado, ni eliminarlo. Sí puede marcarlo como "no aplica" para no mostrarlo.
- **RF-10.** Versionar plantillas: una plantilla publicada es inmutable; los cambios generan una nueva versión.
- **RF-11.** Cada relevamiento queda asociado a una plantilla y versión específicas; los relevamientos históricos siguen siendo legibles aunque la plantilla evolucione.
- **RF-12.** Los frontends renderizan los campos dinámicamente según la plantilla resuelta (con herencia ya aplicada) que devuelve la API. Agregar plantillas nuevas no requiere cambios en el frontend.
- **RF-13.** Cada plantilla puede definir parámetros de captura: timeout de GPS, umbral de precisión aceptable, parámetros de compresión de fotos, radio de captura para modo móvil, threshold de fusión de puntos.

### 5.3. Captura en móvil

- **RF-14.** Tomar una foto desde el dispositivo crea un punto georreferenciado con la foto asociada (si no hay marcador seleccionado).
- **RF-15.** Visualizar todos los marcadores existentes del relevamiento en el mapa.
- **RF-16.** Centrar el mapa en la posición GPS actual.
- **RF-17.** Reubicar manualmente un marcador antes de tomar la foto.
- **RF-18.** Seleccionar un marcador existente y asociar las próximas fotos a él.
- **RF-19.** Doble-tap sobre un marcador abre el catálogo del punto: previsualizar fotos, editar comentarios y título del catálogo, editar descripción de fotos individuales.
- **RF-20.** **Modo móvil con radio configurable**: las fotos se asocian al punto actual mientras el dispositivo esté dentro del radio. Al salir, la primera foto fuera crea un nuevo punto.
- **RF-21.** **Modo detenido**: todas las fotos se asocian al marcador actual, independientemente del movimiento físico del dispositivo.
- **RF-22.** Cambiar entre modos en cualquier momento del relevamiento.
- **RF-23.** Diálogo unificado de captura que gestiona permisos de cámara/ubicación, obtención de GPS con timeout configurable, y reintento. El detalle del comportamiento se documenta en PROJECT-BRIEF Sección 7.

### 5.4. Carga manual desde la web

Permite cargar relevamientos hechos sin la app móvil, por ejemplo fotos sacadas previamente con cámara independiente.

- **RF-24.** Crear un relevamiento desde la web y subir un lote de fotos.
- **RF-25.** Procesar el lote extrayendo coordenadas del EXIF de cada foto. Las fotos sin EXIF GPS quedan en una cola de "fotos pendientes de georreferenciar".
- **RF-26.** Para fotos sin GPS, permitir ingreso manual de coordenadas (formulario con lat/lng o picker en mapa).
- **RF-27.** Solicitar al usuario el modo de agrupación antes del procesamiento (móvil/detenido) para decidir cómo se agrupan las fotos en puntos según proximidad espacial y temporal.
- **RF-28.** Generar comentarios genéricos iniciales ("Cargado el [fecha] desde web") que el usuario edita posteriormente.

### 5.5. Edición y revisión

- **RF-29.** Ver el catálogo completo de fotos del relevamiento desde la web, agrupable por punto geográfico o como vista plana.
- **RF-30.** Editar título y descripción de cada punto geográfico.
- **RF-31.** Editar el comentario individual de cada foto.
- **RF-32.** Agregar y eliminar fotos respetando las reglas de permisos.
- **RF-33.** Mostrar bajo cada foto su comentario editable y un enlace a su ubicación en el mapa.

### 5.6. Sincronización multi-colaborador

- **RF-34.** Trabajo offline pleno en el móvil, con persistencia local de datos y fotos.
- **RF-35.** Sincronización **manual y automática** con el backend.
- **RF-36.** Sincronización bidireccional: empuja cambios propios y trae cambios de otros colaboradores del mismo relevamiento.
- **RF-37.** Identificadores GUID generados en el cliente para idempotencia.
- **RF-38.** Outbox local con reintentos exponenciales para operaciones pendientes.
- **RF-39.** Resolución automática de conflictos por last-write-wins por campo individual, basada en timestamp del evento original (no del momento de llegada al servidor).
- **RF-40.** Panel de estado de sincronización con detalle por entidad y botón de reintento manual.
- **RF-41.** **Notificación de conflictos**: post-sync, los usuarios afectados (jefe de área y/o relevador dueño) reciben aviso de conflictos resueltos automáticamente y de candidatos a fusión pendientes.
- **RF-42.** **Panel de conflictos pendientes** en la web, listando los conflictos detectados que requieren revisión humana: sobrescrituras automáticas que el usuario quiere revertir, candidatos a fusión, eliminaciones con actividad posterior, capturas rechazadas por relevamiento cerrado.
- **RF-43.** **Mecanismo de merge manual** para los casos del panel: UI que muestra lado a lado los valores en conflicto y permite al usuario decidir el valor final.

### 5.7. Fusión de puntos cercanos

Caso especial: dos colaboradores creando puntos en el mismo lugar físico (mismo puente, misma fisura) generan dos marcadores que conviene unificar.

- **RF-44.** Durante la sincronización, el sistema detecta puntos del mismo relevamiento creados por **distintos colaboradores** dentro de un radio configurable de cercanía geográfica y temporal. Estos puntos se marcan como **candidatos a fusión**, sin fusionar automáticamente.
- **RF-45.** UI de revisión de candidatos: vista de mapa con ambos puntos, listado lado a lado de fotos de cada uno, comparación de campos de plantilla con valores. Acciones disponibles: **Fusionar** o **Mantener separados**.
- **RF-46.** Al fusionar, el usuario decide: posición resultante (centroide, A o B), valor final por cada campo donde haya divergencia, y todas las fotos pasan a un único catálogo unificado.
- **RF-47.** Al "mantener separados", los puntos quedan marcados como "no duplicados" y no se vuelven a proponer entre sí.
- **RF-48.** El log de eventos preserva la historia completa de la fusión: cuándo se hizo, quién la decidió, qué valores había antes y cuáles quedaron.

### 5.8. Trazabilidad de cambios

- **RF-49.** Cada punto y cada foto registra metadata de origen: quién lo creó, cuándo, desde qué frente (móvil/web), modo de captura, identificador de dispositivo.
- **RF-50.** Log de eventos por punto, foto y relevamiento que registra cada cambio (quién, cuándo, qué campo, valor anterior, valor nuevo, origen).
- **RF-51.** UI de consulta de trazabilidad: al ver un punto se muestra su metadata de origen y el histórico de ediciones.

> La trazabilidad se incluye como funcionalidad técnica derivada del modelo de sincronización (los eventos *son* lo que se sincroniza). El cliente no planteó una etapa formal de auditoría regulatoria. Si más adelante se requiere una etapa de cierre o aprobación formal del relevamiento (por ejemplo, "Aprobado por jefe de área"), queda como trabajo a definir en una fase posterior.

[REQUIERE_INFO] Confirmar si se desea agregar una etapa formal de cierre/aprobación del relevamiento por parte del jefe de área.

### 5.9. Mapa colaborativo

- **RF-52.** Diferenciación visual de puntos por colaborador (color o ícono distinto por usuario).
- **RF-53.** Filtros en el mapa: "ver solo mis puntos" / "ver todos los puntos del relevamiento".
- **RF-54.** Indicador visual sobre los puntos editados después de su creación o con actividad reciente.

### 5.10. Usuarios y permisos

- **RF-55.** Registro con correo y contraseña para todos los roles excepto admin raíz, que se inicializa con la primera ejecución del sistema.
- **RF-56.** Aceptación jerárquica: el admin raíz acepta a los jefes de área; los jefes de área aceptan a los relevadores de su área.
- **RF-57.** El admin raíz puede **dar de baja** o **inhabilitar** a un jefe de área. La baja elimina el usuario; la inhabilitación lo deja existente pero sin posibilidad de operar (acción reversible).
- **RF-58.** Permisos por punto: el colaborador asignado solo edita los puntos que él mismo creó. El dueño puede editar todos los puntos del relevamiento (suyos y de colaboradores).
- **RF-59.** Login en la app web disponible para todos los roles. El móvil queda restringido a usuarios con rol de relevador.

### 5.11. Configuración del sistema

- **RF-60.** Wizard de primer arranque: en la primera ejecución del sistema, el admin raíz configura el tipo de storage de fotos (local, S3, FTP, SFTP) y sus credenciales.
- **RF-61.** Configuración persistida en una tabla del sistema; reconfigurable posteriormente desde la web por el admin raíz.
- **RF-62.** Cambio de storage: si hay datos previos, el sistema mantiene las referencias al adaptador con el que se generaron y solo usa el nuevo adaptador para datos nuevos. La migración masiva de archivos entre storages queda fuera del MVP.

---

## 6. Requerimientos no funcionales

- **RNF-01. Offline-first en móvil.** La aplicación móvil debe ser plenamente funcional sin conexión por períodos prolongados (días enteros).
- **RNF-02. Sincronización idempotente.** Reenvíos no deben duplicar datos ni producir efectos secundarios.
- **RNF-03. Trazabilidad técnica de cambios.** Todo cambio sobre puntos, fotos y relevamientos queda registrado en el log de eventos. No es un requisito de compliance regulatorio sino una funcionalidad técnica para sustento de la sincronización y de futuras consultas de origen.
- **RNF-04. Storage abstraído.** Cambiar el adaptador de storage no debe requerir cambios en el código de dominio.
- **RNF-05. Portabilidad de frontend.** El backend expone una API REST documentada con OpenAPI versionado, de manera que sea posible reemplazar el frontend (web o móvil) por otra tecnología (por ejemplo, React) sin afectar el backend.
- **RNF-06. Levantamiento local.** Todo el sistema debe poder correrse localmente con scripts `.bat` para desarrollo.
- **RNF-07. Renderizado dinámico de plantillas.** Agregar una plantilla nueva no debe requerir cambios en el código del frontend.
- **RNF-08. Autenticación con JWT bearer.** Las APIs usan JWT con flujo ROPC. Decisión del cliente; ver Sección 9.3 sobre la deuda asumida.
- **RNF-09.** [REQUIERE_INFO] Volumen esperado: cantidad de relevamientos por mes, cantidad típica de fotos por relevamiento, usuarios concurrentes en horas pico.
- **RNF-10.** [REQUIERE_INFO] Política de retención de datos.
- **RNF-11.** [REQUIERE_INFO] SLA de disponibilidad esperada.
- **RNF-12.** [REQUIERE_INFO] Tiempo máximo aceptable de sincronización para un relevamiento típico.

---

## 7. Restricciones conocidas

### 7.1. Stack tecnológico obligatorio

Decisiones tomadas por el cliente, no negociables para el MVP:

| Componente | Tecnología elegida |
|---|---|
| Backend | .NET con ASP.NET Core, API REST |
| Frontend web | Blazor .NET con páginas Interactive Server + MudBlazor |
| Frontend móvil | MAUI .NET híbrido con páginas Blazor + MudBlazor |
| Base de datos | SQL Server |
| Mapas | OpenStreetMap |
| Autenticación | ROPC con JWT bearer |

### 7.2. Restricciones operativas

- **Levantamiento local del sistema completo mediante scripts `.bat`.** Cada desarrollador debe poder iniciar todos los servicios (backend, frontend web, workers, DB local, storage local) en su máquina sin docker ni infraestructura adicional.
- **Storage de fotos configurable** entre los proveedores listados (local, S3, FTP, SFTP) y transparente al backend.

### 7.3. Restricciones pendientes de definición

[REQUIERE_INFO] Tiempo disponible / fecha objetivo de entrega del MVP.

[REQUIERE_INFO] Presupuesto.

[REQUIERE_INFO] Tamaño y composición del equipo (cantidad de desarrolladores backend, frontend, móvil; experiencia previa con MAUI).

[REQUIERE_INFO] Integraciones obligatorias con sistemas existentes de Vialidad (catastro, GIS provincial, sistemas de obra pública).

---

## 8. Definición de éxito

El cliente no planteó métricas explícitas durante la conversación de relevamiento. Las inferencias razonables que el equipo propone, sujetas a validación con el cliente:

- **Reducción del tiempo entre relevamiento de campo y disponibilidad del dato consolidado.** Indicador medible: días desde la captura en campo hasta que el dato es analizable en gabinete.
- **Cobertura del proceso digital.** Indicador medible: porcentaje de relevamientos del organismo realizados a través del sistema, comparado con el proceso manual.
- **Adopción efectiva por relevadores de campo.** Medible por uso real (cantidad de puntos capturados por mes y por usuario), no solo por instalaciones.
- **Capacidad de extender a otros tipos de inspección sin reescritura.** Medible por la facilidad con que se incorpora una nueva plantilla, sin requerir cambios en el código del frontend.

[REQUIERE_INFO] Validar estas métricas con el sponsor del proyecto y agregar valores objetivo concretos.

---

## 9. Notas adicionales

### 9.1. Decisiones heredadas del cliente

Durante el relevamiento se identificaron decisiones que el cliente trajo predefinidas:

- **Stack .NET completo** (backend, web, móvil) con MudBlazor y SQL Server.
- **Microservicios completos** como preferencia inicial. Esta preferencia fue **revisada durante la conversación** y descartada en favor de **monolito modular + workers**, una vez aclarado que la justificación inicial (portabilidad de frontend) no requería microservicios. La portabilidad se logra con una API REST limpia, independientemente de la granularidad interna del backend.
- **OpenStreetMap** para tiles del mapa.
- **ROPC con JWT** para autenticación.

### 9.2. Riesgos identificados

Después del análisis y reclasificación realizada con el cliente, los riesgos vigentes son:

| ID | Riesgo | Probabilidad | Impacto | Notas |
|---|---|---|---|---|
| R-01 | MAUI Blazor Hybrid + acceso a hardware (cámara, GPS, background tasks, batería) | Media | Alto | Si el equipo no tiene experiencia previa con MAUI, sumar buffer de aprendizaje. |
| R-02 | Calidad del fix de GPS en campo | Alta | Medio | Mitigación: filtros de accuracy, UI de reintento, ingreso manual como fallback. |
| R-03 | Volumen y manejo de fotos por relevamiento | Media | Medio | Mitigación: normalización configurable por plantilla con defaults sensatos. |
| R-04 | Granularidad fina de permisos (por punto, no por relevamiento) | Baja | Medio | Capa de autorización fina ya considerada en diseño. |
| R-05 | Transaccionalidad backend ↔ storage externo | Media | Medio | Patrón outbox para evitar fotos huérfanas o registros sin foto. |
| R-06 | Blazor Server bajo redes inestables (solo aplica a la web) | Baja | Bajo | La web es para revisión, no captura crítica. Reload restablece sesión. La parte móvil corre Blazor Hybrid local, no aplica. |

> La sincronización multi-colaborador no aparece en esta lista de riesgos porque no es un riesgo, es la funcionalidad central del sistema. La complejidad de su diseño (catálogo de conflictos y mecanismos de resolución) está documentada como diseño explícito en PROJECT-BRIEF Sección 5.

### 9.3. Deudas técnicas asumidas

- **DT-01. Autenticación con flujo ROPC.** El cliente requiere ROPC con JWT bearer. OAuth 2.1 desaconseja explícitamente este flujo por entregar credenciales del usuario directamente a la app cliente, sin separación entre identity provider y consumidor. Se asume el riesgo en favor de la simplicidad operativa para apps de primera parte. **Plan de revisión:** evaluar migración a authorization code + PKCE cuando el sistema se estabilice o cuando se introduzca un primer cliente que no sea de primera parte.

### 9.4. Acuerdos confirmados durante el relevamiento

- El sistema se diseña con **vocación de extenderse a otras inspecciones de obra pública**, no solo Vialidad.
- La **trazabilidad técnica** (log de eventos por entidad) es un requisito firme del modelo, derivado de la necesidad de sincronización y resolución de conflictos. **No** equivale a un requisito de auditoría regulatoria, que no fue planteado por el cliente.
- Una posible **etapa de cierre o aprobación formal del relevamiento** por parte del jefe de área quedó mencionada pero no formalizada. Pendiente de definición con el cliente.
- **Permisos por punto:** el dueño puede editar todos los puntos del relevamiento (suyos y de colaboradores); los colaboradores solo editan los puntos que ellos mismos crearon. Confirmado.
- Los **relevamientos no pueden ser eliminados por colaboradores asignados.** Confirmado.
- El **admin raíz** puede dar de baja o **inhabilitar** jefes de área; la inhabilitación es reversible.

---

**Fin del documento — PROJECT-README.md**
