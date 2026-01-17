# Permission Management API Guide

## Overview

Este documento describe las APIs CRUD para gestionar **Permisos (Permissions)** en el sistema RBAC.

Los permisos son la combinación de un **Recurso (Resource)** + **Acción (Action)** y se utilizan para controlar el acceso a funcionalidades específicas del sistema.

---

## Base URL

```
http://localhost:5262/api/Permission
```

En producción:
```
https://your-api-domain.com/api/Permission
```

---

## Endpoints

### 1. Create Permission

Crea un nuevo permiso en el sistema.

**Endpoint:** `POST /api/Permission`

**Request Body:**
```json
{
  "resourceId": 1,
  "actionId": 2,
  "permissionName": "View Invoices",
  "permissionKey": "invoices.view",
  "description": "Allows viewing invoice records"
}
```

**Response (200 OK):**
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

**Validation Rules:**
- `resourceId`: Required, must be > 0
- `actionId`: Required, must be > 0
- `permissionName`: Required, max 200 characters
- `permissionKey`: Required, max 100 characters, only letters, numbers, underscores, dots, and hyphens
- `description`: Optional, max 500 characters

**Permission Key Format:**
- Recommended format: `resource.action`
- Examples: `invoices.create`, `customers.edit`, `reports.view`

---

### 2. Get All Permissions

Obtiene todos los permisos con filtros opcionales.

**Endpoint:** `GET /api/Permission`

**Query Parameters:**
- `resourceId` (optional): Filter by resource ID
- `actionId` (optional): Filter by action ID
- `isActive` (optional): Filter by active status (true/false)
- `searchTerm` (optional): Search in permission name, key, or description

**Examples:**
```
GET /api/Permission
GET /api/Permission?resourceId=1
GET /api/Permission?actionId=2
GET /api/Permission?isActive=true
GET /api/Permission?searchTerm=invoice
GET /api/Permission?resourceId=1&isActive=true
```

**Response (200 OK):**
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
    }
  ],
  "statusCode": 200
}
```

---

### 3. Get Permission by ID

Obtiene un permiso específico por su ID.

**Endpoint:** `GET /api/Permission/{permissionId}`

**Example:**
```
GET /api/Permission/5
```

**Response (200 OK):**
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

**Response (404 Not Found):**
```json
{
  "success": false,
  "message": "Permission with ID 999 not found",
  "data": null,
  "statusCode": 404
}
```

---

### 4. Update Permission

Actualiza un permiso existente.

**Endpoint:** `PUT /api/Permission/{permissionId}`

**Request Body:**
```json
{
  "permissionName": "View All Invoices",
  "description": "Allows viewing all invoice records including archived",
  "isActive": true
}
```

**Response (200 OK):**
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

**Note:** `resourceId`, `actionId`, and `permissionKey` **cannot be changed** after creation. Only `permissionName`, `description`, and `isActive` can be updated.

---

### 5. Delete Permission

Elimina un permiso (soft delete - marca como inactivo).

**Endpoint:** `DELETE /api/Permission/{permissionId}`

**Example:**
```
DELETE /api/Permission/5
```

**Response (200 OK):**
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

**Response (400 Bad Request) - Permission is assigned:**
```json
{
  "success": false,
  "message": "Cannot delete permission 'View Invoices' because it is assigned to one or more roles. Please remove it from all roles first.",
  "data": null,
  "statusCode": 400
}
```

**Important:** A permission cannot be deleted if:
- It's assigned to any role
- It's assigned to any user

You must remove the permission from all roles and users before deleting it.

---

## Error Responses

### Validation Error (400)
```json
{
  "success": false,
  "message": "Permission key is required, Permission key can only contain letters, numbers, underscores, dots and hyphens",
  "data": null,
  "statusCode": 400
}
```

### Not Found (404)
```json
{
  "success": false,
  "message": "Resource with ID 999 not found",
  "data": null,
  "statusCode": 404
}
```

### Server Error (500)
```json
{
  "success": false,
  "message": "Error creating permission: [error details]",
  "data": null,
  "statusCode": 500
}
```

---

## Authentication

Todas las APIs requieren autenticación mediante JWT token.

**Header:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

---

## Common Use Cases

### 1. Create Permissions for Invoice Module

```bash
# View invoices
POST /api/Permission
{
  "resourceId": 1,
  "actionId": 1,
  "permissionName": "View Invoices",
  "permissionKey": "invoices.view",
  "description": "Allows viewing invoice records"
}

# Create invoices
POST /api/Permission
{
  "resourceId": 1,
  "actionId": 2,
  "permissionName": "Create Invoices",
  "permissionKey": "invoices.create",
  "description": "Allows creating new invoices"
}

