# Módulo de Oportunidades (Corporate) — Detalle técnico por fase

**Complemento de:** `Corporate-Oportunidades-Fases.md`  
**Audiencia:** desarrollo backend, frontend Corporate, QA.

---

## Convenciones

| Tema | Decisión |
|------|----------|
| Esquema SQL | `Corporate` (igual que `Contracts`, `Customers`) |
| Lead origen | `Ecommerce.CustomerSubmissions` |
| Adjuntos | Azure Blob Storage (patrón `InvoiceAttachments` / `UploadInvoiceAttachmentCommand`) |
| Correos | SendGrid (`SendGridService`, templates en configuración) |
| Tiempo | **UTC** en BD y API (`GETUTCDATE()` / `DateTime.UtcNow`). Conversión a hora local: front (después) |
| RBAC detallado | **Fuera de alcance** fases 1–5; permisos mínimos por query (`AssignedToUserId`, `ViewerUserId`) |
| API Corporate | Prefijo `api/corporate/...`, `[Authorize]` |

---

## Modelo de datos (visión global)

Se crea de forma incremental por fase. Nombres sugeridos:

```
Corporate.OpportunityStages          -- catálogo (Fase 2)
Corporate.Opportunities              -- cabecera (Fase 1)
Corporate.OpportunityFollowUps         -- seguimientos etapa (Fase 2)
Corporate.OpportunityFollowUpAttachments
Corporate.OpportunityComments          -- hilo libre (Fase 4)
Corporate.OpportunityCommentAttachments
Corporate.OpportunityCommentMentions   -- @userId (Fase 4)

Ecommerce.CustomerSubmissions          -- + ConvertedToOpportunityId (Fase 1)
Corporate.Contracts                    -- + OpportunityId opcional (Fase 3)
```

### `Corporate.Opportunities` (Fase 1)

| Columna | Tipo | Notas |
|---------|------|-------|
| OpportunityId | INT IDENTITY PK | |
| SubmissionId | INT NULL FK lógica → `Ecommerce.CustomerSubmissions` | |
| Status | NVARCHAR(20) | `Open`, `Won`, `Lost` |
| CurrentStageKey | NVARCHAR(50) NULL | Fase 2+ |
| Title | NVARCHAR(255) | ej. BrandName del lead |
| FirstName, LastName, Email, PhoneNumber, Country, BrandName, … | copia desnormalizada del lead | listados sin join pesado |
| SelectedPlatform, AccountType, ServiceType, … | opcional | |
| AssignedToUserId | INT NOT NULL | → `Global.Users` |
| ViewerUserId | INT NULL | supervisor |
| CustomerId | INT NULL | Fase 3 |
| ContractId | INT NULL | Fase 3 |
| ConvertedAt, ConvertedByUserId | DATETIME2, INT | |
| WonAt, WonByUserId | DATETIME2, INT | Fase 3 |
| LostAt, LostByUserId, LostReason | DATETIME2, INT, NVARCHAR(500) | Fase 3 |
| CreatedAt, UpdatedAt | DATETIME2 | |

Índices: `AssignedToUserId`, `ViewerUserId`, `Status`, `SubmissionId` (único si convertido).

### Lead — columna adicional (Fase 1)

```sql
ALTER TABLE [Ecommerce].[CustomerSubmissions]
ADD [ConvertedToOpportunityId] INT NULL;
```

Validación en convert: si `ConvertedToOpportunityId IS NOT NULL` → 409 Conflict.

### Catálogo etapas (Fase 2) — seed

`Corporate.OpportunityStages`: `StageKey`, `DisplayName`, `SortOrder`, `DefaultChecklistText`, `IsActive`.

Seed inicial:

| StageKey | DisplayName |
|----------|-------------|
| first_contact | Primer contacto |
| follow_up | Seguimiento |
| proposal_sent | Propuesta enviada |
| proposal_follow_up | Seguimiento de propuesta |
| negotiation | Negociación |
| formalization | Formalización |

(`won` / `lost` pueden ser solo `Status` en oportunidad, no etapa obligatoria.)

### Seguimientos (Fase 2)

**`Corporate.OpportunityFollowUps`**

