# Cambios Frontend: Adaptación a Multi-Tenant RBAC con ApplicationId

## Resumen Ejecutivo

El backend ahora soporta **multi-tenant** mediante `ApplicationId`. Todos los endpoints de RBAC (Roles, Permisos, UserRoles) requieren incluir `applicationId` en las requests. Un usuario puede tener acceso a múltiples aplicaciones y tener roles diferentes en cada una.

**Impacto:** Cambios críticos en requests, validaciones y flujo de usuario.

---

## Cambios Críticos en Endpoints

### 1. POST `/api/RBAC/user-roles` - Asignar Rol a Usuario

**ANTES:**
```json
{
  "userId": 3,
  "roleId": 2,
  "assignedBy": 1
}
```

**AHORA (REQUERIDO):**
```json
{
  "userId": 3,
  "roleId": 2,
  "applicationId": 1,  // ⚠️ NUEVO: Campo requerido
  "assignedBy": 1,
  "expiresAt": null
}
```

**Errores Posibles:**
- `400 Bad Request`: "El usuario con ID X no tiene acceso a la aplicación con ID Y"
- `400 Bad Request`: "El rol con ID X no existe, está inactivo o no pertenece a la aplicación con ID Y"
- `400 Bad Request`: "El rol con ID X ya está asignado al usuario con ID Y en la aplicación con ID Z"

---

### 2. GET `/api/RBAC/user-roles` - Obtener Roles de Usuario

**ANTES:**
```
GET /api/RBAC/user-roles?userId=3&isActive=true
```

**AHORA (RECOMENDADO):**
```
GET /api/RBAC/user-roles?userId=3&isActive=true&applicationId=1
```

**Query Parameters:**
- `userId` (opcional): Filtrar por usuario
- `roleId` (opcional): Filtrar por rol
- `isActive` (opcional): Filtrar por estado activo (default: true)
- `applicationId` (opcional): **NUEVO** - Filtrar por aplicación

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "userRoleId": 1,
      "userId": 3,
      "roleId": 2,
      "applicationId": 1,  // ⚠️ NUEVO
      "assignedBy": 1,
      "assignedAt": "2025-11-11T10:25:14.720",
      "expiresAt": null,
      "isActive": true
    }
  ],
  "message": "1 rol(es) asignado(s) encontrado(s)"
}
```

---

### 3. DELETE `/api/RBAC/user-roles` - Remover Rol de Usuario

**ANTES:**
```
DELETE /api/RBAC/user-roles?userId=3&roleId=2
```

**AHORA (REQUERIDO):**
```
DELETE /api/RBAC/user-roles?userId=3&roleId=2&applicationId=1
```

**Query Parameters Requeridos:**
- `userId`: ID del usuario
- `roleId`: ID del rol
- `applicationId`: **NUEVO** - ID de la aplicación

---

## Flujo de Usuario Actualizado

### 1. Login
No hay cambios en el endpoint de login, pero se recomienda obtener las aplicaciones disponibles después del login exitoso.

### 2. Obtener Aplicaciones del Usuario (Recomendado)
Después del login, obtener las aplicaciones a las que el usuario tiene acceso:

```typescript
// Frontend - Después del login
const applications = await fetch('/api/users/{userId}/applications', {
  headers: { 'Authorization': `Bearer ${token}` }
});

// Respuesta esperada
{
  "data": [
    {
      "applicationId": 1,
      "applicationKey": "corporate",
      "applicationName": "Corporate"
    },
    {
      "applicationId": 2,
      "applicationKey": "brandpartner",
      "applicationName": "Brand Partner"
    }
  ]
}
```

### 3. Selección de Aplicación
El frontend debe:
1. Mostrar selector de aplicación si el usuario tiene múltiples aplicaciones
2. Guardar `selectedApplicationId` en localStorage/sessionStorage
3. Incluir `applicationId` en todas las requests de RBAC

```javascript
// Frontend - Guardar aplicación seleccionada
localStorage.setItem('selectedApplicationId', '1');

// Frontend - Obtener aplicación actual
const applicationId = parseInt(localStorage.getItem('selectedApplicationId'));
```

---

## Cambios en Componentes Frontend

### Componente: Asignar Rol a Usuario

**ANTES:**
```typescript
async function assignRoleToUser(userId: number, roleId: number) {
  const response = await fetch('/api/RBAC/user-roles', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      userId,
      roleId,
      assignedBy: currentUserId
    })
  });
  
  return response.json();
}
```

**AHORA (REQUERIDO):**
```typescript
async function assignRoleToUser(userId: number, roleId: number, applicationId: number) {
  const response = await fetch('/api/RBAC/user-roles', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      userId,
      roleId,
      applicationId,  // ⚠️ NUEVO: Campo requerido
      assignedBy: currentUserId,
      expiresAt: null
    })
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return response.json();
}

