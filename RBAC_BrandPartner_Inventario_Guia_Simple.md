# RBAC — Guía súper simple (Brand Partner)

> Para la pantalla **Inventory Management** (Amazon Seller).  
> Aplicación: **Brand Partner** → `applicationId = 2` → `applicationKey = brandpartner`

---

## ¿Qué es RBAC en una frase?

Es la forma de decir **quién puede hacer qué** en cada pantalla del sistema.

---

## Las tablas (qué guarda cada una)

Piensa en esto como armar un rompecabezas de 6 piezas:

| Tabla | Para qué sirve | Ejemplo mental |
|-------|----------------|----------------|
| **Applications** | ¿De qué app es? | Brand Partner (2) o Corporate (1) |
| **Resources** | ¿Qué pantalla o módulo? | “Inventory Management” |
| **Actions** | ¿Qué botón o cosa se puede hacer? | “Search”, “Create”, “Edit” |
| **Permissions** | Une **pantalla + acción** = un permiso concreto | `amazon_inventory.search` |
| **Roles** | Un “perfil” (Manager, Operador, etc.) | Rol “Inventory Operator” |
| **RolePermissions** | Qué permisos tiene ese rol | El rol puede Search y Edit, pero no Create |
| **UserRoles** | Qué rol tiene cada usuario | Juan tiene el rol “Inventory Operator” |

**Regla de oro:**  
Un permiso = **1 pantalla** + **1 acción**.  
Si el usuario no tiene ese permiso, **no se muestra el botón** (o el API responde sin acceso).

---

## Cómo se ve en árbol (lo que quieres en administración)

```
📁 Inventory Management (amazon_inventory)     ← Resource
   ☑ Entrar a la pantalla      (page_view)
   ☑ Search
   ☑ Refresh
   ☑ Create
   ☑ Load Inventory
   ☑ Bulk Update
   ☑ Settlement List
   ☑ Edit          (por fila del grid)
   ☑ Adjust        (por fila del grid)
   ☑ Moves         (por fila del grid)
```

Cada línea con ☑ es **un Permission** distinto.

---

## Paso a paso: crear esta pantalla en Brand Partner

Hazlo **en este orden**. Si saltas un paso, lo siguiente no cuadra.

### Paso 1 — Crear el Resource (la pantalla)

Es **solo una vez** por pantalla.

| Campo | Valor sugerido |
|-------|----------------|
| applicationId | `2` |
| resourceName | `Inventory Management` |
| resourceKey | `amazon_inventory` |
| module | `Amazon Seller` |
| description | Inventario Amazon Seller |

**API (ejemplo):**  
`POST /api/RBAC/resources?applicationKey=brandpartner`

```json
{
  "applicationId": 2,
  "resourceName": "Inventory Management",
  "resourceKey": "amazon_inventory",
  "module": "Amazon Seller",
  "description": "Pantalla de inventario Amazon Seller"
}
```

Anota el **resourceId** que te devuelve (ejemplo: `25`).

---

### Paso 2 — Crear las Actions (los botones)

Las **Actions** son globales: sirven para Corporate y Brand Partner.  
Si ya existen (por ejemplo `search`, `create`), **no las vuelvas a crear**; solo úsalas en el paso 3.

Crea **una Action por cada botón** de esta pantalla:

| Botón en pantalla | actionName | actionKey |
|-------------------|------------|-----------|
| Entrar / ver pantalla | Ver pantalla | `page_view` |
| Search | Search | `search` |
| Refresh | Refresh | `refresh` |
| Create | Create | `create` |
| Load Inventory | Load Inventory | `load_inventory` |
| Bulk Update | Bulk Update | `bulk_update` |
| Settlement List | Settlement List | `settlement_list` |
| Edit (grid) | Edit | `edit` |
| Adjust (grid) | Adjust | `adjust` |
| Moves (grid) | Moves | `moves` |

**API (ejemplo por cada una nueva):**  
`POST /api/RBAC/actions`

```json
{
  "actionName": "Load Inventory",
  "actionKey": "load_inventory",
  "description": "Cargar inventario"
}
```

Anota el **actionId** de cada una (o búscalas con `GET /api/RBAC/actions?actionKey=search`).

---

### Paso 3 — Crear los Permissions (uno por cada ☑ del árbol)

Aquí juntas **Resource + Action**.

Por cada fila de la tabla del paso 2:

**API:**  
`POST /api/RBAC/permissions?applicationKey=brandpartner`

```json
{
  "applicationId": 2,
  "resourceId": 25,
  "actionId": 8,
  "permissionName": "",
  "permissionKey": ""
}
```