| Columna | Notas |
|---------|-------|
| FollowUpId | PK |
| OpportunityId | FK |
| StageKey | etapa en la que aplica |
| Status | `Pending`, `Completed`, `Cancelled` |
| DueAt | alertas |
| CompletedAt, CompletedByUserId | |
| Notes | NVARCHAR(MAX) |
| IsRequired | BIT — primer contacto del día = 1 |

**`Corporate.OpportunityFollowUpAttachments`**: `FollowUpId`, `FileUrl`, `FileName`, `UploadedBy`, `UploadedAt`.

Regla negocio: `CompleteFollowUp` exige ≥1 attachment.  
Regla: `ChangeStage` valida follow-up requerido pendiente de etapa anterior o crea actividad en mismo request (definir en implementación; recomendado: **completar follow-up y opcionalmente cambiar stage en un solo POST**).

### Comentarios (Fase 4)

**`Corporate.OpportunityComments`**: `CommentId`, `OpportunityId`, `AuthorUserId`, `Body` (NVARCHAR(MAX)), `CreatedAt`, `UpdatedAt` NULL.

**`Corporate.OpportunityCommentAttachments`**: igual patrón que follow-up attachments.

**`Corporate.OpportunityCommentMentions`**: `CommentId`, `MentionedUserId`, `CreatedAt`.

Formato body sugerido para parseo: `@[Nombre Apellido](user:123)` o metadata JSON en columna `MentionsJson` además del texto mostrado.

---

## Fase 1 — Técnico

### Scripts

- `Scripts/Corporate_Create_Opportunities_Table_v1.sql`
- `Scripts/Ecommerce_Add_ConvertedToOpportunityId_CustomerSubmissions.sql`

### Backend

| Pieza | Descripción |
|-------|-------------|
| Entidades | `Opportunity` en `ModelLayer/Corporate/Entities` |
| `DBcontext` | `DbSet<Opportunity>`, configuración tabla `Corporate.Opportunities` |
| Commands | `ConvertLeadToOpportunityCommand`, `UpdateOpportunityAssignmentCommand` (opcional) |
| Queries | `OpportunityQueryRepository` (Dapper, patrón `LeadQueryRepository`) |
| Service | `OpportunityService` en `ApplicationLayer/Corporate` |
| Controller | `OpportunityController` → `api/corporate/Opportunity` |
| Validators | FluentValidation en convert request |

### Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/corporate/Opportunity/convert-from-lead/{submissionId}` | Body: `assignedToUserId`, `viewerUserId?` |
| GET | `/api/corporate/Opportunity` | Query: `status`, `mine=true` (filtra Assigned o Viewer = JWT UserId) |
| GET | `/api/corporate/Opportunity/{id}` | Detalle |
| PATCH | `/api/corporate/Opportunity/{id}/assignment` | Reasignar responsable/visor (opcional v1) |

### Lógica convert

1. Cargar `CustomerSubmission` por id.  
2. Si `ConvertedToOpportunityId` → error.  
3. Insert `Opportunity` (Status=`Open`, copiar campos, `ConvertedByUserId` del JWT).  
4. Update lead `ConvertedToOpportunityId`.  
5. Transacción única.

### Frontend (referencia)

- Botón en detalle de Lead.  
- Listado + detalle oportunidad.  
- Sin etapas aún en UI (o mostrar “Sin etapa”).

### QA

- No doble conversión.  
- Colaborador A no ve oportunidad de B (solo Assigned).  
- Viewer ve oportunidades donde `ViewerUserId = self`.

---

## Fase 2 — Técnico

### Scripts

- `Scripts/Corporate_Create_OpportunityStages_FollowUps_v1.sql` + seed stages.

### Backend

| Pieza | Descripción |
|-------|-------------|
| Entidades | `OpportunityStage`, `OpportunityFollowUp`, `OpportunityFollowUpAttachment` |
| Commands | `CreateFollowUpOnConvertCommand` (llamado desde convert, mover si convert se re-deploy), `CompleteFollowUpCommand`, `ChangeOpportunityStageCommand`, `CreateFollowUpCommand` |
| Queries | Pendientes: `GetPendingFollowUpsForUserQuery`, `GetOpportunityTimelineQuery` |
| Blob | Extender `AzureBlobStorageService` carpeta `OpportunityFollowUps/{opportunityId}/{followUpId}/` o settings nuevos en `AzureStorageSettings` |

### Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/corporate/Opportunity/stages` | Catálogo etapas activas |
| GET | `/api/corporate/Opportunity/{id}/follow-ups` | Lista seguimientos |
| POST | `/api/corporate/Opportunity/{id}/follow-ups` | Crear seguimiento manual (opcional) |
| POST | `/api/corporate/Opportunity/{id}/follow-ups/{followUpId}/complete` | `multipart/form-data`: notes + files (≥1) |
| PATCH | `/api/corporate/Opportunity/{id}/stage` | Body: `stageKey`; validar último pending requerido |
| GET | `/api/corporate/Opportunity/pending-follow-ups` | Para badge alertas (`AssignedToUserId = me`, `Status=Pending`, `DueAt <= end of day`) |

### Al convertir (ajuste Fase 1)

Tras crear oportunidad:

- `CurrentStageKey = 'first_contact'`
- Insert follow-up: `Status=Pending`, `DueAt` = fin del día CR, `IsRequired=1`, `StageKey=first_contact`

### Reglas

- Solo `AssignedToUserId` completa follow-ups de etapa (viewer comenta en Fase 4, no completa seguimiento salvo decisión negocio).  
- Cambio de etapa: sin validación de “solo siguiente”; cualquier `StageKey` activo del catálogo.  
- Historial: cada `CompleteFollowUp` + `ChangeStage` auditable por registros en tablas (no borrar).

### Frontend

- Detalle: panel “Pendiente hoy”, timeline de seguimientos.  
- Formulario completar con upload múltiple.  
- Selector etapa (dropdown).

### QA

- Completar sin archivo → 400.  
- Badge pendientes correcto timezone CR.  
- Salto de etapa permitido con evidencia registrada.

---

## Fase 3 — Técnico

### Scripts

- `Scripts/Corporate_Add_OpportunityId_To_Contracts.sql` — `OpportunityId INT NULL` en `Corporate.Contracts`
- Opcional: catálogo `Corporate.OpportunityLostReasons` (ReasonKey, DisplayName)

### Backend

| Pieza | Descripción |
|-------|-------------|
| Commands | `MarkOpportunityLostCommand`, `MarkOpportunityWonCommand`, `LinkContractToOpportunityCommand` |
| Integración | `CreateContractCommand`: aceptar `OpportunityId?`; al crear, setear `Opportunity.ContractId` y `CustomerId` |
| Validación | Solo `Status=Open` puede pasar a Won/Lost |

### Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/corporate/Opportunity/{id}/mark-lost` | Body: `lostReason`, `notes?` |
| POST | `/api/corporate/Opportunity/{id}/mark-won` | Body: `customerId?`, preparar contrato |
| POST | `/api/corporate/Opportunity/{id}/link-contract` | Body: `contractId` o redirigir a flujo create contract |
| POST | `/api/corporate/Contract` | Extender request con `OpportunityId` (opcional) |

### Flujo ganada recomendado

1. `mark-won` → `Status=Won`, `WonAt`, `WonByUserId`.  
2. UI abre wizard contrato (reusa `CreateContractRequest` + datos oportunidad).  
3. On success: `Opportunity.ContractId`, `Opportunity.CustomerId`.

### Frontend

- Botones cerrar ganada/perdida deshabilitados si no Open.  
- En ganada: link al contrato.

### QA

- No marcar lost/won dos veces.  
- Contrato vinculado aparece en detalle oportunidad.

---

## Fase 4 — Técnico

### Scripts

- `Scripts/Corporate_Create_OpportunityComments_v1.sql`

### Backend

| Pieza | Descripción |
|-------|-------------|
| Entidades | `OpportunityComment`, attachments, mentions |
| Commands | `CreateOpportunityCommentCommand` (parse @, insert mentions, upload files) |
| Queries | `GetOpportunityCommentsQuery` (paginado DESC o ASC) |
| Users search | `GET /api/corporate/users/search?q=` — filtrar `IsCorporate=1`, `IsActive=1` (reusar o crear query ligera) |
| Email | Nuevo template SendGrid `OpportunityMentionTemplateId` en `SendGridSettings`; método `SendOpportunityMentionEmailAsync` |

### Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/corporate/Opportunity/{id}/comments` | Lista + attachments + mentions |
| POST | `/api/corporate/Opportunity/{id}/comments` | `multipart`: body + files; parse mentions |
| GET | `/api/corporate/users/search` | Query `q`, min 2 chars, top 20 |

### Autorización mínima (sin RBAC)

