# Cambios en APIs RBAC - Soporte de Applications

## 📋 Resumen para Frontend

Se ha agregado soporte para múltiples aplicaciones (Corporate y BrandPartner) en el sistema RBAC. Ahora los endpoints de Resources y Roles requieren especificar la aplicación mediante `applicationKey` o `applicationId`.

---

## 🔄 Cambios en Endpoints

### 1. **GET `/api/RBAC/resources`**

**Cambio:** Ahora requiere el parámetro `applicationKey` (recomendado) o `applicationId` como query parameter.

**Antes:**
```
GET /api/RBAC/resources
```

**Ahora:**
```
GET /api/RBAC/resources?applicationKey=corporate
GET /api/RBAC/resources?applicationId=1
```

**Query Parameters:**
- `applicationKey` (string, opcional pero recomendado): 'corporate' o 'brandpartner'
- `applicationId` (int, opcional): ID de la aplicación
- Los demás parámetros siguen igual: `resourceId`, `resourceName`, `resourceKey`, `module`, `isActive`

**Ejemplo de Request:**
```http
GET /api/RBAC/resources?applicationKey=corporate&isActive=true
Authorization: Bearer {token}
```

**Ejemplo de Response:**
```json
{
  "success": true,
  "message": "2 recurso(s) encontrado(s)",
  "data": [
    {
      "resourceId": 1,
      "applicationId": 1,
      "resourceName": "Customers",
      "resourceKey": "customers",
      "description": "Customer management",
      "module": "CustomerManagement",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00",
      "updatedAt": null
    }
  ]
}
```

---

### 2. **GET `/api/RBAC/roles`**

**Cambio:** Ahora requiere el parámetro `applicationKey` (recomendado) o `applicationId` como query parameter.

**Antes:**
```
GET /api/RBAC/roles
```

**Ahora:**
```
GET /api/RBAC/roles?applicationKey=corporate
GET /api/RBAC/roles?applicationId=1
```

**Query Parameters:**
- `applicationKey` (string, opcional pero recomendado): 'corporate' o 'brandpartner'
- `applicationId` (int, opcional): ID de la aplicación
- Los demás parámetros siguen igual: `roleId`, `roleName`, `roleKey`, `isSystemRole`, `isActive`

**Ejemplo de Request:**
```http
GET /api/RBAC/roles?applicationKey=corporate&isActive=true
Authorization: Bearer {token}
```

**Ejemplo de Response:**
```json
{
  "success": true,
  "message": "3 rol(es) encontrado(s)",
  "data": [
    {
      "roleId": 1,
      "applicationId": 1,
      "roleName": "Administrator",
      "roleKey": "admin",
      "description": "Full system access",
      "isSystemRole": true,
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00",
      "updatedAt": null
    }
  ]
}
```

---

### 3. **POST `/api/RBAC/resources`**

**Cambio:** Ahora requiere `applicationKey` o `applicationId` en el body.

**Antes:**
```json
{
  "resourceName": "New Resource",
  "resourceKey": "new_resource",
  "description": "Description",
  "module": "ModuleName"
}
```

**Ahora:**
```json
{
  "applicationKey": "corporate",  // O "applicationId": 1
  "resourceName": "New Resource",
  "resourceKey": "new_resource",
  "description": "Description",
  "module": "ModuleName"
}
```

**Body Parameters:**
- `applicationKey` (string, opcional): 'corporate' o 'brandpartner' - **Recomendado**
- `applicationId` (int, opcional): ID de la aplicación
- `resourceName` (string, requerido)
- `resourceKey` (string, requerido)
- `description` (string, opcional)
- `module` (string, opcional)

**Ejemplo de Request:**
```http
POST /api/RBAC/resources
Authorization: Bearer {token}
Content-Type: application/json

{
  "applicationKey": "corporate",
  "resourceName": "New Resource",
  "resourceKey": "new_resource",
  "description": "Description",
  "module": "ModuleName"
}
```

**Ejemplo de Response:**
```json
{
  "success": true,
  "message": "Recurso creado exitosamente",
  "data": {
    "resourceId": 10
  }
}
```

---

### 4. **POST `/api/RBAC/roles`**

**Cambio:** Ahora requiere `applicationKey` o `applicationId` en el body.