# Edit invoices
POST /api/Permission
{
  "resourceId": 1,
  "actionId": 3,
  "permissionName": "Edit Invoices",
  "permissionKey": "invoices.edit",
  "description": "Allows editing existing invoices"
}

# Delete invoices
POST /api/Permission
{
  "resourceId": 1,
  "actionId": 4,
  "permissionName": "Delete Invoices",
  "permissionKey": "invoices.delete",
  "description": "Allows deleting invoices"
}
```

### 2. Get All Active Permissions

```bash
GET /api/Permission?isActive=true
```

### 3. Search Permissions

```bash
GET /api/Permission?searchTerm=invoice
```

### 4. Get Permissions for a Specific Resource

```bash
GET /api/Permission?resourceId=1
```

### 5. Deactivate a Permission

```bash
PUT /api/Permission/5
{
  "permissionName": "View Invoices",
  "description": "Allows viewing invoice records",
  "isActive": false
}
```

---

## Frontend Integration

### React/TypeScript Example

```typescript
// types.ts
export interface CreatePermissionRequest {
  resourceId: number;
  actionId: number;
  permissionName: string;
  permissionKey: string;
  description?: string;
}

export interface Permission {
  permissionId: number;
  resourceId: number;
  resourceName: string;
  resourceKey: string;
  actionId: number;
  actionName: string;
  actionKey: string;
  permissionName: string;
  permissionKey: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// api.ts
const API_BASE_URL = 'https://your-api-domain.com/api';

export const permissionApi = {
  // Create permission
  create: async (data: CreatePermissionRequest) => {
    const response = await fetch(`${API_BASE_URL}/Permission`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`
      },
      body: JSON.stringify(data)
    });
    return response.json();
  },

  // Get all permissions
  getAll: async (filters?: {
    resourceId?: number;
    actionId?: number;
    isActive?: boolean;
    searchTerm?: string;
  }) => {
    const params = new URLSearchParams();
    if (filters?.resourceId) params.append('resourceId', filters.resourceId.toString());
    if (filters?.actionId) params.append('actionId', filters.actionId.toString());
    if (filters?.isActive !== undefined) params.append('isActive', filters.isActive.toString());
    if (filters?.searchTerm) params.append('searchTerm', filters.searchTerm);

    const response = await fetch(`${API_BASE_URL}/Permission?${params}`, {
      headers: {
        'Authorization': `Bearer ${getToken()}`
      }
    });
    return response.json();
  },

  // Get permission by ID
  getById: async (permissionId: number) => {
    const response = await fetch(`${API_BASE_URL}/Permission/${permissionId}`, {
      headers: {
        'Authorization': `Bearer ${getToken()}`
      }
    });
    return response.json();
  },

  // Update permission
  update: async (permissionId: number, data: {
    permissionName: string;
    description?: string;
    isActive: boolean;
  }) => {
    const response = await fetch(`${API_BASE_URL}/Permission/${permissionId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`
      },
      body: JSON.stringify(data)
    });
    return response.json();
  },

  // Delete permission
  delete: async (permissionId: number) => {
    const response = await fetch(`${API_BASE_URL}/Permission/${permissionId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${getToken()}`
      }
    });
    return response.json();
  }
};

function getToken(): string {
  return localStorage.getItem('jwt_token') || '';
}
```

---

## Database Schema

### Permissions Table

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

---

## Notes

1. **Permission Key Uniqueness:** Each `permissionKey` must be unique across the system.

2. **Immutable Fields:** Once created, `resourceId`, `actionId`, and `permissionKey` cannot be changed.

3. **Soft Delete:** Deleting a permission sets `IsActive = false` instead of removing the record.

4. **Cascading Restrictions:** A permission cannot be deleted if it's assigned to any role or user.

5. **Recommended Naming Convention:**
   - Permission Key: `resource.action` (lowercase, dot-separated)
   - Permission Name: "Action Resource" (human-readable)
   - Examples:
     - Key: `invoices.view`, Name: "View Invoices"
     - Key: `customers.edit`, Name: "Edit Customers"
     - Key: `reports.export`, Name: "Export Reports"

---

## Related APIs

- **Resources API:** `/api/RBAC/resources` - Manage resources
- **Actions API:** `/api/RBAC/actions` - Manage actions
- **Roles API:** `/api/RBAC/roles` - Manage roles
- **Role Permissions API:** `/api/RBAC/roles/{roleId}/permissions` - Assign permissions to roles
- **User Permissions API:** `/api/RBAC/users/{userId}/permissions` - Assign permissions to users

---

## Support

For questions or issues, contact the development team.


