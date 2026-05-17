# Guía de Uso de APIs RBAC - Sistema de Permisos

## 📋 Tabla de Contenidos
1. [Conceptos Básicos](#-conceptos-básicos)
2. [Aplicaciones del Sistema](#-aplicaciones-del-sistema)
3. [APIs de Consulta](#-apis-de-consulta)
4. [APIs de Creación](#-apis-de-creación)
5. [APIs de Actualización](#-apis-de-actualización)
6. [APIs de Eliminación](#-apis-de-eliminación)
7. [Flujo de Administración](#-flujo-de-administración)
8. [Estructura en Árbol de Permisos](#-estructura-en-árbol-de-permisos)

---

## 🎯 Conceptos Básicos

### ¿Qué es RBAC?
RBAC (Role-Based Access Control) es un sistema que controla qué usuarios pueden acceder a qué funciones del sistema basándose en roles y permisos específicos.

### Elementos del Sistema
- **🏢 Aplicación**: Divide el sistema (Corporate o Brand Partner)
- **📁 Recurso**: Representa una pantalla o módulo (ej. Contratos, Facturas)
- **⚡ Acción**: Operación que se puede realizar (ej. leer, crear, generar_pdf)
- **🔐 Permiso**: Combinación de Aplicación + Recurso + Acción
- **👤 Rol**: Conjunto de permisos agrupados por perfil de usuario
- **👥 Usuario**: Persona que usa el sistema y tiene roles asignados

### Estructura de Permisos
Los permisos siguen el patrón: `{applicationKey}.{resourceKey}.{actionKey}`

**Ejemplos:**
- `corporate.contracts.read` - Leer contratos en Corporate
- `corporate.contracts.generate_pdf` - Generar PDF en módulo Contratos
- `brandpartner.invoices.create` - Crear facturas en Brand Partner

---

## 🏢 Aplicaciones del Sistema

### ApplicationId Reference
- **ApplicationId 1**: Corporate
- **ApplicationId 2**: Brand Partner

### Usando applicationKey vs applicationId
- **applicationKey**: Más amigable para usar en URLs (`"corporate"`, `"brandpartner"`)
- **applicationId**: Más eficiente para la base de datos (1, 2)

Los APIs aceptan ambos formatos, el sistema convierte automáticamente.

---

## 🔍 APIs de Consulta

### GET `/api/RBAC/applications`
**Propósito**: Listar todas las aplicaciones disponibles para combos de administración.

**Parámetros Query:**
- `isActive` (bool, opcional): Solo aplicaciones activas. Default: `true`

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "2 aplicación(es) encontrada(s)",
  "data": [
    {
      "applicationId": 1,
      "applicationName": "Corporate",
      "applicationKey": "corporate",
      "description": "Sistema corporativo interno",
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    },
    {
      "applicationId": 2,
      "applicationName": "Brand Partner",
      "applicationKey": "brandpartner",
      "description": "Portal para socios de marca",
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    }
  ]
}
```

---

### GET `/api/RBAC/resources`
**Propósito**: Listar recursos (pantallas/módulos) con filtros opcionales.

**Parámetros Query:**
- `resourceId` (int, opcional): ID específico del recurso
- `resourceName` (string, opcional): Filtrar por nombre
- `resourceKey` (string, opcional): Filtrar por clave única
- `module` (string, opcional): Filtrar por módulo
- `isActive` (bool, opcional): Solo recursos activos. Default: `true`
- `applicationKey` (string, opcional): Filtrar por aplicación ("corporate", "brandpartner")

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "3 recurso(s) encontrado(s)",
  "data": [
    {
      "resourceId": 1,
      "applicationId": 1,
      "resourceName": "Contratos",
      "resourceKey": "contracts",
      "description": "Gestión de contratos corporativos",
      "module": "Financial",
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    }
  ]
}
```

---

### GET `/api/RBAC/actions`
**Propósito**: Listar acciones disponibles en el sistema.

**Parámetros Query:**
- `actionId` (int, opcional): ID específico de la acción
- `actionName` (string, opcional): Filtrar por nombre
- `actionKey` (string, opcional): Filtrar por clave única
- `isActive` (bool, opcional): Solo acciones activas. Default: `true`

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "5 acción(es) encontrada(s)",
  "data": [
    {
      "actionId": 1,
      "actionName": "Ver pantalla",
      "actionKey": "page_view",
      "description": "Acceder y visualizar la pantalla",
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    },
    {
      "actionId": 2,
      "actionName": "Leer",
      "actionKey": "read",
      "description": "Consultar información",
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    },
    {
      "actionId": 3,
      "actionName": "Generar PDF",
      "actionKey": "generate_pdf",
      "description": "Generar documentos PDF",
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    }
  ]
}
```

---

### GET `/api/RBAC/permissions`
**Propósito**: Listar permisos específicos (combinación de aplicación + recurso + acción).

**Parámetros Query:**
- `permissionId` (int, opcional): ID específico del permiso
- `resourceId` (int, opcional): Filtrar por recurso
- `actionId` (int, opcional): Filtrar por acción
- `permissionKey` (string, opcional): Filtrar por clave única
- `isActive` (bool, opcional): Solo permisos activos. Default: `true`
- `applicationId` (int, opcional): Filtrar por aplicación (1=Corporate, 2=Brand Partner)
- `applicationKey` (string, opcional): Filtrar por aplicación ("corporate", "brandpartner")

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "8 permiso(s) encontrado(s)",
  "data": [
    {
      "permissionId": 1,
      "applicationId": 1,
      "resourceId": 1,
      "actionId": 1,
      "permissionName": "Ver Contratos",
      "permissionKey": "corporate.contracts.page_view",
      "description": "Acceso a la pantalla de contratos",
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    }
  ]
}
```

---

### GET `/api/RBAC/roles`
**Propósito**: Listar roles disponibles en el sistema.

**Parámetros Query:**
- `roleId` (int, opcional): ID específico del rol
- `roleName` (string, opcional): Filtrar por nombre
- `roleKey` (string, opcional): Filtrar por clave única
- `isSystemRole` (bool, opcional): Filtrar roles del sistema vs personalizados
- `isActive` (bool, opcional): Solo roles activos. Default: `true`
- `applicationKey` (string, opcional): Filtrar por aplicación

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "3 rol(es) encontrado(s)",
  "data": [
    {
      "roleId": 1,
      "applicationId": 1,
      "roleName": "Administrador Corporate",
      "roleKey": "admin_corporate",
      "description": "Acceso completo al sistema corporativo",
      "isSystemRole": true,
      "isActive": true,
      "createdAt": "2023-01-01T00:00:00Z"
    }
  ]
}
```

---

### GET `/api/RBAC/role-permissions`
**Propósito**: Ver qué permisos tiene asignado un rol específico.

**Parámetros Query:**
- `roleId` (int, opcional): Filtrar por rol específico
- `permissionId` (int, opcional): Filtrar por permiso específico
- `applicationId` (int, opcional): Filtrar por aplicación
- `applicationKey` (string, opcional): Filtrar por aplicación

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "12 permiso(s) asignado(s) encontrado(s)",
  "data": [
    {
      "rolePermissionId": 1,
      "roleId": 1,
      "permissionId": 1,
      "grantedBy": 1,
      "createdAt": "2023-01-01T00:00:00Z",
      "isActive": true
    }
  ]
}
```

---

### GET `/api/RBAC/user-roles`
**Propósito**: Ver qué roles tiene asignado un usuario.

**Parámetros Query:**
- `userId` (int, opcional): Filtrar por usuario específico
- `roleId` (int, opcional): Filtrar por rol específico
- `isActive` (bool, opcional): Solo asignaciones activas. Default: `true`
- `applicationId` (int, opcional): Filtrar por aplicación

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "2 rol(es) asignado(s) encontrado(s)",
  "data": [
    {
      "userRoleId": 1,
      "userId": 5,
      "roleId": 1,
      "applicationId": 1,
      "assignedBy": 1,
      "expiresAt": null,
      "createdAt": "2023-06-01T00:00:00Z",
      "isActive": true
    }
  ]
}
```

---

### GET `/api/RBAC/user-effective-permissions/{userId}`
**Propósito**: Ver TODOS los permisos efectivos de un usuario (combinando roles + permisos directos - denegaciones).

**Parámetros Path:**
- `userId` (int, requerido): ID del usuario

**Parámetros Query:**
- `applicationKey` (string, opcional): Filtrar por aplicación específica

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "25 permiso(s) efectivo(s) encontrado(s)",
  "data": [
    {
      "permissionId": 1,
      "applicationId": 1,
      "permissionKey": "corporate.contracts.page_view",
      "permissionName": "Ver Contratos",
      "resourceName": "Contratos",
      "actionName": "Ver pantalla"
    }
  ]
}
```

---

## ✨ APIs de Creación

### POST `/api/RBAC/resources`
**Propósito**: Crear una nueva pantalla/módulo en el sistema.

**Query Parameters:**
- `applicationKey` (string, opcional): Si no se envía `applicationId` en el body

**Body JSON:**
```json
{
  "applicationId": 1,
  "resourceName": "Contratos",
  "resourceKey": "contracts",
  "description": "Gestión de contratos corporativos",
  "module": "Financial",
  "isActive": true
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Recurso creado exitosamente",
  "data": {
    "resourceId": 15
  }
}
```

---

### POST `/api/RBAC/actions`
**Propósito**: Crear una nueva acción que se puede realizar en el sistema.

**Body JSON:**
```json
{
  "actionName": "Generar PDF",
  "actionKey": "generate_pdf",
  "description": "Generar documentos PDF",
  "isActive": true
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Acción creada exitosamente",
  "data": {
    "actionId": 8
  }
}
```

---

### POST `/api/RBAC/permissions`
**Propósito**: Crear un nuevo permiso específico (combinación de aplicación + recurso + acción).

**Query Parameters:**
- `applicationKey` (string, opcional): Si no se puede determinar la aplicación automáticamente

**Body JSON:**
```json
{
  "applicationId": 1,
  "resourceId": 15,
  "actionId": 8,
  "permissionName": "Generar PDF Contratos",
  "permissionKey": "corporate.contracts.generate_pdf",
  "description": "Generar documentos PDF de contratos",
  "isActive": true
}
```

> **💡 Tip:** Si `permissionName` y `permissionKey` están vacíos, el sistema los genera automáticamente como `{resourceKey}.{actionKey}`.

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Permiso creado exitosamente",
  "data": {
    "permissionId": 45,
    "permissionKey": "corporate.contracts.generate_pdf"
  }
}
```

---

### POST `/api/RBAC/roles`
**Propósito**: Crear un nuevo rol en el sistema.

**Query Parameters:**
- `applicationKey` (string, opcional): Si no se envía `applicationId` en el body

**Body JSON:**
```json
{
  "applicationId": 1,
  "roleName": "Manager de Contratos",
  "roleKey": "contracts_manager",
  "description": "Gestión completa de contratos sin permisos de administrador",
  "isSystemRole": false,
  "isActive": true
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Rol creado exitosamente",
  "data": {
    "roleId": 12
  }
}
```

---

### POST `/api/RBAC/role-permissions`
**Propósito**: Asignar un permiso específico a un rol.

**Body JSON:**
```json
{
  "roleId": 12,
  "permissionId": 45,
  "grantedBy": 1
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Permiso asignado al rol exitosamente",
  "data": {
    "rolePermissionId": 156
  }
}
```

**Error de Validación (400):**
```json
{
  "success": false,
  "message": "El rol y el permiso deben pertenecer a la misma aplicación"
}
```

---

### POST `/api/RBAC/user-roles`
**Propósito**: Asignar un rol a un usuario específico.

**Body JSON:**
```json
{
  "userId": 25,
  "roleId": 12,
  "applicationId": 1,
  "assignedBy": 1,
  "expiresAt": "2024-12-31T23:59:59Z"
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Rol asignado al usuario exitosamente",
  "data": {
    "userRoleId": 89
  }
}
```

---

## 🔧 APIs de Actualización

### PUT `/api/RBAC/resources/{resourceId}`
**Propósito**: Actualizar información de un recurso existente.

**Body JSON:**
```json
{
  "resourceName": "Contratos Actualizado",
  "resourceKey": "contracts",
  "description": "Nueva descripción del módulo",
  "module": "Financial",
  "isActive": true
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Recurso actualizado exitosamente"
}
```

---

### PUT `/api/RBAC/permissions/{permissionId}`
**Propósito**: Actualizar información de un permiso existente.

**Query Parameters:**
- `applicationKey` (string, opcional): Para validaciones de consistencia

**Body JSON:**
```json
{
  "resourceId": 15,
  "actionId": 8,
  "permissionName": "Nuevo nombre del permiso",
  "description": "Nueva descripción",
  "isActive": true
}
```

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Permiso actualizado exitosamente",
  "data": {
    "permissionKey": "corporate.contracts.generate_pdf"
  }
}
```

---

### PUT `/api/RBAC/roles/{roleId}`
**Propósito**: Actualizar información de un rol existente.

**Body JSON:**
```json
{
  "roleName": "Nombre actualizado del rol",
  "roleKey": "contracts_manager",
  "description": "Nueva descripción del rol",
  "isActive": true
}
```

---

## 🗑️ APIs de Eliminación

### DELETE `/api/RBAC/resources/{resourceId}`
**Propósito**: Desactivar un recurso (soft delete).

**Respuesta Exitosa (200):**
```json
{
  "success": true,
  "message": "Recurso eliminado exitosamente"
}
```

---

### DELETE `/api/RBAC/role-permissions?roleId={roleId}&permissionId={permissionId}`
**Propósito**: Remover un permiso específico de un rol.

**Query Parameters:**
- `roleId` (int, requerido): ID del rol
- `permissionId` (int, requerido): ID del permiso a remover

---

### DELETE `/api/RBAC/user-roles?userId={userId}&roleId={roleId}`
**Propósito**: Remover un rol de un usuario.

**Query Parameters:**
- `userId` (int, requerido): ID del usuario
- `roleId` (int, requerido): ID del rol a remover

---

## 🎯 Flujo de Administración

### Paso 1: Crear Recursos (Pantallas)
1. **Identificar la pantalla**: ¿Qué módulo nuevo necesitas?
2. **Definir applicationId**: ¿Es para Corporate (1) o Brand Partner (2)?
3. **Usar `POST /api/RBAC/resources`**

```json
{
  "applicationId": 1,
  "resourceName": "Contratos",
  "resourceKey": "contracts",
  "description": "Pantalla de gestión de contratos",
  "module": "Financial"
}
```

### Paso 2: Crear Acciones
1. **Identificar operaciones**: ¿Qué puede hacer el usuario en esa pantalla?
2. **Usar `POST /api/RBAC/actions`** para cada acción

```json
{
  "actionName": "Generar facturas automáticas",
  "actionKey": "generate_automatic_invoices",
  "description": "Generar facturas de forma automática"
}
```

### Paso 3: Crear Permisos
1. **Combinar recurso + acción** para cada permiso específico
2. **Usar `POST /api/RBAC/permissions`**

```json
{
  "resourceId": 15,
  "actionId": 8,
  "description": "Permite generar facturas automáticas en contratos"
}
```

### Paso 4: Crear/Actualizar Roles
1. **Definir perfiles de usuario**: ¿Qué tipos de usuarios necesitas?
2. **Usar `POST /api/RBAC/roles`**

```json
{
  "applicationId": 1,
  "roleName": "Manager de Contratos",
  "roleKey": "contracts_manager",
  "description": "Gestión completa de contratos"
}
```

### Paso 5: Asignar Permisos a Roles
1. **Usar `POST /api/RBAC/role-permissions`** para cada permiso

```json
{
  "roleId": 12,
  "permissionId": 45,
  "grantedBy": 1
}
```

### Paso 6: Asignar Roles a Usuarios
1. **Usar `POST /api/RBAC/user-roles`**

```json
{
  "userId": 25,
  "roleId": 12,
  "applicationId": 1,
  "assignedBy": 1
}
```

---

## 🌳 Estructura en Árbol de Permisos

### Corporate (ApplicationId: 1)

```
📁 Contratos (corporate.contracts)
   ☑ Ver pantalla               (corporate.contracts.page_view)
   ☑ Leer contratos             (corporate.contracts.read)
   ☑ Crear contrato             (corporate.contracts.create)
   ☑ Editar contrato            (corporate.contracts.update)
   ☑ Eliminar contrato          (corporate.contracts.delete)
   ☑ Generar facturas automáticas (corporate.contracts.generate_automatic_invoices)
   ☑ Generar facturas manuales  (corporate.contracts.generate_manual_invoices)
   ☑ Enviar correo              (corporate.contracts.send_email)
   ☑ Generar PDF                (corporate.contracts.generate_pdf)
   ☑ Calcular importes          (corporate.contracts.calculate_amounts)

📁 Facturas (corporate.invoices)
   ☑ Ver pantalla               (corporate.invoices.page_view)
   ☑ Leer facturas              (corporate.invoices.read)
   ☑ Crear factura              (corporate.invoices.create)
   ☑ Editar factura             (corporate.invoices.update)
   ☑ Enviar por correo          (corporate.invoices.send_email)
   ☑ Generar PDF                (corporate.invoices.generate_pdf)
   ☑ Marcar como pagada         (corporate.invoices.mark_paid)

📁 Usuarios (corporate.users)
   ☑ Ver pantalla               (corporate.users.page_view)
   ☑ Leer usuarios              (corporate.users.read)
   ☑ Crear usuario              (corporate.users.create)
   ☑ Editar usuario             (corporate.users.update)
   ☑ Desactivar usuario         (corporate.users.deactivate)

📁 RBAC (corporate.rbac)
   ☑ Ver pantalla               (corporate.rbac.page_view)
   ☑ Gestionar recursos         (corporate.rbac.manage_resources)
   ☑ Gestionar acciones         (corporate.rbac.manage_actions)
   ☑ Gestionar permisos         (corporate.rbac.manage_permissions)
   ☑ Gestionar roles            (corporate.rbac.manage_roles)
   ☑ Asignar permisos           (corporate.rbac.assign_permissions)
   ☑ Asignar roles              (corporate.rbac.assign_roles)
```

### Brand Partner (ApplicationId: 2)

```
📁 Dashboard (brandpartner.dashboard)
   ☑ Ver pantalla               (brandpartner.dashboard.page_view)
   ☑ Ver estadísticas           (brandpartner.dashboard.view_stats)

📁 Campañas (brandpartner.campaigns)
   ☑ Ver pantalla               (brandpartner.campaigns.page_view)
   ☑ Leer campañas              (brandpartner.campaigns.read)
   ☑ Crear campaña              (brandpartner.campaigns.create)
   ☑ Editar campaña             (brandpartner.campaigns.update)
   ☑ Generar reporte            (brandpartner.campaigns.generate_report)

📁 Facturas (brandpartner.invoices)
   ☑ Ver pantalla               (brandpartner.invoices.page_view)
   ☑ Leer facturas              (brandpartner.invoices.read)
   ☑ Descargar PDF              (brandpartner.invoices.download_pdf)
```

---

## 💡 Consejos de Uso

### Para Desarrolladores Frontend
1. **Usar `GET /api/RBAC/user-effective-permissions/{userId}`** para obtener todos los permisos de un usuario
2. **Filtrar por `applicationKey`** para obtener solo los permisos de la aplicación actual
3. **Verificar permisos por `permissionKey`** antes de mostrar botones o funcionalidades

### Para Administradores
1. **Crear recursos por pantalla/módulo**, no por funcionalidad específica
2. **Crear acciones genéricas** que se puedan reutilizar en múltiples recursos
3. **Usar roles para agrupar permisos** según el perfil del usuario
4. **Aplicar el principio de menor privilegio**: Solo dar los permisos necesarios

### Para APIs
1. **Siempre filtrar por applicationId o applicationKey** para evitar mezclar permisos de diferentes aplicaciones
2. **Usar `permissionKey` para validaciones** ya que es más legible que los IDs
3. **Cachear permisos efectivos del usuario** para mejorar performance

---

## ❌ Respuestas de Error Comunes

### Error 400 - Bad Request
```json
{
  "success": false,
  "message": "El rol y el permiso deben pertenecer a la misma aplicación"
}
```

### Error 404 - Not Found
```json
{
  "success": false,
  "message": "No se encontraron permisos"
}
```

### Error 500 - Server Error
```json
{
  "success": false,
  "message": "Error al crear permiso: Database connection failed"
}
```

---

## 🔍 Filtros Útiles

### Obtener todos los permisos de Corporate
```
GET /api/RBAC/permissions?applicationKey=corporate&isActive=true
```

### Obtener permisos de un recurso específico
```
GET /api/RBAC/permissions?resourceId=15&isActive=true
```

### Obtener roles de una aplicación
```
GET /api/RBAC/roles?applicationKey=brandpartner&isActive=true
```

### Ver permisos efectivos de un usuario en Corporate
```
GET /api/RBAC/user-effective-permissions/25?applicationKey=corporate
```

---

> **📝 Nota**: Todos los APIs requieren autenticación (`[Authorize]`). Asegúrate de incluir el token JWT en el header `Authorization: Bearer {token}`.