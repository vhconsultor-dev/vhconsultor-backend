# Estructura: Usuarios Brand Partner vinculados a Customer (Corporate)

## Objetivo

Tener una **tabla de usuarios exclusiva para Brand Partner** (clientes de VHConsultor: Amig, Cachito, etc.) que:

- Controle el ingreso de Brand Partner de forma **independiente** del sistema de usuarios corporativos (empleados administrativos de VHConsultor).
- Vincule cada usuario a una **compañía** que debe ser estrictamente un **Customer** en Corporate (FK a `Corporate.Customers`).
- **Login siempre con correo** (email); no se usa username para el acceso.
- Registre **nombre, apellidos y datos adicionales** de la persona.
- Permita **forzar cambio de contraseña** en el próximo inicio de sesión (ej. tras restablecer con contraseña temporal).
- Mantenga **historial de inicio de sesión** por usuario.

## Esquema actual (resumen)

- **Global.Users**: usuarios corporativos o brand partner (`IsCorporate`, `IsBrandPartner`).
- **Corporate.Customers**: clientes (Amig, Cachito, etc.) con `CustomerId`, `CompanyName`, `NIT`, etc.

## Propuesta: esquema BrandPartner

### Esquema

- **BrandPartner**: esquema para acceso/usuarios de clientes (Brand Partner).
- **Corporate**: sin cambios; `Customers` es la fuente de verdad de “compañía cliente”.

---

## Tabla: BrandPartner.BrandPartnerUsers

Usuarios que inician sesión en el portal/API de Brand Partner **siempre con su correo**. Cada fila es un usuario de **un solo** Customer (compañía). Se guardan nombre, apellidos y datos de la persona.

| Columna | Tipo | Nullable | Descripción |
|---------|------|----------|-------------|
| **BrandPartnerUserId** | int | NO | PK, IDENTITY(1,1). |
| **CustomerId** | int | NO | FK → Corporate.Customers(CustomerId). La compañía a la que pertenece el usuario. |
| **Email** | nvarchar(255) | NO | **Login:** siempre se usa el correo para iniciar sesión. Único por Customer. |
| **PasswordHash** | nvarchar(500) | NO | Hash de contraseña (mismo criterio que Global.Users). |
| **FirstName** | nvarchar(100) | NO | Nombre de la persona. |
| **LastName** | nvarchar(100) | NO | Apellidos de la persona. |
| **PhoneNumber** | nvarchar(50) | SÍ | Teléfono. |
| **IsActive** | bit | NO | Default 1. Permite desactivar sin borrar. |
| **EmailVerified** | bit | NO | Default 0. |
| **RequirePasswordChangeOnNextLogin** | bit | NO | Default 0. Si 1, al entrar debe cambiar la contraseña (ej. tras restablecer con temporal). |
| **FailedLoginAttempts** | int | NO | Default 0. Para bloqueo por intentos. |
| **LockedUntil** | datetime2 | SÍ | Bloqueo temporal. |
| **LastLogin** | datetime2 | SÍ | Último inicio de sesión. |
| **LastLoginIP** | nvarchar(45) | SÍ | IP del último login. |
| **LastLoginUserAgent** | nvarchar(500) | SÍ | User-Agent del último login. |
| **CreatedAt** | datetime2 | NO | Alta del usuario. |
| **UpdatedAt** | datetime2 | SÍ | Última actualización. |
| **CreatedBy** | nvarchar(255) | SÍ | Usuario/rol corporativo que lo creó. |

### Restricciones e índices (BrandPartnerUsers)

- **FK:** `CustomerId` → `Corporate.Customers(CustomerId)`.
- **UNIQUE (CustomerId, Email):** un mismo correo no puede repetirse dentro del mismo Customer; sí en otro Customer.
- **Índice** en `CustomerId` para listar usuarios por cliente.
- **Índice** en `Email` (con INCLUDE de campos necesarios para login) para autenticación por correo.

### Comportamiento del bit RequirePasswordChangeOnNextLogin

