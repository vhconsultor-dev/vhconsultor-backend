# 📚 Documentación Completa: APIs de Gestión de Permisos (RBAC)

## 🎯 ¿Qué Pretendemos con Esto?

El sistema de **Permisos (Permissions)** es la base del control de acceso en nuestra aplicación. Un permiso representa la **combinación de un Recurso (Resource) + una Acción (Action)**, y permite controlar de forma granular qué usuarios pueden realizar qué operaciones en el sistema.

### Objetivos:

1. **Gestión Centralizada**: Crear, modificar y eliminar permisos desde el frontend sin necesidad de scripts SQL
2. **Flexibilidad**: Asignar permisos a roles y usuarios de forma dinámica
3. **Escalabilidad**: Agregar nuevos permisos fácilmente cuando se crean nuevos módulos (ej: Facturas, Clientes, Reportes)
4. **Control Granular**: Definir permisos específicos como `invoices.view`, `invoices.create`, `invoices.edit`, `invoices.delete`
5. **Auditoría**: Mantener un registro de qué permisos existen y cuándo fueron creados/modificados

---

## 🔐 Autenticación

**TODAS las APIs requieren autenticación mediante JWT Token.**

Debes incluir el token en el header de cada petición:

```
Authorization: Bearer YOUR_JWT_TOKEN
```

Si no incluyes el token o es inválido, recibirás un error `401 Unauthorized`.

---

## 📍 Base URL

**Desarrollo (Local):**
```
http://localhost:5262/api/Permission
```

**Producción:**
```
https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission
```

---

## 🚀 Resumen Rápido de Endpoints

| Método | Endpoint (Producción) | Descripción |
|--------|----------------------|-------------|
| `POST` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission` | Crear nuevo permiso |
| `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission` | Listar permisos (con filtros) |
| `GET` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/{permissionId}` | Obtener permiso por ID |
| `PUT` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/{permissionId}` | Actualizar permiso |
| `DELETE` | `https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/{permissionId}` | Eliminar permiso |

**Nota:** Todos los endpoints requieren autenticación JWT en el header `Authorization: Bearer YOUR_JWT_TOKEN`

---

## 📋 Endpoints Disponibles

### 1. Crear Permiso

Crea un nuevo permiso en el sistema combinando un Recurso con una Acción.

**Endpoint:** 
- **Producción:** `POST https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission`
- **Local:** `POST http://localhost:5262/api/Permission`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body (JSON):**
```json
{
  "resourceId": 1,
  "actionId": 2,
  "permissionName": "View Invoices",
  "permissionKey": "invoices.view",
  "description": "Allows viewing invoice records"
}
```

**Parámetros:**

| Campo | Tipo | Requerido | Descripción | Ejemplo |
|-------|------|-----------|-------------|---------|
| `resourceId` | `integer` | ✅ Sí | ID del recurso (módulo) | `1` |
| `actionId` | `integer` | ✅ Sí | ID de la acción | `2` |
| `permissionName` | `string` | ✅ Sí | Nombre legible del permiso | `"View Invoices"` |
| `permissionKey` | `string` | ✅ Sí | Clave única del permiso | `"invoices.view"` |
| `description` | `string` | ❌ No | Descripción del permiso | `"Allows viewing invoice records"` |

**Validaciones:**

- ✅ `resourceId` debe ser mayor que 0
- ✅ `actionId` debe ser mayor que 0
- ✅ `permissionName` es obligatorio, máximo 200 caracteres
- ✅ `permissionKey` es obligatorio, máximo 100 caracteres
- ✅ `permissionKey` solo puede contener: letras, números, guiones bajos (`_`), puntos (`.`) y guiones (`-`)
- ✅ `permissionKey` debe ser único en todo el sistema
- ✅ `description` es opcional, máximo 500 caracteres
- ✅ El `resourceId` debe existir en la tabla `Resources`
- ✅ El `actionId` debe existir en la tabla `Actions`

**Response Success (200 OK):**
```json
{
  "success": true,
  "message": "Permission created successfully",
  "data": {
    "permissionId": 15,
    "permissionName": "View Invoices",
    "permissionKey": "invoices.view",
    "message": "Permission created successfully"
  },
  "statusCode": 200
}
```

