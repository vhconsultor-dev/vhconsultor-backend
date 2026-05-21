# API — Asignación de clientes a usuarios Corporate

Documentación para el mantenimiento en **Corporate → Administrador de usuarios**: asignar qué **clientes (Customers)** puede atender cada **agente VH (usuario corporate)** y ver las **cuentas Amazon** de esos clientes.

---

## Para qué sirve (contexto de negocio)

En VH Consultor hay dos tipos de usuarios en `Global.Users`:

| Tipo | Quién es | Cómo entra |
|------|----------|------------|
| **Corporate** | Empleados / agentes VH (`IsCorporate = 1`) | Portal Corporate (login JWT vía APIM `corporate-vh`) |
| **Brand Partner** | Usuarios del cliente (`IsBrandPartner = 1`) | Portal Brand Partner (login + 2FA) |

Un **agente corporate** (Ana, Daniel, etc.) puede dar soporte operativo a **varios clientes** (Amig, Cachito, …): inventario, settlements, cuentas Amazon, etc.

**Problema que resuelve esta API:** antes no había forma clara de decir “Ana atiende al cliente 5 y al 8”. El campo `Users.CustomerId` solo sirve para **un** cliente (típico de usuarios Brand Partner).

**Solución:** tabla `[Global].[UserCustomerAssignments]` y estos 3 endpoints para:

1. **Asignar** un cliente a un usuario corporate (POST).
2. **Consultar** qué clientes tiene asignados un agente, con nombre de empresa y cuentas Amazon (GET).
3. **Inactivar o reactivar** una asignación sin borrar historial (PUT).

```text
Usuario Corporate (Ana)  ──<  UserCustomerAssignments  >──  Customer (Amig)
                                      │
                                      └── AmazonAccounts del Customer (todas)
```

---

## Requisitos previos

### 1. Base de datos

Ejecutar en Azure SQL (`vh-db`):

`Scripts/CreateUserCustomerAssignmentsTable.sql`

Crea `[Global].[UserCustomerAssignments]` con FK a `Global.Users` y `Corporate.Customers`.

### 2. Autenticación

Todos los endpoints requieren JWT:

```http
Authorization: Bearer {token}
```

El token se obtiene con el login Corporate habitual del portal (misma suscripción APIM **`corporate-vh`**). Quien use este mantenimiento debe ser un usuario con permisos de administración en Corporate (según RBAC del front).

### 3. URL base (producción APIM)

| Entorno | Base URL |
|---------|----------|
| **APIM Corporate (producción)** | `https://vh-apimanagement.azure-api.net/corporate-vh` |
| Local (desarrollo) | `https://localhost:{puerto}` |

**Recurso:** `/api/corporate/user-customer-assignments`

