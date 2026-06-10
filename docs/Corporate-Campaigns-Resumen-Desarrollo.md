# Resumen ejecutivo — Corporate Email Campaigns (Backend)

**Módulo:** Campañas de correo corporativas  
**Repositorio:** `vhconsultor-backend`  
**Estado:** Desarrollo completo · compilación OK · APIs publicadas en APIM  
**Fecha de referencia:** Mayo 2026

---

## 1. Volumen de código (medido en repo)

| Métrica | Cantidad |
|---------|----------|
| Líneas de código nuevas (archivos Campaign) | ~1,088 |
| Líneas documentación API frontend | ~802 |
| **Total líneas entregables Campaign** | **~1,890** |
| Archivos nuevos dedicados al módulo | 10 |
| Archivos compartidos modificados | 6 |
| Tablas SQL nuevas (`Corporate`) | 3 |
| Entidades EF Core | 3 |
| Commands (CQRS) | 5 |
| Validators (FluentValidation) | 5 |
| Query repositories | 1 (4 consultas) |
| Application services | 1 |
| Controllers | 1 |
| Endpoints REST | 11 |
| DTOs request/response | 9 |
| Registros DI en `Program.cs` | 12 |

### Archivos nuevos

| Capa | Archivo |
|------|---------|
| Scripts | `CreateCorporateCampaignsTables.sql` |
| ModelLayer | `Campaign.cs`, `CampaignAttachment.cs`, `CampaignProspect.cs` |
| BusinessLayer | `CampaignCommands.cs`, `CampaignQueryRepository.cs`, `CampaignValidators.cs` |
| ApplicationLayer | `CampaignService.cs` |
| ApiLayer | `CampaignController.cs` |
| Docs | `API-Campaigns-Frontend.md` |

### Archivos compartidos extendidos

| Archivo | Extensión |
|---------|-----------|
| `ModelLayer/DBcontext.cs` | DbSets + configuración entidades |
| `ApplicationLayer/Shared/SendGridService.cs` | `SendTemplateEmailBccAsync` |
| `BusinessLayer/Shared/SendGridSettings.cs` | `CampaignTemplateId` |
| `BusinessLayer/Shared/Services/AzureBlobStorageService.cs` | `DownloadFileAsync` |
| `ApiLayer/Program.cs` | DI commands, queries, validators, service |
| `ApiLayer/appsettings.json` | `SendGrid:CampaignTemplateId` |

---

## 2. Alcance funcional entregado

| Área | Qué hace | Estado |
|------|----------|--------|
| **Campañas** | Crear, listar, detalle, actualizar (`Pending` / `Sent`) | ✅ |
| **Contenido** | Nombre, asunto, cuerpo HTML dinámico por campaña | ✅ |
| **Adjuntos** | Múltiples documentos → Azure Blob → URL en BD | ✅ |
| **Prospectos** | Lista de destinatarios por campaña (email + nombre) | ✅ |
| **Desde leads** | Importar múltiples leads; captura email, nombre, apellido | ✅ |
| **Manual** | Agregar correos individuales sin ser lead | ✅ |
| **Unicidad** | Mismo email no se repite en la misma campaña | ✅ |
| **Reutilización** | Mismo lead/email en otra campaña distinta | ✅ |
| **Envío** | SendGrid template + variables dinámicas | ✅ |
| **Privacidad BCC** | Un correo por destinatario; nunca ven otros emails | ✅ |
| **Copia al enviar** | `additionalBccEmails` (no persistidos) | ✅ |
| **Post-envío** | Campaña `Sent`; prospectos con `sentAt` | ✅ |
| **Documentación** | Guía completa para frontend + reglas de negocio | ✅ |
| **Template email** | Diseño HTML premium SendGrid (fuera de repo) | ✅ |
| **APIM** | 11 rutas publicadas en `corporate-vh` | ✅ |
| **Variable entorno** | `SendGrid__CampaignTemplateId` | ✅ |

---

## 3. Base de datos

| Tabla | Propósito | Reglas clave |
|-------|-----------|--------------|
| `Corporate.Campaigns` | Cabecera de campaña | Estados: `Pending`, `Sent` |
| `Corporate.CampaignAttachments` | Documentos adjuntos | FK cascade; URL Azure |
| `Corporate.CampaignProspects` | Destinatarios por campaña | UNIQUE (`CampaignId`, `Email`) |

**Script:** `Scripts/CreateCorporateCampaignsTables.sql` (idempotente)

---

## 4. APIs publicadas en APIM

| # | Método | Ruta | Función |
|---|--------|------|---------|
| 1 | GET | `/api/corporate/Campaign` | Listar campañas (`?status`) |
| 2 | GET | `/api/corporate/Campaign/{id}` | Detalle |
| 3 | POST | `/api/corporate/Campaign` | Crear (`Pending`) |
| 4 | PUT | `/api/corporate/Campaign/{id}` | Actualizar (solo `Pending`) |
| 5 | GET | `/api/corporate/Campaign/{id}/attachments` | Listar adjuntos |
| 6 | POST | `/api/corporate/Campaign/{id}/attachments` | Subir archivos (multipart) |
| 7 | GET | `/api/corporate/Campaign/{id}/prospects` | Listar destinatarios |
| 8 | POST | `/api/corporate/Campaign/{id}/prospects` | Agregar correos manuales |
| 9 | POST | `/api/corporate/Campaign/{id}/prospects/from-leads` | Importar desde leads |
| 10 | DELETE | `/api/corporate/Campaign/{id}/prospects/{prospectId}` | Quitar destinatario |
| 11 | POST | `/api/corporate/Campaign/{id}/send` | Enviar campaña (BCC) |