- Al crear usuario con contraseña temporal o al ejecutar “restablecer contraseña” con temporal: poner **RequirePasswordChangeOnNextLogin = 1**.
- En el primer login exitoso tras eso, el backend exige cambio de contraseña (ej. endpoint obligatorio o redirect en front).
- Tras cambiar la contraseña correctamente: poner **RequirePasswordChangeOnNextLogin = 0** y actualizar **PasswordHash**.

---

## Tabla: BrandPartner.BrandPartnerUserLoginHistory

Historial de intentos de inicio de sesión de cada usuario Brand Partner (exitosos y fallidos).

| Columna | Tipo | Nullable | Descripción |
|---------|------|----------|-------------|
| **LoginHistoryId** | int | NO | PK, IDENTITY(1,1). |
| **BrandPartnerUserId** | int | NO | FK → BrandPartner.BrandPartnerUsers(BrandPartnerUserId). |
| **LoginDate** | datetime2 | NO | Fecha/hora del intento. |
| **IPAddress** | nvarchar(45) | NO | IP desde la que se intentó. |
| **UserAgent** | nvarchar(500) | SÍ | User-Agent del navegador/cliente. |
| **Location** | nvarchar(255) | SÍ | Ubicación si se obtiene (ej. geolocalización). |
| **Country** | nvarchar(100) | SÍ | País. |
| **City** | nvarchar(100) | SÍ | Ciudad. |
| **LoginSuccessful** | bit | NO | 1 = login exitoso, 0 = fallido. |
| **FailureReason** | nvarchar(255) | SÍ | Motivo del fallo si LoginSuccessful = 0 (ej. contraseña incorrecta, cuenta bloqueada). |
| **SessionId** | nvarchar(255) | SÍ | Identificador de sesión si aplica. |

### Restricciones e índices (LoginHistory)

- **FK:** `BrandPartnerUserId` → `BrandPartner.BrandPartnerUsers(BrandPartnerUserId)`.
- **Índice** en `BrandPartnerUserId` para consultar historial por usuario.
- **Índice** en `LoginDate` (o (BrandPartnerUserId, LoginDate)) para listados y auditoría.

---

## Relación con Corporate

- **Corporate.Customers** = compañías cliente (Amig, Cachito, etc.).
- **BrandPartner.BrandPartnerUsers** = usuarios que pertenecen a una de esas compañías; **login siempre por Email**.
- **BrandPartner.BrandPartnerUserLoginHistory** = historial de inicios de sesión de cada usuario Brand Partner.

### Diagrama conceptual

```
[Corporate.Customers] 1 ──────────< N [BrandPartner.BrandPartnerUsers]
       CustomerId                      CustomerId (FK)
       CompanyName                     Email (login), FirstName, LastName,
       NIT, ...                       PasswordHash, RequirePasswordChangeOnNextLogin, ...

                                       [BrandPartner.BrandPartnerUsers] 1 ───< N [BrandPartner.BrandPartnerUserLoginHistory]
                                            BrandPartnerUserId                    BrandPartnerUserId (FK)
                                                                                   LoginDate, IPAddress, LoginSuccessful, ...
```

---

## Resumen para implementación

1. Crear esquema **BrandPartner** si no existe.
2. Crear tabla **BrandPartner.BrandPartnerUsers** (sin Username; login por **Email**; **FirstName**, **LastName** obligatorios; **RequirePasswordChangeOnNextLogin**).
3. Crear tabla **BrandPartner.BrandPartnerUserLoginHistory** y FK a BrandPartnerUsers.
4. FK de `BrandPartnerUsers.CustomerId` a `Corporate.Customers(CustomerId)`.
5. UNIQUE (CustomerId, Email) en BrandPartnerUsers.
6. En login Brand Partner: validar por **Email** (+ CustomerId si hay multi-tenant); si login exitoso, insertar fila en LoginHistory y, si RequirePasswordChangeOnNextLogin = 1, exigir cambio de contraseña antes de continuar.
7. En “restablecer contraseña” con temporal: poner RequirePasswordChangeOnNextLogin = 1 hasta que el usuario cambie la contraseña.

Con esto se controla el ingreso de Brand Partner de forma independiente, con login por correo, datos de la persona, obligación de cambiar contraseña cuando corresponda e historial de sesiones.