| Método | URL completa |
|--------|----------------|
| **GET** | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments` |
| **POST** | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments` |
| **PUT** | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments/{id}` |

---

## Formato común de respuesta

Todas las respuestas usan el envelope `ResponseStructure`:

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `status` | boolean | `true` = éxito lógico, `false` = error |
| `statusCode` | number | 200, 400, 404, 500 |
| `data` | object / array / null | Payload |
| `message` | string | Mensaje en inglés |
| `messageES` | string / null | Opcional |
| `errorNumber` | string / null | Opcional |
| `timestamp` | string (ISO) | Hora servidor (Costa Rica) |

Los nombres de propiedades en JSON salen en **camelCase** (`userId`, `companyName`, etc.).

---

## Resumen de endpoints

| # | Método | URL (APIM) | Uso en el administrador |
|---|--------|------------|------------------------|
| 1 | **GET** | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments` | Listar asignaciones / cargar clientes de un agente |
| 2 | **POST** | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments` | Agregar cliente a un agente |
| 3 | **PUT** | `https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments/{id}` | Quitar acceso (inactivar) o restaurar |

---

## 1. GET — Listar asignaciones

### Cuándo usarlo en el front

- Abrir pantalla **“Clientes asignados”** al seleccionar un usuario corporate en el administrador → `GET ?userId={id}`.
- Ver si un cliente ya tiene agentes → `GET ?customerId={id}`.
- Editar / ver detalle de una fila → `GET ?id={assignmentId}`.
- Mostrar solo asignaciones vigentes → `GET ?userId=12&isActive=true`.

### Request

```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments?userId=12&isActive=true
Authorization: Bearer {token}
```

### Query parameters (todos opcionales)

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `userId` | int | **Principal para el admin:** ID del usuario corporate (`Global.Users.UserId`). Devuelve todos los clientes que puede atender. |
| `customerId` | int | Filtrar por un cliente concreto. |
| `id` | int | Una sola asignación por `UserCustomerAssignmentId`. Si no existe → 404. |
| `isActive` | bool | `true` solo activas, `false` solo inactivas. Sin parámetro = todas. |

Puedes combinar parámetros (se aplican con **AND**).

### Respuesta exitosa (200)

`data` es un **array** de objetos con esta forma:

```json
{
  "status": true,
  "statusCode": 200,
  "message": "Found 2 assignment(s).",
  "data": [
    {
      "userCustomerAssignmentId": 1,
      "userId": 12,
      "customerId": 5,
      "isActive": true,
      "assignedAt": "2026-05-20T10:30:00",
      "assignedBy": 1,
      "updatedAt": null,
      "corporateUser": {
        "userId": 12,
        "firstName": "Ana",
        "lastName": "García",
        "email": "ana@vhconsultor.com",
        "isActive": true
      },
      "customer": {
        "customerId": 5,
        "companyName": "Amig SA",
        "nit": "310112345678",
        "companyType": "SA",
        "primaryEmail": "contacto@amig.com",
        "primaryPhone": "+506 2222-3333",
        "city": "San José",
        "countryId": 1,
        "clientStatus": "Active",
        "isActive": true
      },
      "amazonAccounts": [
        {
          "amazonAccountId": 101,
          "customerId": 5,
          "amazonAccountIdentifier": "AMIG-US-01",
          "isSeller": true,
          "isVendor": false,
          "amazonRegion": "NA",
          "isActive": true,
          "createdAt": "2026-01-15T08:00:00",
          "updatedAt": null
        },
        {
          "amazonAccountId": 102,
          "customerId": 5,
          "amazonAccountIdentifier": "AMIG-EU-01",
          "isSeller": false,
          "isVendor": true,
          "amazonRegion": "EU",
          "isActive": true,
          "createdAt": "2026-02-01T09:00:00",
          "updatedAt": "2026-03-10T11:00:00"
        }
      ]
    },
    {
      "userCustomerAssignmentId": 2,
      "userId": 12,
      "customerId": 8,
      "isActive": true,
      "assignedAt": "2026-05-18T14:00:00",
      "assignedBy": 1,
      "updatedAt": null,
      "corporateUser": { "userId": 12, "firstName": "Ana", "lastName": "García", "email": "ana@vhconsultor.com", "isActive": true },
      "customer": {
        "customerId": 8,
        "companyName": "Cachito Corp",
        "nit": "310198765432",
        "companyType": null,
        "primaryEmail": "info@cachito.com",
        "primaryPhone": null,
        "city": "Heredia",
        "countryId": 1,
        "clientStatus": "Active",
        "isActive": true
      },
      "amazonAccounts": []
    }
  ],
  "errorNumber": null,
  "timestamp": "2026-05-20T15:00:00.0000000"
}
```

### Campos importantes en `data[]`

| Campo | Uso en UI |
|-------|-----------|
| `userCustomerAssignmentId` | ID de la fila; usar en PUT para inactivar |
| `isActive` | Si el agente **sigue** pudiendo atender ese cliente |
| `customer.companyName` | Nombre en listado / combo |
| `customer.nit` | Identificación fiscal |
| `amazonAccounts` | Listado de cuentas del cliente (puede ser `[]` si el cliente no tiene cuentas aún) |

**Nota:** No se devuelve `refreshToken` de Amazon (solo datos de mantenimiento/visualización).

### Errores

**404** — solo cuando envías `id` y no existe:

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Assignment with ID 999 was not found.",
  "timestamp": "..."
}
```

**200 con lista vacía** — sin `id`, si no hay coincidencias:

```json
{
  "status": true,
  "statusCode": 200,
  "message": "Found 0 assignment(s).",
  "data": [],
  "timestamp": "..."
}
```

**500** — error interno.