Puede comentar si:

- `UserId == Opportunity.AssignedToUserId` OR  
- `UserId == Opportunity.ViewerUserId`

Lectura comentarios: mismos + usuarios mencionados (opcional).

### Email @mención

Payload template: `mentionedByName`, `opportunityTitle`, `commentBody`, `opportunityUrl` (front base URL + id), `brandName`.

Enviar async; fallo SendGrid no debe revertir comentario (log warning, patrón `CustomerSubmissionService`).

### Parseo @

Opción A: front envía `mentionedUserIds: [45, 12]` además del texto.  
Opción B: backend regex sobre `@[^(]+\(user:(\d+)\)`.

Recomendado **A** para menos errores.

### Blob path

`OpportunityComments/{opportunityId}/{commentId}/{guid}.ext`

### Frontend

- Componente comentarios tipo chat.  
- Autocomplete @ con debounce llamando search users.  
- Render menciones resaltadas.

### QA

- Mención dispara email (mock SendGrid en dev).  
- Viewer puede comentar en oportunidad ajena asignada a otro.  
- Adjunto opcional en comentario.

---

## Fase 5 — Técnico (SignalR)

### Paquetes

- `Microsoft.AspNetCore.SignalR` (incluido en ASP.NET Core)

### Backend

| Pieza | Descripción |
|-------|-------------|
| Hub | `OpportunityHub` en `ApiLayer/Hubs/` |
| Registro | `Program.cs`: `builder.Services.AddSignalR()`, `app.MapHub<OpportunityHub>("/hubs/opportunities")` |
| JWT en SignalR | Query `?access_token=` o header según política CORS front |
| Groups | `opportunity-{opportunityId}` al abrir detalle; `user-{userId}` para menciones personales |
| Emisión | Tras `CreateOpportunityCommentCommand.Save`: `IHubContext<OpportunityHub>.Clients.Group(...).SendAsync("NewComment", dto)` |

### Eventos (contrato)

| Evento | Payload |
|--------|---------|
| `NewComment` | `{ opportunityId, comment }` |
| `Mentioned` | `{ opportunityId, commentId, mentionedUserId }` → grupo `user-{id}` |

### Frontend

- `@microsoft/signalr` cliente.  
- Conectar al entrar módulo oportunidades; `JoinOpportunity(id)` en detalle.  
- Toast + append al hilo en `NewComment`.  
- Reconexión automática.

### Infra

- Azure App Service: WebSockets habilitados.  
- Sticky sessions si múltiples instancias (o Azure SignalR Service en escala — **fase 5.1 opcional**).

### QA

- Dos browsers: supervisor comenta, colaborador ve sin F5.  
- Usuario sin join al grupo no recibe evento de esa oportunidad.

---

## Fase 6 (opcional) — Técnico

- Script `RBAC_Add_Opportunities_Module_v1.sql` (`corporate.opportunities.*`)  
- Permisos: `view_all`, `convert`, `export`  
- Endpoints reportes: funnel por stage, lost reasons aggregate  
- Job Hangfire/scheduled: email diario pendientes (si se adopta scheduler)

---

## Registro en `Program.cs` (por fase)

Acumulativo:

```text
Fase 1: OpportunityService, commands, Lead convert extension
Fase 2: FollowUp commands, blob folder config
Fase 3: Mark won/lost, Contract OpportunityId
Fase 4: Comment commands, user search, SendGrid template
Fase 5: AddSignalR, OpportunityHub
```

---

## Orden de implementación recomendado

1. SQL Fase 1 → entidades → convert API → front lead button.  
2. SQL Fase 2 → follow-ups → pending count → front timeline.  
3. Fase 3 won/lost + contract link.  
4. Fase 4 comments + mentions email.  
5. Fase 5 SignalR sobre POST comment existente.  
6. Fase 6 si negocio aprueba.

---

## Riesgos / notas

| Riesgo | Mitigación |
|--------|------------|
| SignalR multi-instancia | Azure SignalR Service o una instancia hasta escalar |
| Emails duplicados (@ + notif responsable) | Solo mencionados en v1 |
| Archivos grandes | Reusar límites `AzureBlobStorageService` |
| Desincronización lead/oportunidad | Transacción en convert; no editar lead post-convert (o sync selectivo) |

---

*Documento técnico — VH Consultor Backend — Oportunidades. Versión 1.*
