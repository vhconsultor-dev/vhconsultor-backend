# Documentación Backend - Sistema RBAC (Roles y Permisos)

## Índice
1. [Introducción](#introducción)
2. [Conceptos del Sistema RBAC](#conceptos-del-sistema-rbac)
3. [Flujos de Trabajo Comunes](#flujos-de-trabajo-comunes)
4. [Endpoints Disponibles](#endpoints-disponibles)
5. [Estructura de Datos](#estructura-de-datos)
6. [Ejemplos de Uso](#ejemplos-de-uso)

---

## Introducción

El sistema RBAC (Role-Based Access Control) permite gestionar permisos de manera escalable y flexible. Todos los endpoints requieren autenticación JWT mediante el header `Authorization: Bearer {token}`.

**Base URL:** `/api/RBAC`

**Autenticación:** Todos los endpoints requieren JWT válido (excepto que se indique lo contrario)

---

## Conceptos del Sistema RBAC

### Jerarquía del Sistema

1. **Resources (Recursos)**: Módulos o recursos del sistema (ej: Customers, Contracts, Users)
2. **Actions (Acciones)**: Acciones que se pueden realizar (ej: View, Create, Edit, Delete)
3. **Permissions (Permisos)**: Combinación de Resource + Action (ej: customers.view, contracts.create)
4. **Roles (Roles)**: Conjuntos de permisos agrupados (ej: Admin, Manager, Viewer)
5. **UserRoles**: Asignación de roles a usuarios
6. **UserPermissions**: Permisos directos a usuarios (casos especiales)
7. **UserPermissionDenials**: Denegaciones específicas de permisos a usuarios

### Cálculo de Permisos Efectivos

Los permisos efectivos de un usuario se calculan así:
```
Permisos Efectivos = (Permisos de Roles asignados) + (Permisos Directos) - (Denegaciones)
```

**Reglas importantes:**
- Un usuario puede tener múltiples roles
- Los permisos directos se suman a los permisos de roles
- Las denegaciones siempre tienen prioridad (si un permiso está denegado, no se otorga aunque esté en un rol)
- Los roles y permisos con `ExpiresAt` se consideran activos solo si la fecha no ha expirado
- Solo se consideran registros con `IsActive = true`

---

## Flujos de Trabajo Comunes

### Flujo 1: Configurar Permisos para un Rol

**Paso 1:** Crear o verificar que existan los Resources necesarios
```
POST /api/RBAC/resources
```

**Paso 2:** Crear o verificar que existan las Actions necesarias
```
POST /api/RBAC/actions
```

**Paso 3:** Crear los Permissions (combinación de Resource + Action)
```
POST /api/RBAC/permissions
```

**Paso 4:** Crear el Rol
```
POST /api/RBAC/roles
```

**Paso 5:** Asignar Permisos al Rol
```
POST /api/RBAC/role-permissions
```

### Flujo 2: Asignar Rol a Usuario

**Paso 1:** Verificar que el rol existe
```
GET /api/RBAC/roles?roleId={roleId}
```

**Paso 2:** Asignar el rol al usuario
```
POST /api/RBAC/user-roles
```

**Paso 3:** Verificar roles asignados al usuario
```
GET /api/RBAC/user-roles?userId={userId}
```

### Flujo 3: Otorgar Permiso Directo a Usuario

**Paso 1:** Verificar que el permiso existe
```
GET /api/RBAC/permissions?permissionId={permissionId}
```

**Paso 2:** Otorgar permiso directo al usuario
```
POST /api/RBAC/user-permissions
```

### Flujo 4: Consultar Permisos Efectivos de un Usuario

**Paso único:** Obtener todos los permisos efectivos (calculados automáticamente)
```
GET /api/RBAC/user-effective-permissions/{userId}
```

Este endpoint calcula automáticamente:
- Permisos de todos los roles asignados al usuario
- Permisos directos otorgados
- Excluye permisos denegados
- Considera fechas de expiración

---

## Endpoints Disponibles

### GET - Consultas (Todos con filtros opcionales)

#### 1. GET /api/RBAC/resources

Obtiene recursos con filtros opcionales.

**Query Parameters (todos opcionales):**
- `resourceId`: int - ID del recurso específico
- `resourceName`: string - Búsqueda parcial por nombre
- `resourceKey`: string - Búsqueda exacta por clave
- `module`: string - Filtrar por módulo
- `isActive`: bool - Filtrar por estado activo (por defecto true)

**Ejemplo:**
```
GET /api/RBAC/resources
GET /api/RBAC/resources?resourceId=1
GET /api/RBAC/resources?module=Corporate&isActive=true
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "resourceId": 1,
      "resourceName": "Customers",
      "resourceKey": "customers",
      "description": "Gestión de clientes corporativos",
      "module": "Corporate",
      "isActive": true,
      "createdAt": "2024-12-20T10:00:00",
      "updatedAt": null
    }
  ],
  "message": "1 recurso(s) encontrado(s)"
}
```

---

#### 2. GET /api/RBAC/actions

Obtiene acciones con filtros opcionales.

**Query Parameters (todos opcionales):**
- `actionId`: int - ID de la acción específica
- `actionName`: string - Búsqueda parcial por nombre
- `actionKey`: string - Búsqueda exacta por clave
- `isActive`: bool - Filtrar por estado activo (por defecto true)

**Ejemplo:**
```
GET /api/RBAC/actions
GET /api/RBAC/actions?actionKey=view
GET /api/RBAC/actions?actionName=create
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "actionId": 1,
      "actionName": "View",
      "actionKey": "view",
      "description": "Ver/Consultar",
      "isActive": true,
      "createdAt": "2024-12-20T10:00:00"
    }
  ],
  "message": "1 acción(es) encontrada(s)"
}
```

---

#### 3. GET /api/RBAC/permissions

Obtiene permisos con filtros opcionales.

**Query Parameters (todos opcionales):**
- `permissionId`: int - ID del permiso específico
- `resourceId`: int - Filtrar por recurso
- `actionId`: int - Filtrar por acción
- `permissionKey`: string - Búsqueda exacta por clave
- `isActive`: bool - Filtrar por estado activo (por defecto true)

**Ejemplo:**
```
GET /api/RBAC/permissions
GET /api/RBAC/permissions?resourceId=1&actionId=1
GET /api/RBAC/permissions?permissionKey=customers.view
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "permissionId": 1,
      "resourceId": 1,
      "actionId": 1,
      "permissionName": "Customers - View",
      "permissionKey": "customers.view",
      "description": "Permite ver clientes",
      "isActive": true,
      "createdAt": "2024-12-20T10:00:00",
      "updatedAt": null
    }
  ],
  "message": "1 permiso(s) encontrado(s)"
}
```

---

#### 4. GET /api/RBAC/roles

Obtiene roles con filtros opcionales.

**Query Parameters (todos opcionales):**
- `roleId`: int - ID del rol específico
- `roleName`: string - Búsqueda parcial por nombre
- `roleKey`: string - Búsqueda exacta por clave
- `isSystemRole`: bool - Filtrar por rol del sistema
- `isActive`: bool - Filtrar por estado activo (por defecto true)

**Ejemplo:**
```
GET /api/RBAC/roles
GET /api/RBAC/roles?roleId=1
GET /api/RBAC/roles?isSystemRole=false&isActive=true
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "roleId": 1,
      "roleName": "Administrator",
      "roleKey": "admin",
      "description": "Rol de administrador con todos los permisos",
      "isSystemRole": true,
      "isActive": true,
      "createdAt": "2024-12-20T10:00:00",
      "updatedAt": null
    }
  ],
  "message": "1 rol(es) encontrado(s)"
}
```

---

#### 5. GET /api/RBAC/role-permissions

Obtiene permisos asignados a roles.

**Query Parameters (todos opcionales):**
- `roleId`: int - Filtrar por rol
- `permissionId`: int - Filtrar por permiso

**Ejemplo:**
```
GET /api/RBAC/role-permissions?roleId=1
GET /api/RBAC/role-permissions?permissionId=5
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "rolePermissionId": 1,
      "roleId": 1,
      "permissionId": 5,
      "grantedBy": 1,
      "grantedAt": "2024-12-20T10:00:00"
    }
  ],
  "message": "1 permiso(s) asignado(s) encontrado(s)"
}
```

---

#### 6. GET /api/RBAC/user-roles

Obtiene roles asignados a usuarios.

**Query Parameters (todos opcionales):**
- `userId`: int - Filtrar por usuario
- `roleId`: int - Filtrar por rol
- `isActive`: bool - Filtrar por estado activo (por defecto true)

**Ejemplo:**
```
GET /api/RBAC/user-roles?userId=1
GET /api/RBAC/user-roles?roleId=2&isActive=true
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "userRoleId": 1,
      "userId": 1,
      "roleId": 2,
      "assignedBy": 1,
      "assignedAt": "2024-12-20T10:00:00",
      "expiresAt": null,
      "isActive": true
    }
  ],
  "message": "1 rol(es) asignado(s) encontrado(s)"
}
```

---

#### 7. GET /api/RBAC/user-permissions

Obtiene permisos directos otorgados a usuarios.

**Query Parameters (todos opcionales):**
- `userId`: int - Filtrar por usuario
- `permissionId`: int - Filtrar por permiso
- `isActive`: bool - Filtrar por estado activo (por defecto true)

**Ejemplo:**
```
GET /api/RBAC/user-permissions?userId=1
GET /api/RBAC/user-permissions?permissionId=5&isActive=true
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "userPermissionId": 1,
      "userId": 1,
      "permissionId": 5,
      "grantedBy": 1,
      "grantedAt": "2024-12-20T10:00:00",
      "expiresAt": null,
      "isActive": true
    }
  ],
  "message": "1 permiso(s) directo(s) encontrado(s)"
}
```

---

#### 8. GET /api/RBAC/user-permission-denials

Obtiene denegaciones de permisos a usuarios.

**Query Parameters (todos opcionales):**
- `userId`: int - Filtrar por usuario
- `permissionId`: int - Filtrar por permiso
- `isActive`: bool - Filtrar por estado activo (por defecto true)

**Ejemplo:**
```
GET /api/RBAC/user-permission-denials?userId=1
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "userPermissionDenialId": 1,
      "userId": 1,
      "permissionId": 5,
      "deniedBy": 1,
      "deniedAt": "2024-12-20T10:00:00",
      "reason": "Restricción temporal por política de seguridad",
      "isActive": true
    }
  ],
  "message": "1 denegación(es) encontrada(s)"
}
```

---

#### 9. GET /api/RBAC/user-effective-permissions/{userId}

Obtiene todos los permisos efectivos de un usuario (calculados automáticamente).

**Path Parameter:**
- `userId`: int - ID del usuario (requerido)

**Ejemplo:**
```
GET /api/RBAC/user-effective-permissions/1
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": [
    {
      "permissionId": 1,
      "resourceId": 1,
      "actionId": 1,
      "permissionName": "Customers - View",
      "permissionKey": "customers.view",
      "description": "Permite ver clientes",
      "isActive": true,
      "createdAt": "2024-12-20T10:00:00",
      "updatedAt": null
    }
  ],
  "message": "5 permiso(s) efectivo(s) encontrado(s)"
}
```

**Nota:** Este endpoint calcula automáticamente:
- Permisos de todos los roles activos del usuario
- Permisos directos activos del usuario
- Excluye permisos denegados
- Considera fechas de expiración

---

### POST - Creación y Asignación

#### 10. POST /api/RBAC/resources

Crea un nuevo recurso.

**Request Body:**
```json
{
  "resourceName": "Customers",
  "resourceKey": "customers",
  "description": "Gestión de clientes corporativos",
  "module": "Corporate"
}
```

**Campos requeridos:**
- `resourceName`: string (máximo 100 caracteres)
- `resourceKey`: string (máximo 50 caracteres, debe ser único)

**Campos opcionales:**
- `description`: string (máximo 500 caracteres)
- `module`: string (máximo 50 caracteres)

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "resourceId": 1
  },
  "message": "Recurso creado exitosamente"
}
```

---

#### 11. POST /api/RBAC/actions

Crea una nueva acción.

**Request Body:**
```json
{
  "actionName": "View",
  "actionKey": "view",
  "description": "Ver/Consultar"
}
```

**Campos requeridos:**
- `actionName`: string (máximo 100 caracteres)
- `actionKey`: string (máximo 50 caracteres, debe ser único)

**Campos opcionales:**
- `description`: string (máximo 500 caracteres)

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "actionId": 1
  },
  "message": "Acción creada exitosamente"
}
```

---

#### 12. POST /api/RBAC/permissions

Crea un nuevo permiso (combinación de Resource + Action).

**Request Body:**
```json
{
  "resourceId": 1,
  "actionId": 1,
  "permissionName": "Customers - View",
  "permissionKey": "customers.view",
  "description": "Permite ver clientes"
}
```

**Campos requeridos:**
- `resourceId`: int
- `actionId`: int
- `permissionName`: string (máximo 200 caracteres)
- `permissionKey`: string (máximo 100 caracteres, debe ser único)

**Campos opcionales:**
- `description`: string (máximo 500 caracteres)

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "permissionId": 1
  },
  "message": "Permiso creado exitosamente"
}
```

---

#### 13. POST /api/RBAC/roles

Crea un nuevo rol.

**Request Body:**
```json
{
  "roleName": "Manager",
  "roleKey": "manager",
  "description": "Rol de gerente con permisos de gestión",
  "isSystemRole": false
}
```

**Campos requeridos:**
- `roleName`: string (máximo 100 caracteres)
- `roleKey`: string (máximo 50 caracteres, debe ser único)

**Campos opcionales:**
- `description`: string (máximo 500 caracteres)
- `isSystemRole`: bool (por defecto false)

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "roleId": 1
  },
  "message": "Rol creado exitosamente"
}
```

---

#### 14. POST /api/RBAC/role-permissions

Asigna un permiso a un rol.

**Request Body:**
```json
{
  "roleId": 1,
  "permissionId": 5,
  "grantedBy": 1
}
```

**Campos requeridos:**
- `roleId`: int
- `permissionId`: int

**Campos opcionales:**
- `grantedBy`: int - ID del usuario que otorga el permiso

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "rolePermissionId": 1
  },
  "message": "Permiso asignado al rol exitosamente"
}
```

**Nota:** Si el permiso ya está asignado al rol, puede generar error de duplicado.

---

#### 15. POST /api/RBAC/user-roles

Asigna un rol a un usuario.

**Request Body:**
```json
{
  "userId": 1,
  "roleId": 2,
  "assignedBy": 1,
  "expiresAt": "2025-12-31T23:59:59"
}
```

**Campos requeridos:**
- `userId`: int
- `roleId`: int

**Campos opcionales:**
- `assignedBy`: int - ID del usuario que asigna el rol
- `expiresAt`: DateTime - Fecha de expiración del rol (si es null, no expira)

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "userRoleId": 1
  },
  "message": "Rol asignado al usuario exitosamente"
}
```