**Response Error - Validación (400 Bad Request):**
```json
{
  "success": false,
  "message": "Permission key is required, Permission key can only contain letters, numbers, underscores, dots and hyphens",
  "data": null,
  "statusCode": 400
}
```

**Response Error - Recurso no encontrado (404 Not Found):**
```json
{
  "success": false,
  "message": "Resource with ID 999 not found",
  "data": null,
  "statusCode": 404
}
```

**Response Error - Clave duplicada (400 Bad Request):**
```json
{
  "success": false,
  "message": "Permission with key 'invoices.view' already exists",
  "data": null,
  "statusCode": 400
}
```

**Response Error - Sin autenticación (401 Unauthorized):**
```json
{
  "success": false,
  "message": "Unauthorized",
  "data": null,
  "statusCode": 401
}
```

---

### 2. Obtener Todos los Permisos

Obtiene una lista de todos los permisos con filtros opcionales.

**Endpoint:**
- **Producción:** `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission`
- **Local:** `GET http://localhost:5262/api/Permission`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Query Parameters (Todos opcionales):**

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `resourceId` | `integer` | Filtrar por ID de recurso | `?resourceId=1` |
| `actionId` | `integer` | Filtrar por ID de acción | `?actionId=2` |
| `isActive` | `boolean` | Filtrar por estado activo/inactivo | `?isActive=true` |
| `searchTerm` | `string` | Buscar en nombre, key o descripción | `?searchTerm=invoice` |

**Ejemplos de URLs (Producción):**

```
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?resourceId=1
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?actionId=2
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?isActive=true
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?searchTerm=invoice
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?resourceId=1&isActive=true
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?resourceId=1&actionId=2&isActive=true&searchTerm=view
```

**Response Success (200 OK):**
```json
{
  "success": true,
  "message": "Retrieved 5 permission(s)",
  "data": [
    {
      "permissionId": 1,
      "resourceId": 1,
      "resourceName": "Invoices",
      "resourceKey": "invoices",
      "actionId": 1,
      "actionName": "View",
      "actionKey": "view",
      "permissionName": "View Invoices",
      "permissionKey": "invoices.view",
      "description": "Allows viewing invoice records",
      "isActive": true,
      "createdAt": "2024-12-26T10:00:00Z",
      "updatedAt": null
    },
    {
      "permissionId": 2,
      "resourceId": 1,
      "resourceName": "Invoices",
      "resourceKey": "invoices",
      "actionId": 2,
      "actionName": "Create",
      "actionKey": "create",
      "permissionName": "Create Invoices",
      "permissionKey": "invoices.create",
      "description": "Allows creating new invoices",
      "isActive": true,
      "createdAt": "2024-12-26T10:05:00Z",
      "updatedAt": null
    },
    {
      "permissionId": 3,
      "resourceId": 1,
      "resourceName": "Invoices",
      "resourceKey": "invoices",
      "actionId": 3,
      "actionName": "Edit",
      "actionKey": "edit",
      "permissionName": "Edit Invoices",
      "permissionKey": "invoices.edit",
      "description": "Allows editing existing invoices",
      "isActive": true,
      "createdAt": "2024-12-26T10:10:00Z",
      "updatedAt": null
    },
    {
      "permissionId": 4,
      "resourceId": 1,
      "resourceName": "Invoices",
      "resourceKey": "invoices",
      "actionId": 4,
      "actionName": "Delete",
      "actionKey": "delete",
      "permissionName": "Delete Invoices",
      "permissionKey": "invoices.delete",
      "description": "Allows deleting invoices",
      "isActive": false,
      "createdAt": "2024-12-26T10:15:00Z",
      "updatedAt": "2024-12-26T11:00:00Z"
    },
    {
      "permissionId": 5,
      "resourceId": 2,
      "resourceName": "Customers",
      "resourceKey": "customers",
      "actionId": 1,
      "actionName": "View",
      "actionKey": "view",
      "permissionName": "View Customers",
      "permissionKey": "customers.view",
      "description": "Allows viewing customer records",
      "isActive": true,
      "createdAt": "2024-12-26T10:20:00Z",
      "updatedAt": null
    }
  ],
  "statusCode": 200
}
```

