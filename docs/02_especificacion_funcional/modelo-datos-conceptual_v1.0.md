**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** modelo-datos-conceptual_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-02 via orquestador

---

# Modelo de Datos Conceptual

Modelo de dominio del sistema, expresado en términos de entidades, atributos conceptuales (no técnicos) y relaciones. Cada entidad surge de uno o más casos de uso. Las decisiones técnicas de persistencia (tipos SQL, índices, EAV vs. tablas dedicadas) son responsabilidad de SA-05.

---

## 1. Diagrama conceptual

```
┌─────────────┐       ┌──────────────┐
│  Usuario    │───*───│    Área      │
│             │       │              │
└─────┬───────┘       └──────────────┘
      │
      │ es dueño de / es colaborador en
      │
      ▼
┌─────────────────────┐         ┌──────────────────────┐
│   Relevamiento      │────1────│ VersiónDePlantilla   │
│                     │         │                      │
│                     │         └──────────┬───────────┘
└────────┬────────────┘                    │
         │                                 │ pertenece a
         │ contiene 1:N                    │
         ▼                                 ▼
┌─────────────────────┐         ┌──────────────────────┐
│      Punto          │         │     Plantilla        │
│  (georreferenciado) │         │  (con herencia)      │
└────────┬────────────┘         └──────────────────────┘
         │
         ├── 1:N ──> Foto
         ├── 1:N ──> ValorDeCampo (EAV)
         ├── 1:N ──> EventoDeAuditoría
         └── *:N (par) ──> CandidatoAFusión / NoDuplicado

┌─────────────────────┐
│ Configuración       │
│  Sistema (Storage)  │
└─────────────────────┘

┌─────────────────────┐
│ OperaciónPendiente  │   ← outbox local del móvil
│ (cliente)           │
└─────────────────────┘
```

---

## 2. Entidades principales

### 2.1. Usuario

Representa a una persona que opera el sistema con un rol determinado.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador | GUID único |
| email | texto | Identificación única del usuario en el sistema |
| nombre completo | texto | — |
| rol | enumeración | `admin_raiz` \| `jefe_area` \| `relevador` |
| estado | enumeración | `pendiente_aceptacion` \| `activo` \| `inhabilitado` \| `dado_de_baja` |
| área | referencia a Área | Aplica a `jefe_area` y `relevador`; null para `admin_raiz` |
| fecha de registro | fecha | — |
| fecha de aceptación | fecha | Cuando un nivel jerárquico superior lo aceptó |

> Reglas: ver [RN-11](reglas-de-negocio/RN-11-aceptacion-jerarquica-y-movil-restringido_v1.0.md).

### 2.2. Área

Unidad organizativa de Vialidad bajo la cual operan los jefes de área y relevadores.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador | — |
| nombre | texto | — |
| descripción | texto | — |

### 2.3. Plantilla

Define el conjunto de campos a capturar para un tipo de inspección. Soporta herencia y versionado.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador | — |
| nombre | texto | — |
| plantilla padre | referencia a Plantilla \| null | null en la plantilla raíz |
| es raíz | booleano | true solo en la plantilla genérica |
| es eliminable | booleano | false en la raíz |

### 2.4. VersiónDePlantilla

Cada vez que una plantilla se publica, se genera una versión inmutable. Los relevamientos quedan atados a una versión.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador | — |
| plantilla | referencia a Plantilla | — |
| número de versión | número | Secuencial dentro de la plantilla |
| estado | enumeración | `borrador` \| `publicada` |
| fecha de publicación | fecha \| null | — |
| campos definidos | colección de DefiniciónDeCampo | Resultado de aplicar herencia con la plantilla padre |
| parámetros de captura | objeto | Timeout GPS, accuracy threshold, radio del modo móvil, parámetros de compresión de fotos, threshold de fusión |

> Una vez publicada, una versión no puede modificarse. Ver [RN-05](reglas-de-negocio/RN-05-inmutabilidad-plantilla-publicada_v1.0.md).

### 2.5. DefiniciónDeCampo

Definición individual de un campo dentro de una versión de plantilla.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| clave | texto | Identifica el campo dentro de la plantilla |
| etiqueta visible | texto | Lo que ve el usuario |
| tipo | enumeración | `texto` \| `número` \| `fecha` \| `booleano` \| `selección` |
| reglas de validación | objeto | Min/max, requerido, opciones de selección |
| heredado de | referencia a Plantilla \| null | null si fue definido en esta plantilla |
| no aplica | booleano | Cuando una hija "oculta" un campo heredado |
| atributos visuales | objeto | Orden, agrupación, hint |