**Nota:** Un usuario puede tener múltiples roles. Si el rol ya está asignado y activo, puede generar error de duplicado.

---

#### 16. POST /api/RBAC/user-permissions

Otorga un permiso directo a un usuario (sin pasar por roles).

**Request Body:**
```json
{
  "userId": 1,
  "permissionId": 5,
  "grantedBy": 1,
  "expiresAt": "2025-12-31T23:59:59"
}
```

**Campos requeridos:**
- `userId`: int
- `permissionId`: int

**Campos opcionales:**
- `grantedBy`: int - ID del usuario que otorga el permiso
- `expiresAt`: DateTime - Fecha de expiración del permiso (si es null, no expira)

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "userPermissionId": 1
  },
  "message": "Permiso otorgado al usuario exitosamente"
}
```

**Uso típico:** Para casos especiales donde un usuario necesita un permiso específico sin tener que crear un rol completo.

---

#### 17. POST /api/RBAC/user-permission-denials

Deniega un permiso específico a un usuario.

**Request Body:**
```json
{
  "userId": 1,
  "permissionId": 5,
  "deniedBy": 1,
  "reason": "Restricción temporal por política de seguridad"
}
```

**Campos requeridos:**
- `userId`: int
- `permissionId`: int

**Campos opcionales:**
- `deniedBy`: int - ID del usuario que deniega el permiso
- `reason`: string (máximo 500 caracteres) - Razón de la denegación

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": {
    "denialId": 1
  },
  "message": "Permiso denegado al usuario exitosamente"
}
```