**Response cuando no hay resultados:**
```json
{
  "success": true,
  "message": "Retrieved 0 permission(s)",
  "data": [],
  "statusCode": 200
}
```

**Notas sobre los filtros:**

- Los filtros se pueden combinar (ej: `?resourceId=1&isActive=true`)
- `searchTerm` busca en: `permissionName`, `permissionKey`, y `description`
- La búsqueda es case-insensitive (no distingue mayúsculas/minúsculas)
- Los resultados se ordenan por: `resourceName` → `actionName`

---

### 3. Obtener Permiso por ID

Obtiene los detalles de un permiso específico.

**Endpoint:**
- **Producción:** `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/{permissionId}`
- **Local:** `GET http://localhost:5262/api/Permission/{permissionId}`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Path Parameters:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `permissionId` | `integer` | ✅ Sí | ID del permiso a obtener |

**Ejemplo (Producción):**
```
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/5
```

**Response Success (200 OK):**
```json
{
  "success": true,
  "message": "Permission retrieved successfully",
  "data": {
    "permissionId": 5,
    "resourceId": 2,
    "resourceName": "Customers",
    "resourceKey": "customers",
    "actionId": 3,
    "actionName": "Edit",
    "actionKey": "edit",
    "permissionName": "Edit Customers",
    "permissionKey": "customers.edit",
    "description": "Allows editing customer information",
    "isActive": true,
    "createdAt": "2024-12-26T10:15:00Z",
    "updatedAt": "2024-12-26T11:00:00Z"
  },
  "statusCode": 200
}
```

**Response Error - No encontrado (404 Not Found):**
```json
{
  "success": false,
  "message": "Permission with ID 999 not found",
  "data": null,
  "statusCode": 404
}
```

---

### 4. Actualizar Permiso

Actualiza un permiso existente. **IMPORTANTE:** Solo se pueden actualizar `permissionName`, `description` e `isActive`. Los campos `resourceId`, `actionId` y `permissionKey` NO se pueden cambiar después de la creación.

**Endpoint:**
- **Producción:** `PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/{permissionId}`
- **Local:** `PUT http://localhost:5262/api/Permission/{permissionId}`

**Headers:**
```
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Path Parameters:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `permissionId` | `integer` | ✅ Sí | ID del permiso a actualizar |

**Request Body (JSON):**
```json
{
  "permissionName": "View All Invoices",
  "description": "Allows viewing all invoice records including archived ones",
  "isActive": true
}
```

**Parámetros del Body:**

| Campo | Tipo | Requerido | Descripción | Ejemplo |
|-------|------|-----------|-------------|---------|
| `permissionName` | `string` | ✅ Sí | Nuevo nombre del permiso | `"View All Invoices"` |
| `description` | `string` | ❌ No | Nueva descripción | `"Allows viewing all invoice records"` |
| `isActive` | `boolean` | ✅ Sí | Estado activo/inactivo | `true` |

**Validaciones:**

- ✅ `permissionName` es obligatorio, máximo 200 caracteres
- ✅ `description` es opcional, máximo 500 caracteres
- ✅ `isActive` es obligatorio (debe ser `true` o `false`)

**Response Success (200 OK):**
```json
{
  "success": true,
  "message": "Permission updated successfully",
  "data": {
    "permissionId": 5,
    "permissionName": "View All Invoices",
    "message": "Permission updated successfully"
  },
  "statusCode": 200
}
```

**Response Error - No encontrado (404 Not Found):**
```json
{
  "success": false,
  "message": "Permission with ID 999 not found",
  "data": null,
  "statusCode": 404
}
```

**Response Error - Validación (400 Bad Request):**
```json
{
  "success": false,
  "message": "Permission name is required, Permission name cannot exceed 200 characters",
  "data": null,
  "statusCode": 400
}
```

**⚠️ Campos Inmutables:**

Los siguientes campos **NO se pueden cambiar** después de crear el permiso:
- `resourceId`
- `actionId`
- `permissionKey`

Si necesitas cambiar estos campos, debes:
1. Crear un nuevo permiso con los valores correctos
2. Asignar el nuevo permiso a los roles/usuarios que lo necesiten
3. Eliminar el permiso antiguo (si ya no se usa)

---

### 5. Eliminar Permiso

Elimina un permiso del sistema. Realiza un **soft delete** (marca como inactivo) en lugar de eliminar físicamente el registro.

**Endpoint:**
- **Producción:** `DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/{permissionId}`
- **Local:** `DELETE http://localhost:5262/api/Permission/{permissionId}`

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Path Parameters:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `permissionId` | `integer` | ✅ Sí | ID del permiso a eliminar |