---

## 2. POST — Asignar cliente a usuario corporate

### Cuándo usarlo en el front

- Botón **“Agregar cliente”** en la ficha del usuario corporate.
- El admin elige un **Customer** (de `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/customer` o catálogo existente) y guarda.

### Request

```http
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments
Authorization: Bearer {token}
Content-Type: application/json
```

```json
{
  "userId": 12,
  "customerId": 5,
  "assignedBy": 1
}
```

| Campo | Obligatorio | Reglas |
|-------|-------------|--------|
| `userId` | Sí | Debe existir en `Global.Users`, `IsCorporate = true`, usuario activo |
| `customerId` | Sí | Debe existir en `Corporate.Customers` |
| `assignedBy` | No | `UserId` de quien hace la asignación (auditoría) |

### Respuesta exitosa (200)

```json
{
  "status": true,
  "statusCode": 200,
  "message": "Customer assigned to corporate user successfully.",
  "data": {
    "userCustomerAssignmentId": 3,
    "assignment": {
      "userCustomerAssignmentId": 3,
      "userId": 12,
      "customerId": 5,
      "isActive": true,
      "assignedAt": "2026-05-20T16:00:00",
      "assignedBy": 1,
      "updatedAt": null,
      "corporateUser": {
        "userId": 12,
        "firstName": "Ana",
        "lastName": "García",
        "email": "ana@vhconsultor.com",
        "isActive": true
      },
      "customer": {
        "customerId": 5,
        "companyName": "Amig SA",
        "nit": "310112345678",
        "companyType": "SA",
        "primaryEmail": "contacto@amig.com",
        "primaryPhone": "+506 2222-3333",
        "city": "San José",
        "countryId": 1,
        "clientStatus": "Active",
        "isActive": true
      },
      "amazonAccounts": [
        {
          "amazonAccountId": 101,
          "customerId": 5,
          "amazonAccountIdentifier": "AMIG-US-01",
          "isSeller": true,
          "isVendor": false,
          "amazonRegion": "NA",
          "isActive": true,
          "createdAt": "2026-01-15T08:00:00",
          "updatedAt": null
        }
      ]
    }
  },
  "errorNumber": null,
  "timestamp": "2026-05-20T16:00:00.0000000"
}
```

Guarda `data.userCustomerAssignmentId` (o `data.assignment.userCustomerAssignmentId`) para futuros PUT/GET.

### Errores de negocio (400)

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "User with ID 12 is not a corporate user. Only corporate users can be assigned to customers.",
  "timestamp": "..."
}
```

Otros mensajes posibles:

| Mensaje (aprox.) | Causa |
|------------------|--------|
| `User with ID X was not found.` | `userId` inválido |
| `User with ID X is inactive.` | Usuario corporate desactivado |
| `Customer with ID X was not found.` | `customerId` inválido |
| `User X is already assigned to customer Y...` | Duplicado; usar PUT con `isActive: true` si estaba inactivo |
| `AssignedBy user with ID X was not found.` | `assignedBy` inválido |

### Validación (400)

```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "User ID is required and must be greater than zero., Customer ID is required...",
  "timestamp": "..."
}
```

---

## 3. PUT — Activar o inactivar asignación

### Cuándo usarlo en el front

- **Quitar** cliente del agente sin borrar registro → `isActive: false`.
- **Restaurar** acceso → `isActive: true` (misma pareja user + customer; no hace falta nuevo POST si ya existía la fila).

### Request

```http
PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments/3
Authorization: Bearer {token}
Content-Type: application/json
```

**Inactivar (dejar de atender el cliente):**

```json
{
  "isActive": false
}
```

**Reactivar:**

```json
{
  "isActive": true
}
```

| Campo | Descripción |
|-------|-------------|
| `isActive` | `false` = el agente ya no debe ver/trabajar ese cliente; `true` = asignación vigente |

### Respuesta exitosa — inactivar (200)

```json
{
  "status": true,
  "statusCode": 200,
  "message": "Assignment inactivated successfully.",
  "data": {
    "userCustomerAssignmentId": 3,
    "userId": 12,
    "customerId": 5,
    "isActive": false,
    "assignedAt": "2026-05-20T16:00:00",
    "assignedBy": 1,
    "updatedAt": "2026-05-20T17:30:00",
    "corporateUser": { "userId": 12, "firstName": "Ana", "lastName": "García", "email": "ana@vhconsultor.com", "isActive": true },
    "customer": { "customerId": 5, "companyName": "Amig SA", "nit": "310112345678", "..." : "..." },
    "amazonAccounts": [ "..." ]
  },
  "errorNumber": null,
  "timestamp": "2026-05-20T17:30:00.0000000"
}
```

### Respuesta exitosa — activar (200)

`message`: `"Assignment activated successfully."`  
Misma estructura en `data`, con `"isActive": true`.

### Error 404

```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "Assignment with ID 999 was not found.",
  "timestamp": "..."
}
```

---

## Flujo recomendado en el administrador de usuarios (Corporate)

### Pantalla: detalle de usuario corporate

```text
1. Admin abre usuario corporate (userId = 12)
2. GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments?userId=12&isActive=true
3. Tabla: companyName, NIT, cantidad amazonAccounts, botón Quitar
4. Botón "Asignar cliente" → modal con búsqueda de customers (API customers existente)
5. POST https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments
   Body: { userId: 12, customerId: seleccionado, assignedBy: adminUserId }