**Nota importante:** Las denegaciones tienen prioridad. Si un usuario tiene un permiso a través de un rol pero también tiene una denegación activa, el permiso NO se otorga.

---

### PUT - Actualización

#### 18. PUT /api/RBAC/resources/{resourceId}

Actualiza un recurso existente.

**Path Parameter:**
- `resourceId`: int - ID del recurso a actualizar

**Request Body:**
```json
{
  "resourceName": "Customers Updated",
  "resourceKey": "customers",
  "description": "Descripción actualizada",
  "module": "Corporate",
  "isActive": true
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Recurso actualizado exitosamente"
}
```

**Error si no existe (404 Not Found):**
```json
{
  "success": false,
  "data": null,
  "message": "Recurso no encontrado"
}
```

---

#### 19. PUT /api/RBAC/permissions/{permissionId}

Actualiza un permiso existente.

**Path Parameter:**
- `permissionId`: int - ID del permiso a actualizar

**Request Body:**
```json
{
  "resourceId": 1,
  "actionId": 1,
  "permissionName": "Customers - View Updated",
  "permissionKey": "customers.view",
  "description": "Descripción actualizada",
  "isActive": true
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Permiso actualizado exitosamente"
}
```

---

#### 20. PUT /api/RBAC/roles/{roleId}