**Base URL:** `https://vh-apimanagement.azure-api.net/corporate-vh`

---

## 5. Integraciones

| Servicio | Uso en campañas |
|----------|-----------------|
| **SendGrid** | Dynamic template; envío individual BCC; adjuntos en cada correo |
| **Azure Blob Storage** | Upload adjuntos; download al enviar |
| **SQL Server (VH-DB)** | Persistencia campañas, prospectos, adjuntos |
| **Leads (`CustomerSubmissions`)** | Fuente de destinatarios vía `from-leads` |

### Variables SendGrid inyectadas

| Variable | Origen |
|----------|--------|
| `subject` | `Campaign.Subject` |
| `campaignBody` | `Campaign.BodyContent` |
| `campaignName` | `Campaign.Name` |
| `recipientFirstName` | Prospecto |
| `recipientLastName` | Prospecto |

### Configuración Azure

| Variable de entorno | Valor |
|---------------------|-------|
| `SendGrid__CampaignTemplateId` | ID del dynamic template en SendGrid |

---

## 6. Esfuerzo estimado por capa (backend)

| Capa / entregable | Commands / métodos | Horas (baja) | Horas (alta) |
|-------------------|-------------------|--------------|--------------|
| SQL + entidades + DbContext | 3 tablas, 3 entities | 8 | 14 |
| Commands + validators | 5 commands, 5 validators, 9 DTOs | 18 | 28 |
| Queries (Dapper) | 4 consultas | 4 | 8 |
| CampaignService + envío BCC | SendAsync, dedup, adjuntos | 20 | 32 |
| Controller + manejo errores | 11 endpoints | 10 | 16 |
| SendGrid + Azure (extensión shared) | BCC + download blob | 8 | 14 |
| DI + configuración | Program.cs, appsettings | 2 | 4 |
| Documentación API frontend | Guía completa | 8 | 14 |
| Template HTML SendGrid (diseño) | Fuera de repo | 4 | 10 |
| **TOTAL backend Campaigns** | | **~82 h** | **~140 h** |

---

## 7. Escenarios de costo (solo backend Campaigns)

| Tarifa | Bajo (~82 h) | Medio (~111 h) | Alto (~140 h) |
|--------|--------------|----------------|---------------|
| **$65/hr** (blended) | ~$5.3K | ~$7.2K | ~$9.1K |
| **$95/hr** (mid-senior) | ~$7.8K | ~$10.5K | ~$13.3K |
| **$130/hr** (senior/agencia) | ~$10.7K | ~$14.4K | ~$18.2K |

*No incluye frontend CRM Campaigns (~40–56 h estimadas en análisis portal), configuración APIM operativa, costo SendGrid por envío, almacenamiento Azure, QA formal ni PM.*

---

## 8. Comparativa con estimación frontend (referencia portal v2.3.7)

| Concepto | Frontend (estimado) | Backend (este desarrollo) |
|----------|---------------------|---------------------------|
| Módulo | CRM — Email campaigns | Corporate Email Campaigns |
| Pantallas | 3 | — |
| Componentes UI | 14 | — |
| Endpoints API | — | 11 |
| Horas estimadas | 40 – 56 h | 82 – 140 h |
| Documentación | Incluida en portal | `API-Campaigns-Frontend.md` |

**Proyecto llave en mano Campaigns (front + back):** ~122 – 196 h combinadas.

---

## 9. Checklist de despliegue

| Paso | Estado |
|------|--------|
| Código backend compilado | ✅ |
| Script SQL ejecutado en VH-DB | ⬜ Pendiente operaciones |
| `SendGrid__CampaignTemplateId` en Azure App Service | ⬜ Pendiente configuración |
| Template SendGrid creado y probado | ✅ (diseño listo) |
| Rutas en APIM `corporate-vh` | ✅ |
| Frontend CRM Campaigns | ⬜ En desarrollo |

---

## 10. Flujo de negocio resumido

```
Crear campaña (Pending)
    → Editar nombre, asunto, body HTML
    → Subir documentos (Azure)
    → Agregar destinatarios (leads múltiples + correos manuales)
    → Revisar lista
    → Enviar (+ correos extra opcionales en BCC)
    → Campaña Sent (solo lectura)
```

**Regla crítica:** cada destinatario recibe su propio correo en copia oculta. Los clientes nunca ven el email de otros destinatarios.

---

## Documentos relacionados

- `docs/API-Campaigns-Frontend.md` — Guía detallada de consumo de APIs para el equipo frontend
- `Scripts/CreateCorporateCampaignsTables.sql` — Esquema de base de datos
