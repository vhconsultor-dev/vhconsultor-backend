# Administración de permisos — Guía completa (APIs + qué pedir al Front)

**Base de APIs:** `https://vh-apimanagement.azure-api.net/shared-vh/api/RBAC`  
**Autenticación:** Todas llevan token (header Authorization).

---

## 1. Cómo hablar (sin tecnicismos)

| En la base de datos dice | En pantalla debe decir | Qué es en la vida real |
|--------------------------|------------------------|-------------------------|
| Application | **Aplicación** | Corporate o Brand Partner |
| Resource | **Pantalla** | Una opción del menú (Inventory, Roles, Contratos…) |
| Action | **Botón / acción** | Search, Create, Edit, etc. |
| Permission | **Permiso** | “En esta pantalla, ¿puede usar este botón?” |
| Role | **Rol** | Perfil (Admin, Manager, Operador…) |
| RolePermission | *(no se muestra)* | Qué permisos tiene el rol |
| UserRole | *(no se muestra)* | Qué rol tiene el usuario |

**Los APIs siguen diciendo `resources` y `actions` en la URL.** En el front y en textos para el usuario solo digan **Pantallas** y **Botones**.

---

## 2. Lo que ya tienes vs lo que falta

En **Settings** hoy ves algo como:

- Corporate Users / Brand Partner Users  
- **Roles** ✅  
- **Permissions** ✅  

**Falta en el menú (pero el backend ya lo tiene):**

- **Pantallas** (API: `resources`) — crear y listar pantallas por aplicación  
- **Botones** (API: `actions`) — crear y listar botones reutilizables  

Sin pantallas y botones, crear permisos a mano es confuso porque no se ve el árbol completo.

---

## 3. El ciclo completo (de principio a fin)

Orden obligatorio. Si lo reviertes, algo no cuadra.

```
PASO 1  Elegir aplicación          →  Corporate (1) o Brand Partner (2)
PASO 2  Crear PANTALLA             →  ej. "Inventory Management"
PASO 3  Crear BOTONES             →  ej. Search, Create, Edit…
PASO 4  Crear PERMISOS             →  unir pantalla + cada botón
PASO 5  Crear ROL                  →  ej. "Inventory Manager"
PASO 6  Marcar permisos al ROL     →  árbol con ☑
PASO 7  Asignar ROL al USUARIO     →  Juan es Inventory Manager en Brand Partner
```

**Resultado:** Juan entra a Brand Partner, ve solo las pantallas y botones que su rol permite.

---

## 4. Formato de todas las respuestas

Todas las respuestas vienen parecidas:

**Éxito (200):**
```json
{
  "status": true,
  "statusCode": 200,
  "data": { ... },
  "message": "Texto descriptivo"
}
```

**Error de negocio (400):**
```json
{
  "status": false,
  "statusCode": 400,
  "data": null,
  "message": "Explicación del problema"
}
```

**No encontrado (404):**
```json
{
  "status": false,
  "statusCode": 404,
  "data": null,
  "message": "No se encontraron ..."
}
```

---

## 5. APIs — Aplicaciones

### GET `/applications`

**Para qué:** Llenar el selector “¿Corporate o Brand Partner?”

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| isActive | bool | Default `true` |

**data de ejemplo:**
```json
[
  { "applicationId": 1, "applicationName": "Corporate", "applicationKey": "corporate" },
  { "applicationId": 2, "applicationName": "Brand Partner", "applicationKey": "brandpartner" }
]
```

---

## 6. APIs — PANTALLAS (en API: `resources`)

### GET `/resources`

**Para qué:** Listar pantallas de una aplicación.

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| applicationKey | string | `corporate` o `brandpartner` |
| resourceId | int | Una pantalla específica |
| resourceKey | string | Ej. `amazon_inventory` |
| isActive | bool | Default `true` |

**data de ejemplo:**
```json
[
  {
    "resourceId": 25,
    "applicationId": 2,
    "resourceName": "Inventory Management",
    "resourceKey": "amazon_inventory",
    "description": "Inventario Amazon Seller",
    "module": "Amazon Seller",
    "isActive": true
  }
]
```

---

### POST `/resources`

**Para qué:** Crear una pantalla nueva.

**Query opcional:** `applicationKey=brandpartner` (si en el body no mandas applicationId).

