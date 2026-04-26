**Proyecto:** Sistema de Gestión de Relevamientos Georreferenciados de Vialidad
**Documento:** estrategia-ambientes_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-26
**Autor:** Generado por SA-09 via orquestador

---

# Estrategia de Ambientes

Define los ambientes en los que el sistema opera y la promoción entre ellos. Para el MVP, **el único ambiente confirmado por el cliente es Local-dev** (RNF-06 + `PROJECT-BRIEF` Sec. 9.1). Los ambientes superiores (Staging, Producción) están marcados como `[REQUIERE_INFO]` y se documentan a nivel de plantilla para activación cuando el cliente confirme.

---

## 1. Ambientes

### 1.1. Local-dev (confirmado)

| Aspecto | Configuración |
|---|---|
| Propósito | Desarrollo individual de cada miembro del equipo |
| Levantamiento | Scripts `.bat` (BT-03) |
| DB | SQL Server local (Express o Developer Edition) |
| Storage | Local FS por default; admin puede configurar S3 con cuenta personal |
| Frontend web | Blazor Server en `localhost:5001` (puerto sugerido) |
| Backend API | `localhost:5000` |
| Workers | Procesos .NET locales |
| App móvil | Emulador Android/iOS apuntando a `http://10.0.2.2:5000` (Android) / `http://localhost:5000` (iOS) |
| Datos | Seeds básicos del backend; cada dev mantiene su propia DB |
| Acceso a producción de cliente | No |
| Logs | Consola |
| Secretos | User Secrets de .NET |

### 1.2. Staging (`[REQUIERE_INFO]`)

| Aspecto | Configuración propuesta |
|---|---|
| Propósito | Ambiente de pre-producción donde QA valida releases antes de producción |
| Hosting | `[REQUIERE_INFO]` on-premise / nube pública / híbrido (`PROJECT-BRIEF` Sec. 9.2) |
| DB | SQL Server compartido del ambiente; datos no productivos |
| Storage | S3 (o equivalente) bucket `staging-photos` |
| Frontend web | URL pública de staging |
| Backend API | URL pública de staging con TLS |
| Workers | Servicios independientes |
| App móvil | Build "staging" instalable manualmente |
| Datos | Datos de prueba; refrescados periódicamente |
| Acceso | Equipo + cliente para validación |
| Logs | Sink remoto centralizado (`[REQUIERE_INFO]` herramienta) |
| Secretos | Secret manager del provider |

### 1.3. Producción (`[REQUIERE_INFO]`)

| Aspecto | Configuración propuesta |
|---|---|
| Propósito | Ambiente productivo del cliente |
| Hosting | `[REQUIERE_INFO]` |
| DB | SQL Server productivo con backups |
| Storage | S3 / FTP / SFTP según [CU-02](../02_especificacion_funcional/casos-de-uso/CU-02-configurar-storage_v1.0.md) decidido por admin raíz |
| Frontend web | URL pública productiva con TLS |
| Backend API | URL pública productiva con TLS |
| Workers | Servicios independientes con escalado a definir |
| App móvil | Build "production" distribuido vía store interno o sideload |
| Datos | Reales |
| Acceso | Restringido; cambios solo por pipeline release |
| Logs | Centralizado con retención `[REQUIERE_INFO]` |
| Secretos | Secret manager / KMS productivo |
| Alta disponibilidad | `[REQUIERE_INFO]` SLA esperado (RNF-11) |

---

## 2. Promotion flow

```
[Local-dev]
   │  (PR + CI verde + DoD cumplido)
   ▼
[main]
   │  (release SemVer + pipeline release)
   ▼
[Staging]
   │  (smoke + manual validation + sponsor approval)
   ▼
[Producción]
```

**Reglas:**
- No se despliega directo a Producción saltando Staging (cuando ambos existan).
- Cada deploy requiere build de release inmutable: el mismo artefacto que pasó por Staging es el que va a Prod.
- Hotfixes urgentes siguen el mismo flujo con prioridad alta; no se introducen cambios directos a producción.

---

## 3. Configuración por ambiente

| Configuración | Local-dev | Staging | Producción |
|---|---|---|---|
| Connection string DB | `localhost\\SQLEXPRESS;Database=SgrDev;Trusted_Connection=True` | Secret manager | Secret manager |
| Storage adapter default | Local FS | S3 (bucket staging) | Configurado por admin raíz vía wizard |
| JWT secret | User Secrets | Secret manager | Secret manager |
| ASPNETCORE_ENVIRONMENT | `Development` | `Staging` | `Production` |
| Logging level | Debug | Information | Warning |
| OpenStreetMap tiles | OSM público | OSM público o mirror interno | OSM público o mirror interno |
| CORS allowed origins | localhost | URL staging | URL prod |
| Outbox max retries | 5 (rápido en dev) | 10 | 10 |

> Los archivos `appsettings.{Environment}.json` materializan diferencias estructurales; los valores sensibles vienen del Secret Manager correspondiente.

---

## 4. Datos por ambiente

| Tipo de datos | Local-dev | Staging | Producción |
|---|---|---|---|
| Usuarios | Seeds: admin + 1 jefe + 2 relevadores | Datos de prueba reproducibles | Reales |
| Plantillas | Raíz seed | Raíz + Puente + Pavimento (datos de prueba) | Reales |
| Relevamientos | Vacío al iniciar | Datos de prueba | Reales |
| Fotos | Generadas localmente | Banco de fotos sintéticas o anonimizadas | Reales |
| Borrado de DB | Permitido y frecuente | Solo el responsable del ambiente | Nunca; se restaura desde backup |

> Política de **datos sintéticos en staging**: nunca se copian datos productivos sin anonimización (privacidad básica + buenas prácticas).

---

## 5. Backups y restore

`[REQUIERE_INFO]` Política formal de respaldo (alcance EX-15). Plantilla mínima cuando se confirme:

| Aspecto | Definición pendiente |
|---|---|
| Frecuencia | `[REQUIERE_INFO]` (sugerido: full diario + transactional cada 1h) |
| Retención | `[REQUIERE_INFO]` (RNF-10) |
| Restore drill | Trimestral con escenario simulado |
| Storage de fotos | Snapshot del provider correspondiente |

---

## 6. Observabilidad por ambiente

| Aspecto | Local-dev | Staging | Producción |
|---|---|---|---|
| Logs | Consola | Sink remoto | Sink remoto + retención larga |
| Métricas | `/metrics` local | Centralizadas | Centralizadas + alertas |
| Trazas | Por correlation_id | OpenTelemetry export | OpenTelemetry export |
| Alertas | No | Bajo volumen | Críticas + on-call (`[REQUIERE_INFO]` rotación) |

---

## 7. Pendientes a confirmar con el sponsor

- [ ] Existencia de Staging y Producción.
- [ ] Hosting (on-premise / nube / híbrido).
- [ ] Política de respaldo y SLA.
- [ ] Volumen esperado para dimensionar (RNF-09).
- [ ] Política de retención de datos (RNF-10).
- [ ] Tiempo aceptable de sincronización (RNF-12).

---

## 8. Trazabilidad

| Documento upstream | Aporte |
|---|---|
| `devs/intake/PROJECT-BRIEF.md` Sec. 9 | Local-dev confirmado; superiores `[REQUIERE_INFO]` |
| [arquitectura-solucion](../05_arquitectura_tecnica/arquitectura-solucion_v1.0.md) | Componentes a desplegar |
| [pipeline-cicd](pipeline-cicd_v1.0.md) | Pipeline cuyo destino son estos ambientes |

---

**Fin del documento — estrategia-ambientes_v1.0.md**