Actualiza un rol existente.

**Path Parameter:**
- `roleId`: int - ID del rol a actualizar

**Request Body:**
```json
{
  "roleName": "Manager Updated",
  "roleKey": "manager",
  "description": "Descripción actualizada",
  "isActive": true
}
```

**Nota:** El campo `isSystemRole` no se puede actualizar por seguridad.

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Rol actualizado exitosamente"
}
```

---

### DELETE - Eliminación y Remoción

#### 21. DELETE /api/RBAC/resources/{resourceId}

Elimina (desactiva) un recurso.

**Path Parameter:**
- `resourceId`: int - ID del recurso a eliminar

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Recurso eliminado exitosamente"
}
```

**Nota:** La eliminación es lógica (IsActive = false), no física.

---

#### 22. DELETE /api/RBAC/permissions/{permissionId}

Elimina (desactiva) un permiso.

**Path Parameter:**
- `permissionId`: int - ID del permiso a eliminar

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Permiso eliminado exitosamente"
}
```

---

#### 23. DELETE /api/RBAC/roles/{roleId}

Elimina (desactiva) un rol.

**Path Parameter:**
- `roleId`: int - ID del rol a eliminar

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Rol eliminado exitosamente"
}
```

---

#### 24. DELETE /api/RBAC/role-permissions