### 2.6. Relevamiento

Agregado raíz que reúne puntos georreferenciados de una campaña.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador (GUID generado en cliente) | — |
| nombre | texto | — |
| descripción | texto | — |
| área | referencia a Área | — |
| dueño | referencia a Usuario | El relevador que lo creó |
| colaboradores | colección de Usuario | Asignados por el dueño o el jefe |
| versión de plantilla | referencia a VersiónDePlantilla | — |
| estado | enumeración | `abierto` \| `cerrado` \| `eliminado_logico` |
| etiquetas | colección de texto | — |
| fecha de creación | fecha | — |
| fecha de cierre | fecha \| null | — |

> Reglas: ver [RN-02](reglas-de-negocio/RN-02-restricciones-eliminacion-relevamiento_v1.0.md), [RN-08](reglas-de-negocio/RN-08-capturas-post-cierre_v1.0.md).

### 2.7. Punto

Ubicación georreferenciada dentro de un relevamiento, con su catálogo de fotos y valores de campos.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador (GUID generado en cliente) | — |
| relevamiento | referencia a Relevamiento | — |
| coordenadas | objeto (latitud, longitud) | — |
| precisión GPS | número | En metros |
| título | texto | — |
| descripción | texto | — |
| creado por | referencia a Usuario | — |
| origen | enumeración | `mobile_capture` \| `mobile_edit` \| `web_edit` \| `web_manual_upload` |
| modo de captura | enumeración | `detenido` \| `movil` \| `web` |
| device id | texto \| null | Identificador del dispositivo de captura |
| fecha de creación | fecha | Timestamp del evento original, no de llegada al servidor |
| eliminado lógicamente | booleano | Soft-delete con timestamp |

> Reglas: ver [RN-01](reglas-de-negocio/RN-01-permisos-por-punto_v1.0.md), [RN-09](reglas-de-negocio/RN-09-deteccion-candidatos-fusion_v1.0.md).

### 2.8. Foto

Una fotografía asociada a un punto.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador (GUID generado en cliente) | — |
| punto | referencia a Punto | — |
| comentario | texto | — |
| referencia al storage | texto | Identificador del archivo en el adaptador con el que fue creada |
| adaptador de storage | enumeración | `local` \| `s3` \| `ftp` \| `sftp` |
| metadata | objeto | EXIF resumido, resolución, tamaño, generación de thumbnail |
| creada por | referencia a Usuario | — |
| origen | enumeración | mismo dominio que Punto.origen |
| fecha de creación | fecha | Timestamp original |

> Las fotos creadas con un adaptador conservan su referencia aún si el storage del sistema cambia. Ver [RN-12](reglas-de-negocio/RN-12-storage-datos-previos_v1.0.md).

### 2.9. ValorDeCampo

Valor de un campo de la plantilla aplicado a un punto. Modelo EAV (DD-07).

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| punto | referencia a Punto | — |
| clave del campo | texto | — |
| valor texto | texto \| null | — |
| valor numérico | número \| null | — |
| valor fecha | fecha \| null | — |
| valor booleano | booleano \| null | — |

### 2.10. EventoDeAuditoría

Cada cambio sobre puntos, fotos y relevamientos genera un evento append-only. Es el sustrato de la sincronización.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador | — |
| entidad afectada | enumeración | `relevamiento` \| `punto` \| `foto` |
| id de la entidad | identificador | — |
| tipo de evento | enumeración | `created` \| `field_updated` \| `deleted` \| `restored` \| `merged` |
| campo (cuando aplica) | texto | — |
| valor anterior | texto serializado \| null | — |
| valor nuevo | texto serializado \| null | — |
| autor | referencia a Usuario | — |
| origen | enumeración | mismo dominio que Punto.origen |
| device id | texto \| null | — |
| timestamp original | fecha | El del dispositivo cuando ocurrió |
| timestamp de aplicación | fecha | Cuando el backend aplicó el evento |

> Reglas: ver [RN-10](reglas-de-negocio/RN-10-eventos-append-only_v1.0.md).

### 2.11. CandidatoAFusión