**Body:**
```json
{
  "applicationId": 2,
  "resourceName": "Inventory Management",
  "resourceKey": "amazon_inventory",
  "description": "Pantalla de inventario",
  "module": "Amazon Seller"
}
```

**data de respuesta:**
```json
{ "resourceId": 25 }
```

**message:** `Recurso creado exitosamente` (en UI mostrar: “Pantalla creada”).

---

### PUT `/resources/{resourceId}`

**Para qué:** Editar nombre, descripción, módulo, activo/inactivo.

**Body:** mismos campos que al crear (sin cambiar applicationId si no es necesario).

---

### DELETE `/resources/{resourceId}`

**Para qué:** Desactivar la pantalla (no borra físico, marca inactiva).

---

## 7. APIs — BOTONES (en API: `actions`)

Los botones **no** llevan applicationId. Son catálogo general.  
Se “enganchan” a una aplicación cuando creas el **permiso** sobre una **pantalla** de esa app.

### GET `/actions`

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| actionId | int | Un botón específico |
| actionKey | string | Ej. `search`, `load_inventory` |
| isActive | bool | Default `true` |

**data de ejemplo:**
```json
[
  {
    "actionId": 10,
    "actionName": "Search",
    "actionKey": "search",
    "description": "Buscar en la pantalla",
    "isActive": true
  }
]
```

---

### POST `/actions`

**Para qué:** Crear un botón nuevo (si no existe `search`, `create`, etc.).

**Body:**
```json
{
  "actionName": "Load Inventory",
  "actionKey": "load_inventory",
  "description": "Cargar inventario desde archivo"
}
```

**data de respuesta:**
```json
{ "actionId": 15 }
```

---

## 8. APIs — PERMISOS (pantalla + botón)

### GET `/permissions`

**Para qué:** Ver todos los permisos de una pantalla (armar el árbol).

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| applicationKey | string | `brandpartner` |
| applicationId | int | `2` |
| resourceId | int | ID de la pantalla |
| isActive | bool | Default `true` |

**data de ejemplo:**
```json
[
  {
    "permissionId": 101,
    "applicationId": 2,
    "resourceId": 25,
    "actionId": 10,
    "permissionName": "Inventory Management · Search",
    "permissionKey": "amazon_inventory.search",
    "isActive": true
  }
]
```

El **permissionKey** es lo que el front usa para mostrar u ocultar botones.

---

### POST `/permissions`

**Para qué:** Crear un permiso = pantalla + botón.

**Query opcional:** `applicationKey=brandpartner`

**Body:**
```json
{
  "applicationId": 2,
  "resourceId": 25,
  "actionId": 10,
  "permissionName": "",
  "permissionKey": ""
}
```

Si `permissionName` y `permissionKey` van **vacíos**, el sistema arma:
- key: `amazon_inventory.search`
- name: `Inventory Management · Search`

**data de respuesta:**
```json
{
  "permissionId": 101,
  "permissionKey": "amazon_inventory.search"
}
```

---

### PUT `/permissions/{permissionId}`

**Para qué:** Cambiar nombre, descripción o reactivar.

### DELETE `/permissions/{permissionId}`

**Para qué:** Desactivar un permiso.

---

## 9. APIs — ROLES

### GET `/roles`

| Parámetro | Tipo |
|-----------|------|
| applicationKey | `corporate` / `brandpartner` |
| roleId | int |
| isActive | bool |

### POST `/roles`

**Query opcional:** `applicationKey=brandpartner`

**Body:**
```json
{
  "applicationId": 2,
  "roleName": "Inventory Manager",
  "roleKey": "inventory_manager",
  "description": "Gestiona inventario",
  "isSystemRole": false
}
```

**data:** `{ "roleId": 5 }`

### PUT `/roles/{roleId}` — editar  
### DELETE `/roles/{roleId}` — desactivar  

---

## 10. APIs — Unir ROL con PERMISOS

### GET `/role-permissions`

**Para qué:** Saber qué permisos ya tiene marcados un rol.

| Parámetro | Tipo |
|-----------|------|
| roleId | int |
| applicationKey | string |

### POST `/role-permissions`

**Para qué:** Marcar un ☑ en el árbol (asignar un permiso al rol).