**Ejemplo (Producción):**
```
DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/5
```

**Condiciones para Eliminar:**

Un permiso **NO puede ser eliminado** si:
- ❌ Está asignado a uno o más **roles** (tabla `RolePermissions`)
- ❌ Está asignado a uno o más **usuarios** (tabla `UserPermissions` con `IsActive = true`)

**Pasos antes de eliminar:**
1. Verificar si el permiso está asignado a algún rol usando: `GET /api/RBAC/roles/{roleId}/permissions`
2. Verificar si el permiso está asignado a algún usuario usando: `GET /api/RBAC/users/{userId}/permissions`
3. Remover el permiso de todos los roles y usuarios
4. Luego intentar eliminar el permiso

**Response Success (200 OK):**
```json
{
  "success": true,
  "message": "Permission deleted successfully",
  "data": {
    "permissionId": 5,
    "message": "Permission deleted successfully"
  },
  "statusCode": 200
}
```

**Response Error - No encontrado (404 Not Found):**
```json
{
  "success": false,
  "message": "Permission with ID 999 not found",
  "data": null,
  "statusCode": 404
}
```

**Response Error - Asignado a roles (400 Bad Request):**
```json
{
  "success": false,
  "message": "Cannot delete permission 'View Invoices' because it is assigned to one or more roles. Please remove it from all roles first.",
  "data": null,
  "statusCode": 400
}
```

**Response Error - Asignado a usuarios (400 Bad Request):**
```json
{
  "success": false,
  "message": "Cannot delete permission 'View Invoices' because it is assigned to one or more users. Please remove it from all users first.",
  "data": null,
  "statusCode": 400
}
```

**Nota sobre Soft Delete:**

El permiso no se elimina físicamente de la base de datos. En su lugar:
- Se marca `IsActive = false`
- Se actualiza `UpdatedAt` con la fecha/hora actual
- El permiso deja de estar disponible para asignar a nuevos roles/usuarios
- Los registros históricos se mantienen para auditoría

---

## 📝 Convenciones de Nomenclatura

### Permission Key (Recomendado)

**Formato:** `{resource}.{action}` (en minúsculas, separado por punto)

**Ejemplos:**
- `invoices.view` - Ver facturas
- `invoices.create` - Crear facturas
- `invoices.edit` - Editar facturas
- `invoices.delete` - Eliminar facturas
- `customers.view` - Ver clientes
- `customers.edit` - Editar clientes
- `reports.export` - Exportar reportes
- `users.manage` - Gestionar usuarios

**Reglas:**
- ✅ Solo letras minúsculas
- ✅ Números permitidos
- ✅ Guiones bajos (`_`), puntos (`.`) y guiones (`-`) permitidos
- ❌ No espacios
- ❌ No caracteres especiales

### Permission Name (Recomendado)

**Formato:** `{Action} {Resource}` (en formato legible)

**Ejemplos:**
- `"View Invoices"` (para `invoices.view`)
- `"Create Invoices"` (para `invoices.create`)
- `"Edit Customers"` (para `customers.edit`)
- `"Delete Reports"` (para `reports.delete`)

---

## 🎯 Casos de Uso Comunes

### Caso 1: Crear Permisos para el Módulo de Facturas

```bash
# 1. Ver facturas
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission
{
  "resourceId": 1,
  "actionId": 1,
  "permissionName": "View Invoices",
  "permissionKey": "invoices.view",
  "description": "Allows viewing invoice records"
}

# 2. Crear facturas
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission
{
  "resourceId": 1,
  "actionId": 2,
  "permissionName": "Create Invoices",
  "permissionKey": "invoices.create",
  "description": "Allows creating new invoices"
}

# 3. Editar facturas
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission
{
  "resourceId": 1,
  "actionId": 3,
  "permissionName": "Edit Invoices",
  "permissionKey": "invoices.edit",
  "description": "Allows editing existing invoices"
}

# 4. Eliminar facturas
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission
{
  "resourceId": 1,
  "actionId": 4,
  "permissionName": "Delete Invoices",
  "permissionKey": "invoices.delete",
  "description": "Allows deleting invoices"
}
```