// Uso
const selectedAppId = parseInt(localStorage.getItem('selectedApplicationId'));
await assignRoleToUser(3, 2, selectedAppId);
```

---

### Componente: Listar Roles de Usuario

**ANTES:**
```typescript
async function getUserRoles(userId: number) {
  const response = await fetch(
    `/api/RBAC/user-roles?userId=${userId}&isActive=true`,
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  
  return response.json();
}
```

**AHORA (RECOMENDADO):**
```typescript
async function getUserRoles(userId: number, applicationId?: number) {
  const appId = applicationId || parseInt(localStorage.getItem('selectedApplicationId'));
  
  const params = new URLSearchParams({
    userId: userId.toString(),
    isActive: 'true',
    applicationId: appId.toString()  // ⚠️ NUEVO: Filtrar por aplicación
  });
  
  const response = await fetch(
    `/api/RBAC/user-roles?${params}`,
    {
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  
  return response.json();
}

// Uso
const roles = await getUserRoles(3);
```

---

### Componente: Remover Rol de Usuario

**ANTES:**
```typescript
async function removeRoleFromUser(userId: number, roleId: number) {
  const response = await fetch(
    `/api/RBAC/user-roles?userId=${userId}&roleId=${roleId}`,
    {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  
  return response.json();
}
```

**AHORA (REQUERIDO):**
```typescript
async function removeRoleFromUser(userId: number, roleId: number, applicationId: number) {
  const appId = applicationId || parseInt(localStorage.getItem('selectedApplicationId'));
  
  const params = new URLSearchParams({
    userId: userId.toString(),
    roleId: roleId.toString(),
    applicationId: appId.toString()  // ⚠️ NUEVO: Parámetro requerido
  });
  
  const response = await fetch(
    `/api/RBAC/user-roles?${params}`,
    {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    }
  );
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }
  
  return response.json();
}

// Uso
const selectedAppId = parseInt(localStorage.getItem('selectedApplicationId'));
await removeRoleFromUser(3, 2, selectedAppId);
```

---

## Validaciones Requeridas en Frontend

### 1. Validar Aplicación Seleccionada
Antes de hacer cualquier request de RBAC, validar que haya una aplicación seleccionada:

```typescript
function validateApplicationSelected(): number {
  const appId = localStorage.getItem('selectedApplicationId');
  
  if (!appId) {
    throw new Error('No hay una aplicación seleccionada. Por favor selecciona una aplicación.');
  }
  
  return parseInt(appId);
}

// Uso en componentes
try {
  const applicationId = validateApplicationSelected();
  await assignRoleToUser(userId, roleId, applicationId);
} catch (error) {
  showError(error.message);
}
```

### 2. Mostrar Aplicación Actual en UI
Agregar indicador visual de la aplicación activa:

```typescript
// React/Vue/Angular Component
function ApplicationSelector() {
  const [selectedApp, setSelectedApp] = useState(null);
  const [applications, setApplications] = useState([]);
  
  useEffect(() => {
    // Cargar aplicaciones al montar componente
    loadUserApplications();
  }, []);
  
  async function loadUserApplications() {
    const userId = getCurrentUserId();
    const response = await fetch(`/api/users/${userId}/applications`);
    const data = await response.json();
    setApplications(data.data);
    
    // Cargar aplicación guardada
    const savedAppId = localStorage.getItem('selectedApplicationId');
    if (savedAppId) {
      const app = data.data.find(a => a.applicationId === parseInt(savedAppId));
      setSelectedApp(app);
    }
  }
  
  function handleApplicationChange(app) {
    localStorage.setItem('selectedApplicationId', app.applicationId.toString());
    setSelectedApp(app);
    // Recargar datos si es necesario
    window.location.reload();
  }
  
  return (
    <div>
      <label>Aplicación actual:</label>
      <select value={selectedApp?.applicationId} onChange={(e) => {
        const app = applications.find(a => a.applicationId === parseInt(e.target.value));
        handleApplicationChange(app);
      }}>
        {applications.map(app => (
          <option key={app.applicationId} value={app.applicationId}>
            {app.applicationName}
          </option>
        ))}
      </select>
    </div>
  );
}
```

---

## Manejo de Errores

### Errores Comunes y Cómo Manejarlos

#### 1. Error 400: "El usuario no tiene acceso a esta aplicación"
```typescript
async function handleAssignRole(userId, roleId, applicationId) {
  try {
    await assignRoleToUser(userId, roleId, applicationId);
    showSuccess('Rol asignado exitosamente');
  } catch (error) {
    if (error.message.includes('no tiene acceso a esta aplicación')) {
      showError('El usuario no tiene acceso a esta aplicación. Por favor, asigna la aplicación al usuario primero.');
    } else {
      showError(error.message);
    }
  }
}
```

#### 2. Error 400: "El rol no pertenece a esta aplicación"
```typescript
// Validar en el frontend antes de enviar
async function getRolesForApplication(applicationId) {
  const response = await fetch(
    `/api/RBAC/roles?applicationId=${applicationId}&isActive=true`
  );
  return response.json();
}

// En el componente de asignar rol
const roles = await getRolesForApplication(selectedApplicationId);
// Mostrar solo roles de la aplicación actual
```

#### 3. Error 400: "El rol ya está asignado"
```typescript
async function handleAssignRole(userId, roleId, applicationId) {
  try {
    await assignRoleToUser(userId, roleId, applicationId);
    showSuccess('Rol asignado exitosamente');
  } catch (error) {
    if (error.message.includes('ya está asignado')) {
      showWarning('El usuario ya tiene este rol asignado en esta aplicación');
    } else {
      showError(error.message);
    }
  }
}
```

---

## Ejemplo Completo: Pantalla de Gestión de Roles

```typescript
// React Component
import { useState, useEffect } from 'react';

function UserRolesManagement({ userId }) {
  const [userRoles, setUserRoles] = useState([]);
  const [availableRoles, setAvailableRoles] = useState([]);
  const [selectedApplicationId, setSelectedApplicationId] = useState(null);
  const [applications, setApplications] = useState([]);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    loadApplications();
  }, []);
  
  useEffect(() => {
    if (selectedApplicationId) {
      loadUserRoles();
      loadAvailableRoles();
    }
  }, [selectedApplicationId]);
  
  async function loadApplications() {
    try {
      const response = await fetch(`/api/users/${userId}/applications`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
      });
      const data = await response.json();
      setApplications(data.data);
      
      // Cargar aplicación guardada o usar la primera
      const savedAppId = localStorage.getItem('selectedApplicationId');
      const appId = savedAppId 
        ? parseInt(savedAppId) 
        : data.data[0]?.applicationId;
      
      setSelectedApplicationId(appId);
    } catch (error) {
      showError('Error al cargar aplicaciones: ' + error.message);
    }
  }
  
  async function loadUserRoles() {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        userId: userId.toString(),
        isActive: 'true',
        applicationId: selectedApplicationId.toString()
      });
      
      const response = await fetch(`/api/RBAC/user-roles?${params}`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
      });
      
      if (response.ok) {
        const data = await response.json();
        setUserRoles(data.data || []);
      } else {
        setUserRoles([]);
      }
    } catch (error) {
      showError('Error al cargar roles: ' + error.message);
    } finally {
      setLoading(false);
    }
  }
  
  async function loadAvailableRoles() {
    try {
      const response = await fetch(
        `/api/RBAC/roles?applicationId=${selectedApplicationId}&isActive=true`,
        {
          headers: { 'Authorization': `Bearer ${getToken()}` }
        }
      );
      const data = await response.json();
      setAvailableRoles(data.data || []);
    } catch (error) {
      showError('Error al cargar roles disponibles: ' + error.message);
    }
  }
  
  async function handleAssignRole(roleId) {
    try {
      const response = await fetch('/api/RBAC/user-roles', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${getToken()}`
        },
        body: JSON.stringify({
          userId,
          roleId,
          applicationId: selectedApplicationId,
          assignedBy: getCurrentUserId(),
          expiresAt: null
        })
      });
      
      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message);
      }
      
      showSuccess('Rol asignado exitosamente');
      await loadUserRoles();
    } catch (error) {
      if (error.message.includes('no tiene acceso a esta aplicación')) {
        showError('El usuario no tiene acceso a esta aplicación');
      } else if (error.message.includes('ya está asignado')) {
        showWarning('El rol ya está asignado a este usuario');
      } else {
        showError('Error al asignar rol: ' + error.message);
      }
    }
  }
  
  async function handleRemoveRole(roleId) {
    try {
      const params = new URLSearchParams({
        userId: userId.toString(),
        roleId: roleId.toString(),
        applicationId: selectedApplicationId.toString()
      });
      
      const response = await fetch(`/api/RBAC/user-roles?${params}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${getToken()}` }
      });
      
      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message);
      }
      
      showSuccess('Rol removido exitosamente');
      await loadUserRoles();
    } catch (error) {
      showError('Error al remover rol: ' + error.message);
    }
  }
  
  function handleApplicationChange(appId) {
    setSelectedApplicationId(appId);
    localStorage.setItem('selectedApplicationId', appId.toString());
  }
  
  if (loading) return <div>Cargando...</div>;
  
  return (
    <div>
      <h2>Gestión de Roles de Usuario</h2>
      
      {/* Selector de Aplicación */}
      <div className="application-selector">
        <label>Aplicación:</label>
        <select 
          value={selectedApplicationId} 
          onChange={(e) => handleApplicationChange(parseInt(e.target.value))}
        >
          {applications.map(app => (
            <option key={app.applicationId} value={app.applicationId}>
              {app.applicationName}
            </option>
          ))}
        </select>
      </div>
      
      {/* Roles Actuales */}
      <div className="current-roles">
        <h3>Roles Asignados</h3>
        {userRoles.length === 0 ? (
          <p>No hay roles asignados en esta aplicación</p>
        ) : (
          <ul>
            {userRoles.map(userRole => (
              <li key={userRole.userRoleId}>
                Rol ID: {userRole.roleId} - 
                Asignado: {new Date(userRole.assignedAt).toLocaleDateString()}
                <button onClick={() => handleRemoveRole(userRole.roleId)}>
                  Remover
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
      
      {/* Asignar Nuevo Rol */}
      <div className="assign-role">
        <h3>Asignar Nuevo Rol</h3>
        <select onChange={(e) => e.target.value && handleAssignRole(parseInt(e.target.value))}>
          <option value="">Seleccionar rol...</option>
          {availableRoles
            .filter(role => !userRoles.some(ur => ur.roleId === role.roleId))
            .map(role => (
              <option key={role.roleId} value={role.roleId}>
                {role.roleName}
              </option>
            ))}
        </select>
      </div>
    </div>
  );
}

export default UserRolesManagement;
```

---

## Checklist de Implementación Frontend

### Fase 1: Configuración Inicial
- [ ] Agregar selector de aplicación en el layout principal
- [ ] Guardar `selectedApplicationId` en localStorage
- [ ] Crear función helper `getSelectedApplicationId()`
- [ ] Mostrar indicador visual de aplicación actual

### Fase 2: Actualizar Servicios/API Calls
- [ ] Actualizar `assignRoleToUser()` para incluir `applicationId`
- [ ] Actualizar `getUserRoles()` para incluir `applicationId` en query
- [ ] Actualizar `removeRoleFromUser()` para incluir `applicationId` en query
- [ ] Agregar validación de aplicación seleccionada antes de cada request

### Fase 3: Actualizar Componentes
- [ ] Componente de asignar rol: agregar parámetro `applicationId`
- [ ] Componente de listar roles: filtrar por `applicationId`
- [ ] Componente de remover rol: incluir `applicationId`
- [ ] Agregar manejo de errores específicos de multi-tenant

### Fase 4: Validaciones y UX
- [ ] Validar aplicación seleccionada antes de requests
- [ ] Mostrar mensaje claro si no hay aplicación seleccionada
- [ ] Agregar tooltips explicando que los roles son por aplicación
- [ ] Actualizar mensajes de error para mencionar la aplicación

### Fase 5: Testing
- [ ] Probar asignar rol con aplicación correcta
- [ ] Probar asignar rol sin aplicación (debe fallar)
- [ ] Probar asignar rol de aplicación incorrecta (debe fallar)
- [ ] Probar listar roles filtrados por aplicación
- [ ] Probar remover rol con aplicación correcta
- [ ] Probar cambio de aplicación y recarga de datos

---

## Notas Importantes

1. ⚠️ **`applicationId` es REQUERIDO** en POST y DELETE de user-roles
2. ⚠️ **Un usuario puede tener el mismo rol en diferentes aplicaciones**
3. ⚠️ **Siempre filtrar por `applicationId`** al listar roles de usuario
4. ⚠️ **Validar acceso a aplicación** antes de mostrar opciones de asignación
5. ⚠️ **El constraint único ahora es (UserId, RoleId, ApplicationId)**, no solo (UserId, RoleId)

---

## Soporte y Documentación Adicional

- Backend: `CAMBIOS_BACKEND_MULTITENANT.md`
- Migraciones BD: `MULTITENANT_APPLICATIONID_CHANGES.md`

---

**Fecha:** Noviembre 2025  
**Versión Backend:** Multi-Tenant con ApplicationId