**Body:**
```json
{
  "roleId": 5,
  "permissionId": 101,
  "grantedBy": 1
}
```

**data:** `{ "rolePermissionId": 200 }`

**Error típico (400):** rol y permiso de aplicaciones distintas.

### DELETE `/role-permissions?roleId=5&permissionId=101`

**Para qué:** Quitar un ☑ del rol.

---

## 11. APIs — USUARIO con ROL

### GET `/user-roles`

| Parámetro | Tipo |
|-----------|------|
| userId | int |
| applicationId | int (1 o 2) |

### POST `/user-roles`

**Para qué:** Decir “este usuario es Inventory Manager en Brand Partner”.

**Body:**
```json
{
  "userId": 25,
  "roleId": 5,
  "applicationId": 2,
  "assignedBy": 1,
  "expiresAt": null
}
```

**data:** `{ "userRoleId": 89 }`

### DELETE `/user-roles?userId=25&roleId=5`

**Para qué:** Quitar el rol al usuario.

---

## 12. API útil — ¿Qué puede hacer el usuario al final?

### GET `/user-effective-permissions/{userId}?applicationKey=brandpartner`

**Para qué:** Lista final de `permissionKey` que el front debe respetar (menú + botones).

**data de ejemplo:**
```json
[
  {
    "permissionKey": "amazon_inventory.page_view",
    "permissionName": "Inventory Management · Ver pantalla"
  },
  {
    "permissionKey": "amazon_inventory.search",
    "permissionName": "Inventory Management · Search"
  }
]
```

---

## 13. Ejemplo real — Inventory en Brand Partner

**Pantalla:** Inventory Management → key `amazon_inventory` → applicationId `2`

**Botones a crear (actions) y luego permisos:**

| Botón en UI | actionKey |
|-------------|-----------|
| Ver en menú | page_view |
| Search | search |
| Refresh | refresh |
| Create | create |
| Load Inventory | load_inventory |
| Bulk Update | bulk_update |
| Settlement List | settlement_list |
| Edit (grid) | edit |
| Adjust (grid) | adjust |
| Moves (grid) | moves |

**Árbol que debe verse en administración:**

```
📁 Inventory Management
   ☑ Ver en menú
   ☑ Search
   ☑ Refresh
   ☑ Create
   ☑ Load Inventory
   ☑ Bulk Update
   ☑ Settlement List
   ☑ Edit
   ☑ Adjust
   ☑ Moves
```

---

# PROMPT PARA EL EQUIPO DE FRONT (sin código)

Copia esta sección y úsala como requerimiento de producto.

---

## Objetivo

Construir administración de permisos **tan simple que no haga falta explicar qué es un “resource”**.  
El usuario solo ve: **Aplicación → Pantallas → Botones → Roles → Usuarios**.

---

## Menú sugerido (dentro de Settings)

Hoy tienen **Roles** y **Permissions**. Agregar:

| Opción en menú | Qué hace |
|----------------|----------|
| **Aplicaciones** | Solo lectura: Corporate / Brand Partner (selector global) |
| **Pantallas** | Crear y listar pantallas por aplicación |
| **Botones** | Catálogo de botones/acciones (crear si no existe) |
| **Permisos por pantalla** | Árbol: una pantalla y todos sus permisos |
| **Roles** | (ya existe) + al editar rol, mostrar el **árbol de permisos** |
| **Asignar roles a usuario** | Desde Corporate Users o Brand Partner Users |

Nunca mostrar la palabra **Resource** al usuario. Siempre **Pantalla**.

---

## Pantalla 1 — Selector de aplicación (siempre visible arriba)

- Dos opciones claras: **Corporate** | **Brand Partner**
- Tooltip: *“Los permisos de Corporate no mezclan con Brand Partner.”*
- Todo lo que se liste abajo filtra por la aplicación elegida.

---

## Pantalla 2 — Pantallas

**Lista:** nombre legible, módulo (ej. Amazon Seller), key técnica pequeña (`amazon_inventory`).

**Botón:** “Nueva pantalla”

**Formulario nueva pantalla:**
- Nombre (ej. Inventory Management) — obligatorio  
- Clave (ej. amazon_inventory) — obligatorio, sin espacios  
- Módulo / sección del menú (ej. Amazon Seller)  
- Descripción corta (tooltip en admin)