Remueve un permiso de un rol.

**Query Parameters (requeridos):**
- `roleId`: int
- `permissionId`: int

**Ejemplo:**
```
DELETE /api/RBAC/role-permissions?roleId=1&permissionId=5
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Permiso removido del rol exitosamente"
}
```

---

#### 25. DELETE /api/RBAC/user-roles

Remueve un rol de un usuario.

**Query Parameters (requeridos):**
- `userId`: int
- `roleId`: int

**Ejemplo:**
```
DELETE /api/RBAC/user-roles?userId=1&roleId=2
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Rol removido del usuario exitosamente"
}
```

**Nota:** La remoción es lógica (IsActive = false), no física.

---

#### 26. DELETE /api/RBAC/user-permissions

Revoca un permiso directo de un usuario.

**Query Parameters (requeridos):**
- `userId`: int
- `permissionId`: int

**Ejemplo:**
```
DELETE /api/RBAC/user-permissions?userId=1&permissionId=5
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Permiso revocado del usuario exitosamente"
}
```

---

#### 27. DELETE /api/RBAC/user-permission-denials

Remueve una denegación de permiso de un usuario.

**Query Parameters (requeridos):**
- `userId`: int
- `permissionId`: int

**Ejemplo:**
```
DELETE /api/RBAC/user-permission-denials?userId=1&permissionId=5
```

**Respuesta Exitosa (200 OK):**
```json
{
  "success": true,
  "data": null,
  "message": "Denegación removida del usuario exitosamente"
}
```

---

## Estructura de Datos