### Caso 2: Obtener Solo Permisos Activos

```bash
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?isActive=true
```

### Caso 3: Buscar Permisos por Término

```bash
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?searchTerm=invoice
```

Esto buscará en:
- `permissionName`: "View Invoices", "Create Invoices", etc.
- `permissionKey`: "invoices.view", "invoices.create", etc.
- `description`: Cualquier descripción que contenga "invoice"

### Caso 4: Obtener Permisos de un Recurso Específico

```bash
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?resourceId=1
```

Esto devuelve todos los permisos relacionados con el recurso ID 1 (ej: todas las acciones de "Invoices").

### Caso 5: Desactivar un Permiso Temporalmente

```bash
PUT https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/5
{
  "permissionName": "View Invoices",
  "description": "Allows viewing invoice records",
  "isActive": false
}
```

Esto desactiva el permiso sin eliminarlo, permitiendo reactivarlo más tarde.

---

## 🔄 Flujo de Trabajo Completo

### Paso 1: Verificar Recursos y Acciones Disponibles

Antes de crear permisos, necesitas conocer los IDs de los recursos y acciones:

```bash
# Obtener recursos
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/resources

# Obtener acciones
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/actions
```

### Paso 2: Crear los Permisos

```bash
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission
{
  "resourceId": 1,
  "actionId": 1,
  "permissionName": "View Invoices",
  "permissionKey": "invoices.view",
  "description": "Allows viewing invoice records"
}
```

### Paso 3: Asignar Permisos a Roles

Una vez creados los permisos, puedes asignarlos a roles:

```bash
POST https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/roles/{roleId}/permissions
{
  "permissionIds": [1, 2, 3, 4]
}
```

### Paso 4: Verificar Permisos Asignados

```bash
GET https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/roles/{roleId}/permissions
```

---

## ⚠️ Errores Comunes y Soluciones

### Error: "Permission with key 'invoices.view' already exists"

**Causa:** Intentaste crear un permiso con una clave que ya existe.

**Solución:** 
- Usa una clave diferente
- O verifica si el permiso ya existe: `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?searchTerm=invoices.view`

### Error: "Resource with ID 999 not found"

**Causa:** El `resourceId` que proporcionaste no existe.

**Solución:**
- Verifica los recursos disponibles: `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/resources`
- Usa un `resourceId` válido

### Error: "Cannot delete permission because it is assigned to one or more roles"

**Causa:** El permiso está asignado a uno o más roles.

**Solución:**
1. Obtén los roles que tienen este permiso
2. Remueve el permiso de cada rol: `DELETE https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/roles/{roleId}/permissions/{permissionId}`
3. Luego intenta eliminar el permiso nuevamente

### Error: "Permission name cannot exceed 200 characters"

**Causa:** El nombre del permiso es demasiado largo.

**Solución:** Acorta el nombre a máximo 200 caracteres.

---

## 📊 Estructura de Datos

### Permission Entity

```sql
CREATE TABLE [Global].[Permissions] (
    [PermissionId] INT PRIMARY KEY IDENTITY(1,1),
    [ResourceId] INT NOT NULL,
    [ActionId] INT NOT NULL,
    [PermissionName] NVARCHAR(200) NOT NULL,
    [PermissionKey] NVARCHAR(100) NOT NULL UNIQUE,
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME NULL,
    FOREIGN KEY ([ResourceId]) REFERENCES [Global].[Resources]([ResourceId]),
    FOREIGN KEY ([ActionId]) REFERENCES [Global].[Actions]([ActionId])
);
```

### Relaciones

- **Permission** → **Resource** (Muchos a Uno)
- **Permission** → **Action** (Muchos a Uno)
- **Permission** → **RolePermission** (Uno a Muchos)
- **Permission** → **UserPermission** (Uno a Muchos)