- Pon el **resourceId** de tu pantalla (`amazon_inventory`).
- Pon el **actionId** de esa acción.
- Si dejas `permissionKey` y `permissionName` **vacíos**, el sistema arma algo como:  
  `amazon_inventory.search` y nombre `Inventory Management · Search`.

**Lista completa de permisos que debes tener al final:**

| # | permissionKey (resultado) | Para qué sirve |
|---|---------------------------|----------------|
| 1 | `amazon_inventory.page_view` | Ver la pantalla en el menú |
| 2 | `amazon_inventory.search` | Botón Search |
| 3 | `amazon_inventory.refresh` | Botón Refresh |
| 4 | `amazon_inventory.create` | Botón Create |
| 5 | `amazon_inventory.load_inventory` | Botón Load Inventory |
| 6 | `amazon_inventory.bulk_update` | Botón Bulk Update |
| 7 | `amazon_inventory.settlement_list` | Botón Settlement List |
| 8 | `amazon_inventory.edit` | Botón Edit en cada fila |
| 9 | `amazon_inventory.adjust` | Botón Adjust en cada fila |
| 10 | `amazon_inventory.moves` | Botón Moves en cada fila |

**Verificar:**  
`GET /api/RBAC/permissions?applicationKey=brandpartner&resourceId=25`  
Debes ver **10 permisos** activos.

---

### Paso 4 — Crear un Role y marcarle permisos

**4a. Crear el rol** (solo Brand Partner)

`POST /api/RBAC/roles?applicationKey=brandpartner`

```json
{
  "applicationId": 2,
  "roleName": "Inventory Operator",
  "roleKey": "inventory_operator",
  "description": "Opera inventario Amazon",
  "isSystemRole": false
}
```

**4b. Asignar permisos al rol** (repite por cada permissionId)

`POST /api/RBAC/role-permissions`

```json
{
  "roleId": 5,
  "permissionId": 101,
  "grantedBy": 1
}
```

En la pantalla de administración sería: abrir el árbol de `amazon_inventory` y marcar los ☑ que ese rol debe tener.

---

### Paso 5 — Asignar el rol al usuario

`POST /api/RBAC/user-roles`

```json
{
  "userId": 25,
  "roleId": 5,
  "applicationId": 2,
  "assignedBy": 1
}
```

Listo: ese usuario ya tiene solo los botones que el rol permite.

---

## Cómo usa el front cada permiso (idea simple)

| Si el usuario tiene… | El front… |
|----------------------|-----------|
| `amazon_inventory.page_view` | Muestra el menú “Inventory Management” |
| `amazon_inventory.search` | Muestra el botón Search |
| `amazon_inventory.create` | Muestra el botón Create (naranja +) |
| `amazon_inventory.edit` | Muestra “Edit” en cada fila |
| No tiene `amazon_inventory.adjust` | **Oculta** “Adjust” en el grid |

Para saber qué tiene un usuario:

`GET /api/RBAC/user-effective-permissions/{userId}?applicationKey=brandpartner`

Ahí viene la lista de `permissionKey` que debe usar el front.

---

## Errores comunes (evítalos)

1. **Crear el Resource en Corporate (1) en vez de Brand Partner (2).**  
   → Los permisos quedarían en la app equivocada.

2. **Olvidar `page_view`.**  
   → El usuario tiene botones pero no debería entrar a la pantalla (o al revés).

3. **Poner el nombre del botón en el Resource.**  
   → El Resource es **la pantalla**, no cada botón. Los botones son **Actions**.

4. **Asignar un permiso de Brand Partner a un rol de Corporate.**  
   → El API debe rechazarlo: rol y permiso deben ser de la **misma** aplicación.

---

## Resumen en 4 líneas

1. **Resource** = la pantalla (`amazon_inventory`).  
2. **Action** = cada botón (`search`, `create`, `edit`…).  
3. **Permission** = pantalla + botón (`amazon_inventory.search`).  
4. **Role** + **UserRole** = qué usuario puede marcar qué ☑ en el árbol.

---

## Checklist rápido para Inventory Management

- [ ] Resource `amazon_inventory` con `applicationId = 2`
- [ ] 10 Actions (o reutilizar las que ya existan)
- [ ] 10 Permissions ligados a ese Resource
- [ ] Rol creado en Brand Partner
- [ ] Permisos marcados en el rol según el perfil
- [ ] Rol asignado al usuario con `applicationId = 2`

Si tienes dudas, pregunta: **“¿esto es Resource, Action o Permission?”**  
Casi siempre: pantalla = Resource, botón = Action, checkbox del árbol = Permission.