**Antes:**
```json
{
  "roleName": "New Role",
  "roleKey": "new_role",
  "description": "Description",
  "isSystemRole": false
}
```

**Ahora:**
```json
{
  "applicationKey": "corporate",  // O "applicationId": 1
  "roleName": "New Role",
  "roleKey": "new_role",
  "description": "Description",
  "isSystemRole": false
}
```

**Body Parameters:**
- `applicationKey` (string, opcional): 'corporate' o 'brandpartner' - **Recomendado**
- `applicationId` (int, opcional): ID de la aplicación
- `roleName` (string, requerido)
- `roleKey` (string, requerido)
- `description` (string, opcional)
- `isSystemRole` (boolean, opcional, default: false)

**Ejemplo de Request:**
```http
POST /api/RBAC/roles
Authorization: Bearer {token}
Content-Type: application/json

{
  "applicationKey": "corporate",
  "roleName": "Manager",
  "roleKey": "manager",
  "description": "Manager role",
  "isSystemRole": false
}
```

**Ejemplo de Response:**
```json
{
  "success": true,
  "message": "Rol creado exitosamente",
  "data": {
    "roleId": 5
  }
}
```

---

### 5. **GET `/api/RBAC/user-effective-permissions/{userId}`**

**Cambio:** Ahora acepta el parámetro opcional `applicationKey` para filtrar permisos por aplicación.

**Antes:**
```
GET /api/RBAC/user-effective-permissions/123
```

**Ahora:**
```
GET /api/RBAC/user-effective-permissions/123?applicationKey=corporate
```

**Query Parameters:**
- `applicationKey` (string, opcional): Si se proporciona, filtra los permisos por aplicación. Si no se proporciona, devuelve todos los permisos del usuario.

**Ejemplo de Request:**
```http
GET /api/RBAC/user-effective-permissions/123?applicationKey=corporate
Authorization: Bearer {token}
```

**Ejemplo de Response:**
```json
{
  "success": true,
  "message": "5 permiso(s) efectivo(s) encontrado(s)",
  "data": [
    {
      "permissionId": 1,
      "resourceId": 1,
      "actionId": 1,
      "permissionName": "View Customers",
      "permissionKey": "customers.view",
      "description": "View customer information",
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00",
      "updatedAt": null
    }
  ]
}
```

---

## ⚠️ Cambios Importantes

### 1. **Resources y Roles ahora tienen `applicationId`**

Todas las respuestas de Resources y Roles ahora incluyen el campo `applicationId`:

```json
{
  "resourceId": 1,
  "applicationId": 1,  // ← NUEVO
  "resourceName": "Customers",
  ...
}
```

### 2. **Filtrado por Aplicación es Recomendado**

Aunque los parámetros `applicationKey` y `applicationId` son técnicamente opcionales en los GET, **se recomienda siempre incluirlos** para evitar resultados inesperados. Sin estos parámetros, podrías obtener recursos/roles de múltiples aplicaciones mezclados.

### 3. **ApplicationKey vs ApplicationId**

- **`applicationKey`**: String legible ('corporate', 'brandpartner') - **Recomendado para usar en el frontend**
- **`applicationId`**: Número entero (1, 2, etc.) - Útil si ya tienes el ID

### 4. **Valores de ApplicationKey**

Los valores válidos son:
- `"corporate"` - Para la aplicación Corporate
- `"brandpartner"` - Para la aplicación Brand Partner

---

## 🔧 Migración del Frontend

### Paso 1: Actualizar llamadas GET

**Antes:**
```typescript
// ❌ Antes
const resources = await fetch('/api/RBAC/resources');
```

**Ahora:**
```typescript
// ✅ Ahora
const resources = await fetch('/api/RBAC/resources?applicationKey=corporate');
```

### Paso 2: Actualizar llamadas POST

**Antes:**
```typescript
// ❌ Antes
const response = await fetch('/api/RBAC/resources', {
  method: 'POST',
  body: JSON.stringify({
    resourceName: 'New Resource',
    resourceKey: 'new_resource'
  })
});
```