6. Refrescar GET del paso 2
```

### Quitar cliente del agente

```text
1. PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments/{userCustomerAssignmentId}
   Body: { "isActive": false }
2. Refrescar lista (o quitar fila en UI)
```

### Restaurar cliente previamente quitado

```text
1. Si conoces el assignment id → PUT con isActive: true
2. Si no existe fila (nunca se asignó) → POST nueva asignación
```

---

## Reglas de negocio (backend)

1. Solo usuarios con **`IsCorporate = true`** reciben asignaciones.
2. Un mismo par **`userId` + `customerId`** solo puede existir **una vez** (unique en BD).
3. Las **cuentas Amazon** no se asignan una a una: al asignar el **customer**, el agente ve **todas** las `Corporate.AmazonAccounts` de ese customer.
4. **Inactivar** la asignación no borra el registro; sirve para auditoría y reactivación.
5. El agente corporate **no** debe usar `Users.CustomerId` para multi-cliente; usar siempre esta tabla.

---

## Integración con otros módulos (futuro / front)

Cuando el agente corporate entre a módulos de un cliente (inventario BP, reportes, etc.), el front o el backend debe filtrar:

- Solo `customerId` presentes en `UserCustomerAssignments` con `isActive = true` para su `userId`.

Ejemplo de consulta al cargar sesión del agente:

```http
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/user-customer-assignments?userId={jwtUserId}&isActive=true
```

Usar `data[].customer.customerId` y `data[].amazonAccounts` para combos de contexto.

---

## Script y modelo de datos

| Elemento | Ubicación |
|----------|-----------|
| Tabla SQL | `[Global].[UserCustomerAssignments]` |
| Script creación | `Scripts/CreateUserCustomerAssignmentsTable.sql` |
| Controlador | `ApiLayer/Controllers/Corporate/UserCustomerAssignmentController.cs` |

Columnas de la tabla:

| Columna | Descripción |
|---------|-------------|
| `UserCustomerAssignmentId` | PK |
| `UserId` | Agente corporate |
| `CustomerId` | Cliente |
| `IsActive` | 1 = vigente, 0 = inactivo |
| `AssignedAt` | Fecha de alta |
| `AssignedBy` | Opcional, quien asignó |
| `UpdatedAt` | Último cambio (PUT) |

---

## Checklist de despliegue

- [ ] Ejecutar `CreateUserCustomerAssignmentsTable.sql` en `vh-db`
- [ ] Desplegar API con el controlador registrado
- [ ] Probar POST + GET + PUT con token corporate
- [ ] Conectar pantalla de administrador de usuarios Corporate

---

## Contacto / referencias

Misma base APIM: `https://vh-apimanagement.azure-api.net/corporate-vh`

- Usuarios corporate: `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Auth/users?isCorporate=true` (o la ruta de auth que use el portal)
- Catálogo de clientes: `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/customer`
- Cuentas Amazon por cliente: `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/corporate/AmazonAccount?customerId={id}` (también vienen anidadas en el GET de asignaciones)