### Resource
```json
{
  "resourceId": 1,
  "resourceName": "Customers",
  "resourceKey": "customers",
  "description": "Gestión de clientes corporativos",
  "module": "Corporate",
  "isActive": true,
  "createdAt": "2024-12-20T10:00:00",
  "updatedAt": null
}
```

### Action
```json
{
  "actionId": 1,
  "actionName": "View",
  "actionKey": "view",
  "description": "Ver/Consultar",
  "isActive": true,
  "createdAt": "2024-12-20T10:00:00"
}
```

### Permission
```json
{
  "permissionId": 1,
  "resourceId": 1,
  "actionId": 1,
  "permissionName": "Customers - View",
  "permissionKey": "customers.view",
  "description": "Permite ver clientes",
  "isActive": true,
  "createdAt": "2024-12-20T10:00:00",
  "updatedAt": null
}
```

### Role
```json
{
  "roleId": 1,
  "roleName": "Administrator",
  "roleKey": "admin",
  "description": "Rol de administrador con todos los permisos",
  "isSystemRole": true,
  "isActive": true,
  "createdAt": "2024-12-20T10:00:00",
  "updatedAt": null
}
```

### RolePermission
```json
{
  "rolePermissionId": 1,
  "roleId": 1,
  "permissionId": 5,
  "grantedBy": 1,
  "grantedAt": "2024-12-20T10:00:00"
}
```

### UserRole
```json
{
  "userRoleId": 1,
  "userId": 1,
  "roleId": 2,
  "assignedBy": 1,
  "assignedAt": "2024-12-20T10:00:00",
  "expiresAt": null,
  "isActive": true
}
```

### UserPermission
```json
{
  "userPermissionId": 1,
  "userId": 1,
  "permissionId": 5,
  "grantedBy": 1,
  "grantedAt": "2024-12-20T10:00:00",
  "expiresAt": null,
  "isActive": true
}
```

### UserPermissionDenial
```json
{
  "userPermissionDenialId": 1,
  "userId": 1,
  "permissionId": 5,
  "deniedBy": 1,
  "deniedAt": "2024-12-20T10:00:00",
  "reason": "Restricción temporal por política de seguridad",
  "isActive": true
}
```

---

## Ejemplos de Uso

### Ejemplo 1: Configurar Permisos para un Rol "Manager"

**Paso 1:** Crear el rol
```http
POST /api/RBAC/roles
Authorization: Bearer {token}
Content-Type: application/json

{
  "roleName": "Manager",
  "roleKey": "manager",
  "description": "Rol de gerente con permisos de gestión"
}
```

**Paso 2:** Obtener los permisos disponibles
```http
GET /api/RBAC/permissions?isActive=true
Authorization: Bearer {token}
```

**Paso 3:** Asignar permisos al rol (ejemplo: asignar 3 permisos)
```http
POST /api/RBAC/role-permissions
Authorization: Bearer {token}
Content-Type: application/json

{
  "roleId": 1,
  "permissionId": 1,
  "grantedBy": 1
}
```

Repetir para cada permiso que se quiera asignar.

**Paso 4:** Verificar permisos asignados
```http
GET /api/RBAC/role-permissions?roleId=1
Authorization: Bearer {token}
```

---

### Ejemplo 2: Asignar Rol a Usuario

**Paso 1:** Verificar que el rol existe
```http
GET /api/RBAC/roles?roleId=1
Authorization: Bearer {token}
```

**Paso 2:** Asignar el rol al usuario
```http
POST /api/RBAC/user-roles
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": 5,
  "roleId": 1,
  "assignedBy": 1,
  "expiresAt": null
}
```

**Paso 3:** Verificar roles del usuario
```http
GET /api/RBAC/user-roles?userId=5&isActive=true
Authorization: Bearer {token}
```

---

### Ejemplo 3: Otorgar Permiso Directo a Usuario

**Paso 1:** Verificar que el permiso existe
```http
GET /api/RBAC/permissions?permissionId=10
Authorization: Bearer {token}
```

**Paso 2:** Otorgar permiso directo
```http
POST /api/RBAC/user-permissions
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": 5,
  "permissionId": 10,
  "grantedBy": 1,
  "expiresAt": "2025-12-31T23:59:59"
}
```

---

### Ejemplo 4: Consultar Permisos Efectivos de un Usuario