---

## 🧪 Ejemplos de Integración (JavaScript/TypeScript)

### Crear Permiso

```typescript
async function createPermission() {
  const response = await fetch('https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${getToken()}`
    },
    body: JSON.stringify({
      resourceId: 1,
      actionId: 1,
      permissionName: "View Invoices",
      permissionKey: "invoices.view",
      description: "Allows viewing invoice records"
    })
  });

  const result = await response.json();
  
  if (result.success) {
    console.log('Permiso creado:', result.data);
  } else {
    console.error('Error:', result.message);
  }
}
```

### Obtener Todos los Permisos con Filtros

```typescript
async function getPermissions(filters?: {
  resourceId?: number;
  actionId?: number;
  isActive?: boolean;
  searchTerm?: string;
}) {
  const params = new URLSearchParams();
  if (filters?.resourceId) params.append('resourceId', filters.resourceId.toString());
  if (filters?.actionId) params.append('actionId', filters.actionId.toString());
  if (filters?.isActive !== undefined) params.append('isActive', filters.isActive.toString());
  if (filters?.searchTerm) params.append('searchTerm', filters.searchTerm);

  const response = await fetch(`https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission?${params}`, {
    headers: {
      'Authorization': `Bearer ${getToken()}`
    }
  });

  const result = await response.json();
  return result.data; // Array de permisos
}
```

### Actualizar Permiso

```typescript
async function updatePermission(permissionId: number, data: {
  permissionName: string;
  description?: string;
  isActive: boolean;
}) {
  const response = await fetch(`https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/${permissionId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${getToken()}`
    },
    body: JSON.stringify(data)
  });

  const result = await response.json();
  return result;
}
```

### Eliminar Permiso

```typescript
async function deletePermission(permissionId: number) {
  const response = await fetch(`https://vh-apimanagement.azure-api.net/corporate-vh/api/Permission/${permissionId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${getToken()}`
    }
  });

  const result = await response.json();
  return result;
}
```

---

## 📚 APIs Relacionadas

Para gestionar completamente el sistema RBAC, también necesitarás estas APIs:

- **Resources:** `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/resources` - Gestionar recursos
- **Actions:** `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/actions` - Gestionar acciones
- **Roles:** `GET https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/roles` - Gestionar roles
- **Role Permissions:** `POST https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/roles/{roleId}/permissions` - Asignar permisos a roles
- **User Permissions:** `POST https://vh-apimanagement.azure-api.net/corporate-vh/api/RBAC/users/{userId}/permissions` - Asignar permisos a usuarios

---

## ✅ Resumen

### ¿Qué son los Permisos?

Los permisos son la **combinación de un Recurso (qué módulo) + una Acción (qué operación)**. Ejemplo: `invoices.view` = Ver el módulo de Facturas.

### ¿Para qué sirven?

1. **Control de Acceso Granular**: Definir exactamente qué puede hacer cada usuario
2. **Asignación a Roles**: Agrupar permisos en roles para facilitar la gestión
3. **Escalabilidad**: Agregar nuevos permisos fácilmente cuando se crean nuevos módulos
4. **Auditoría**: Mantener un registro de todos los permisos del sistema

### ¿Cómo funcionan?

1. **Crear Permisos**: Define los permisos que necesitas (ej: `invoices.view`, `invoices.create`)
2. **Asignar a Roles**: Agrupa permisos en roles (ej: "Administrador", "Editor", "Visualizador")
3. **Asignar Roles a Usuarios**: Los usuarios heredan los permisos de sus roles
4. **Verificar en el Frontend**: Antes de mostrar una funcionalidad, verifica si el usuario tiene el permiso necesario

### Ventajas

- ✅ **Flexibilidad**: Cambiar permisos sin modificar código
- ✅ **Centralización**: Todo el control de acceso en un solo lugar
- ✅ **Mantenibilidad**: Fácil de entender y gestionar
- ✅ **Escalabilidad**: Agregar nuevos módulos sin problemas

---

## 🆘 Soporte

Si tienes dudas o problemas con estas APIs, contacta al equipo de desarrollo.

**Última actualización:** Diciembre 2024