**Después de crear:** mensaje claro + botón **“Configurar botones de esta pantalla”** que lleva a la pantalla 4.

---

## Pantalla 3 — Botones (catálogo)

**Lista** de todos los botones: Search, Create, Edit…

**Botón:** “Nuevo botón”

**Formulario:**
- Nombre visible (Search)  
- Clave (search) — sin espacios  
- Descripción (para tooltip)

**Nota en UI:** *“Los botones se reutilizan en varias pantallas. El permiso se arma al unir pantalla + botón.”*

---

## Pantalla 4 — Permisos de una pantalla (la más importante)

**Flujo:**
1. Elegir aplicación  
2. Elegir **una pantalla** de un dropdown  
3. Ver **árbol** con todos los permisos de esa pantalla solamente  

**Botón:** “Agregar botón a esta pantalla”  
- Muestra lista de botones del catálogo (o crear uno nuevo en modal)  
- Al guardar, llama crear permiso (pantalla + botón)  
- El nuevo ítem aparece en el árbol  

**Cada línea del árbol muestra:**
- Nombre amigable (Search)  
- Clave técnica pequeña (`amazon_inventory.search`)  
- Estado activo/inactivo  

**No** listar permisos de otras pantallas en la misma vista.

---

## Pantalla 5 — Roles (mejorar la actual)

Al **crear o editar un rol:**
1. Nombre del rol  
2. Aplicación (Corporate o Brand Partner) — bloqueada si el rol ya tiene permisos  
3. **Árbol de permisos** agrupado por pantalla:

```
📁 Inventory Management
   ☑ Search
   ☑ Create
📁 Settlement List
   ☑ Ver en menú
```

- Marcar/desmarcar = asignar o quitar permiso al rol  
- Agrupar siempre por **pantalla**, nunca lista plana de 200 permisos  

**Tooltip en rol:** *“Un rol es un paquete de permisos. El usuario hereda lo que el rol tenga marcado.”*

---

## Pantalla 6 — Usuario y roles

Desde **Corporate Users** o **Brand Partner Users**:

- Ver roles actuales del usuario en **esa** aplicación  
- Botón “Asignar rol” → dropdown solo roles de esa aplicación  
- Opcional: fecha de expiración  
- Botón quitar rol  

**Tooltip:** *“El usuario puede tener varios roles. Sus permisos son la unión de todos los roles (menos denegaciones).”*

---

## Reglas de experiencia (obligatorias)

1. **Siempre** filtrar por aplicación antes de pantallas, permisos y roles.  
2. **Nunca** mezclar permisos de Corporate y Brand Partner en un mismo árbol.  
3. Usar palabras: Aplicación, Pantalla, Botón, Permiso, Rol.  
4. Mostrar `permissionKey` solo como texto secundario (gris, pequeño), no como título.  
5. Después de crear pantalla, guiar al usuario: *“Ahora agrega los botones de esta pantalla.”*  
6. Al asignar rol, mostrar resumen: *“Este rol tendrá X permisos en Y pantallas.”*  
7. En la app real (Inventory, etc.), ocultar botones si el usuario no tiene el `permissionKey` correspondiente.

---

## Orden de implementación sugerido para front

1. Selector de aplicación + listar/crear **Pantallas**  
2. Listar/crear **Botones**  
3. **Permisos por pantalla** (árbol + agregar botón)  
4. Mejorar **Roles** con árbol por pantalla  
5. **Asignar rol a usuario** por aplicación  
6. En cada pantalla de negocio, validar `permissionKey` contra permisos efectivos del usuario  

---

## Checklist para validar que quedó bien

- [ ] Puedo crear “Inventory Management” en Brand Partner sin saber qué es un resource  
- [ ] Puedo agregar Search y Create solo a esa pantalla  
- [ ] Veo un árbol solo de Inventory, no de Contratos  
- [ ] Creo rol “Manager” y marco ☑ en el árbol  
- [ ] Asigno ese rol a un usuario de Brand Partner  
- [ ] Ese usuario solo ve los botones marcados en Inventory  

---

*Documento para administración RBAC — backend `shared-vh/api/RBAC`. En UI: Pantalla = API `resources`, Botón = API `actions`.*