**Ahora:**
```typescript
// ✅ Ahora
const response = await fetch('/api/RBAC/resources', {
  method: 'POST',
  body: JSON.stringify({
    applicationKey: 'corporate',  // ← Agregar esto
    resourceName: 'New Resource',
    resourceKey: 'new_resource'
  })
});
```

### Paso 3: Actualizar tipos/interfaces

**TypeScript/Interfaces:**
```typescript
// Resource ahora incluye applicationId
interface Resource {
  resourceId: number;
  applicationId: number;  // ← NUEVO
  resourceName: string;
  resourceKey: string;
  description?: string;
  module?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// Role ahora incluye applicationId
interface Role {
  roleId: number;
  applicationId: number;  // ← NUEVO
  roleName: string;
  roleKey: string;
  description?: string;
  isSystemRole: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// Request para crear Resource
interface CreateResourceRequest {
  applicationKey?: string;  // ← NUEVO
  applicationId?: number;   // ← NUEVO
  resourceName: string;
  resourceKey: string;
  description?: string;
  module?: string;
}

// Request para crear Role
interface CreateRoleRequest {
  applicationKey?: string;  // ← NUEVO
  applicationId?: number;   // ← NUEVO
  roleName: string;
  roleKey: string;
  description?: string;
  isSystemRole?: boolean;
}
```

---

## 📝 Ejemplos Completos

### Ejemplo 1: Obtener todos los recursos de Corporate

```typescript
async function getCorporateResources() {
  const response = await fetch('/api/RBAC/resources?applicationKey=corporate&isActive=true', {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  const result = await response.json();
  return result.data; // Array de Resources con applicationId
}
```

### Ejemplo 2: Crear un nuevo recurso en Corporate

```typescript
async function createCorporateResource(resourceData: {
  resourceName: string;
  resourceKey: string;
  description?: string;
  module?: string;
}) {
  const response = await fetch('/api/RBAC/resources', {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      applicationKey: 'corporate',
      ...resourceData
    })
  });
  
  const result = await response.json();
  return result.data.resourceId;
}
```

### Ejemplo 3: Obtener permisos efectivos de un usuario para Corporate

```typescript
async function getUserCorporatePermissions(userId: number) {
  const response = await fetch(
    `/api/RBAC/user-effective-permissions/${userId}?applicationKey=corporate`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );
  
  const result = await response.json();
  return result.data; // Array de Permissions filtrados por Corporate
}
```

---

## 🚨 Errores Comunes

### Error: "ApplicationKey 'xxx' no encontrado o inactivo"

**Causa:** El `applicationKey` proporcionado no existe o está inactivo.

**Solución:** Verifica que estés usando `'corporate'` o `'brandpartner'` (en minúsculas).

### Error: "ApplicationId o ApplicationKey es requerido"

**Causa:** Al crear un Resource o Role, no se proporcionó `applicationId` ni `applicationKey`.

**Solución:** Agrega `applicationKey` o `applicationId` en el body del request.

### Resultados vacíos en GET

**Causa:** Puede que no haya recursos/roles para la aplicación especificada, o el `applicationKey` es incorrecto.

**Solución:** Verifica que el `applicationKey` sea correcto y que existan datos para esa aplicación.

---

## ✅ Checklist de Migración

- [ ] Actualizar todas las llamadas GET a `/api/RBAC/resources` para incluir `applicationKey`
- [ ] Actualizar todas las llamadas GET a `/api/RBAC/roles` para incluir `applicationKey`
- [ ] Actualizar todas las llamadas POST a `/api/RBAC/resources` para incluir `applicationKey` en el body
- [ ] Actualizar todas las llamadas POST a `/api/RBAC/roles` para incluir `applicationKey` en el body
- [ ] Actualizar interfaces/tipos TypeScript para incluir `applicationId` en Resource y Role
- [ ] Actualizar componentes que muestran Resources/Roles para manejar `applicationId`
- [ ] Actualizar llamadas a `user-effective-permissions` para incluir `applicationKey` si es necesario
- [ ] Probar con ambas aplicaciones: 'corporate' y 'brandpartner'

---

## 📞 Soporte

Si encuentras algún problema o tienes preguntas sobre estos cambios, contacta al equipo de backend.

**Fecha de implementación:** 2024
**Versión de API afectada:** RBAC endpoints