Par de puntos del mismo relevamiento creados por colaboradores distintos dentro de un threshold geo y temporal.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador | — |
| punto A | referencia a Punto | — |
| punto B | referencia a Punto | — |
| distancia geodésica | número | En metros |
| diferencia temporal | número | En segundos |
| estado | enumeración | `pendiente` \| `fusionado` \| `mantenido_separado` |
| resuelto por | referencia a Usuario \| null | — |
| fecha de resolución | fecha \| null | — |
| evento de fusión | referencia a EventoDeAuditoría \| null | Cuando estado=fusionado |

### 2.12. ConfiguraciónSistema

Configuración global del sistema editada por el admin raíz, principalmente storage.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| storage activo | enumeración | `local` \| `s3` \| `ftp` \| `sftp` |
| credenciales | objeto cifrado | Específicas del adaptador activo |
| última actualización | fecha | — |
| actualizado por | referencia a Usuario | Siempre el admin raíz |

### 2.13. OperaciónPendiente (entidad cliente, no servidor)

Outbox local del móvil. No vive en el backend; es local al dispositivo.

| Atributo | Tipo conceptual | Descripción |
|---|---|---|
| id | identificador | — |
| evento serializado | objeto | Evento completo a aplicar al backend |
| estado | enumeración | `pendiente` \| `en_envio` \| `enviado` \| `error` \| `terminal_error` |
| intentos | número | — |
| último error | texto \| null | — |
| siguiente reintento | fecha | Calculado según política exponencial |

---

## 3. Cardinalidades resumidas

| Relación | Cardinalidad |
|---|---|
| Usuario — Área | N:1 (admin raíz no tiene área) |
| Plantilla — VersiónDePlantilla | 1:N |
| VersiónDePlantilla — Relevamiento | 1:N |
| Relevamiento — Usuario (dueño) | N:1 |
| Relevamiento — Usuario (colaboradores) | N:M |
| Relevamiento — Punto | 1:N |
| Punto — Foto | 1:N |
| Punto — ValorDeCampo | 1:N |
| Punto — EventoDeAuditoría | 1:N |
| (Punto, Punto) — CandidatoAFusión | N:M (par no ordenado) |

---

## 4. Cobertura por CUs

Cada entidad principal aparece en al menos un CU. La trazabilidad inversa garantiza que el modelo no tiene entidades huérfanas.

| Entidad | CUs que la manipulan |
|---|---|
| Usuario | CU-01, CU-05 |
| Área | CU-01 (implícita) |
| Plantilla | CU-03, CU-04 |
| VersiónDePlantilla | CU-03, CU-04 |
| Relevamiento | CU-04, CU-05, CU-08, CU-09, CU-10 |
| Punto | CU-06, CU-07, CU-08, CU-09, CU-10, CU-11, CU-12 |
| Foto | CU-06, CU-07, CU-09, CU-10 |
| ValorDeCampo | CU-06, CU-09, CU-10 |
| EventoDeAuditoría | Todos (los CUs generan eventos) |
| CandidatoAFusión | CU-08 (creación), CU-11 (resolución) |
| ConfiguraciónSistema | CU-02 |
| OperaciónPendiente | CU-08 (en cliente) |

---

## 5. Notas para SA-05

Decisiones técnicas que SA-05 debe tomar partiendo de este modelo:

- Tipo concreto de los identificadores (UNIQUEIDENTIFIER en SQL Server según `PROJECT-BRIEF` Sec. 1.6).
- Estructura física de `ValorDeCampo` (EAV ya decidido en DD-07).
- Estructura del log de eventos (append-only, particionamiento, índices).
- Estrategia de almacenamiento de credenciales del storage (DPAPI / Azure Key Vault / equivalente local).
- Manejo de soft-delete (flag vs. tabla histórica separada).
- Cifrado en reposo según `[REQUIERE_INFO]` del intake.

---

## 6. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| `devs/intake/PROJECT-BRIEF.md` Sec. 1.4, 1.6 | Módulos del backend y persistencia EAV |
| `devs/intake/PROJECT-BRIEF.md` Sec. 5 | Eventos, GUIDs, outbox, candidatos a fusión |
| [necesidades-negocio](../01_necesidades_negocio/necesidades-negocio_v1.0.md) | NBs que justifican cada entidad |
| Casos de uso CU-01 a CU-12 | Validación de cobertura del modelo |

---

**Fin del documento — modelo-datos-conceptual_v1.0.md**