**Paso único:** Obtener todos los permisos efectivos
```http
GET /api/RBAC/user-effective-permissions/5
Authorization: Bearer {token}
```

Este endpoint devuelve automáticamente:
- Todos los permisos de los roles activos del usuario
- Todos los permisos directos activos
- Excluye permisos denegados
- Considera fechas de expiración

---

### Ejemplo 5: Pantalla de Gestión de Permisos para Usuario

**Flujo recomendado para la pantalla:**

1. **Cargar datos iniciales:**
   - Obtener usuario: `GET /api/Auth/users?userId={userId}`
   - Obtener roles del usuario: `GET /api/RBAC/user-roles?userId={userId}&isActive=true`
   - Obtener permisos directos: `GET /api/RBAC/user-permissions?userId={userId}&isActive=true`
   - Obtener denegaciones: `GET /api/RBAC/user-permission-denials?userId={userId}&isActive=true`
   - Obtener permisos efectivos: `GET /api/RBAC/user-effective-permissions/{userId}`

2. **Para asignar un rol:**
   - Obtener lista de roles disponibles: `GET /api/RBAC/roles?isActive=true`
   - Asignar rol: `POST /api/RBAC/user-roles`

3. **Para remover un rol:**
   - Remover rol: `DELETE /api/RBAC/user-roles?userId={userId}&roleId={roleId}`

4. **Para otorgar permiso directo:**
   - Obtener lista de permisos: `GET /api/RBAC/permissions?isActive=true`
   - Otorgar permiso: `POST /api/RBAC/user-permissions`

5. **Para revocar permiso directo:**
   - Revocar permiso: `DELETE /api/RBAC/user-permissions?userId={userId}&permissionId={permissionId}`

6. **Para denegar un permiso:**
   - Denegar permiso: `POST /api/RBAC/user-permission-denials`

---

### Ejemplo 6: Pantalla de Gestión de Permisos para Rol

**Flujo recomendado para la pantalla:**

1. **Cargar datos iniciales:**
   - Obtener rol: `GET /api/RBAC/roles?roleId={roleId}`
   - Obtener permisos del rol: `GET /api/RBAC/role-permissions?roleId={roleId}`

2. **Para asignar un permiso al rol:**
   - Obtener lista de permisos disponibles: `GET /api/RBAC/permissions?isActive=true`
   - Asignar permiso: `POST /api/RBAC/role-permissions`

3. **Para remover un permiso del rol:**
   - Remover permiso: `DELETE /api/RBAC/role-permissions?roleId={roleId}&permissionId={permissionId}`

---

## Manejo de Errores

### Códigos de Estado HTTP

- **200 OK**: Operación exitosa
- **400 Bad Request**: Error de validación o datos inválidos
- **401 Unauthorized**: Token JWT inválido o no proporcionado
- **404 Not Found**: Recurso no encontrado
- **500 Internal Server Error**: Error interno del servidor

### Estructura de Respuesta de Error

```json
{
  "success": false,
  "data": null,
  "message": "Mensaje descriptivo del error",
  "statusCode": 400,
  "errorNumber": null,
  "timestamp": "2024-12-20T10:00:00"
}
```

---

## Notas Importantes

1. **Autenticación:** Todos los endpoints requieren JWT válido en el header `Authorization: Bearer {token}`

2. **Eliminación Lógica:** Los DELETE no eliminan físicamente los registros, solo los desactivan (IsActive = false)

3. **Fechas de Expiración:** Los roles y permisos con `expiresAt` se consideran activos solo si la fecha no ha pasado

4. **Prioridad de Denegaciones:** Las denegaciones siempre tienen prioridad sobre permisos otorgados

5. **Múltiples Roles:** Un usuario puede tener múltiples roles, los permisos se suman

6. **Permisos Efectivos:** Use el endpoint `GET /api/RBAC/user-effective-permissions/{userId}` para obtener los permisos finales calculados

7. **Búsquedas Parciales:** Los parámetros de búsqueda por nombre usan LIKE (búsqueda parcial)

8. **Ordenamiento:** Los resultados se ordenan por fecha descendente (más recientes primero) o alfabéticamente según el caso

---

**Última actualización:** Diciembre 2024  
**Versión del Backend:** .NET 8.0

