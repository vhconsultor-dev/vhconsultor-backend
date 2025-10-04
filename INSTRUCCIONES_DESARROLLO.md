# Instrucciones de Desarrollo - VHConsultor Backend

## 📋 Índice
1. [Arquitectura del Proyecto](#arquitectura-del-proyecto)
2. [Estructura de Capas](#estructura-de-capas)
3. [Patrón CQRS](#patrón-cqrs)
4. [FluentValidation](#fluentvalidation)
5. [Entity Framework vs Dapper](#entity-framework-vs-dapper)
6. [Guías de Desarrollo](#guías-de-desarrollo)
7. [Configuración y Setup](#configuración-y-setup)
8. [Mejores Prácticas](#mejores-prácticas)

---

## 🏗️ Arquitectura del Proyecto

El proyecto **VHConsultor Backend** está construido con una arquitectura de **4 capas** siguiendo principios de **Clean Architecture** y **Domain-Driven Design (DDD)**:

```
VHConsultor-Backend/
├── ApiLayer/           # Capa de presentación (Controllers, Middleware)
├── ApplicationLayer/   # Capa de aplicación (Services, DTOs, Validators)
├── BusinessLayer/      # Capa de lógica de negocio (Repositories, Queries, Commands)
└── ModelLayer/         # Capa de datos (Entities, DbContext, Configurations)
```

### Dependencias entre Capas
```
ApiLayer → ApplicationLayer → BusinessLayer → ModelLayer
```

---

## 🏛️ Estructura de Capas

### 1. **ApiLayer** (Capa de Presentación)
- **Responsabilidad**: Exposición de APIs REST, manejo de HTTP, autenticación
- **Componentes**:
  - `Controllers/` - Controladores de API
  - `Tools/` - Middleware, ResponseStructure, ExceptionHandlers
  - `Program.cs` - Configuración de la aplicación

### 2. **ApplicationLayer** (Capa de Aplicación)
- **Responsabilidad**: Orquestación de casos de uso, validación, mapeo de datos
- **Componentes**:
  - `Services/` - Servicios de aplicación
  - `Validators/` - Validadores FluentValidation
  - `DTOs/` - Data Transfer Objects

### 3. **BusinessLayer** (Capa de Lógica de Negocio)
- **Responsabilidad**: Implementación de reglas de negocio, acceso a datos
- **Componentes**:
  - `Commands/` - Operaciones de escritura (Entity Framework)
  - `Queries/` - Operaciones de lectura (Dapper)
  - `Validators/` - Validadores de negocio

### 4. **ModelLayer** (Capa de Datos)
- **Responsabilidad**: Entidades, DbContext, configuraciones de base de datos
- **Componentes**:
  - `Entities/` - Entidades del dominio
  - `DBcontext.cs` - Contexto de Entity Framework
  - `Shared/` - Componentes compartidos (ConnectionResolver)

---

## 🔄 Patrón CQRS

El proyecto implementa **Command Query Responsibility Segregation (CQRS)** para separar las operaciones de lectura y escritura:

### **Commands** (Escritura)
- **Tecnología**: Entity Framework Core
- **Ubicación**: `BusinessLayer/{Module}/Commands/`
- **Propósito**: Operaciones que modifican datos (Create, Update, Delete)

```csharp
// Ejemplo: BusinessLayer/Security/Commands/CreateUserCommand.cs
public class CreateUserCommand
{
    public async Task<int> ExecuteAsync(CreateUserRequest request)
    {
        // Lógica de creación usando Entity Framework
    }
}
```

### **Queries** (Lectura)
- **Tecnología**: Dapper
- **Ubicación**: `BusinessLayer/{Module}/Queries/`
- **Propósito**: Operaciones de consulta (Select, Get, Find)

```csharp
// Ejemplo: BusinessLayer/Security/Queries/UserQueryRepository.cs
public class UserQueryRepository
{
    public async Task<IEnumerable<User>> GetUsersAsync(string? email = null)
    {
        // Consultas optimizadas usando Dapper
    }
}
```

---

## ✅ FluentValidation

### Configuración Actual
- **Paquete**: `FluentValidation` v12.0.0
- **Ubicación**: `BusinessLayer/{Module}/Validators/`
- **Registro**: Manual en `Program.cs` (línea 62-63)

### Estructura de Validadores

```csharp
// BusinessLayer/Security/Validators/CreateUserValidator.cs
public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("El email es requerido")
            .EmailAddress()
            .WithMessage("El formato del email es inválido");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre no puede exceder 100 caracteres");
    }
}
```

### Uso en Controllers

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    // Validación manual
    var validationResult = await _validationService.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return BadRequest(ResponseStructure<object>.ValidationError(
            string.Join(", ", validationResult.Errors)));
    }

    // Lógica del endpoint
}
```

---

## 🗄️ Entity Framework vs Dapper

### **Entity Framework** (Commands)
- **Uso**: Operaciones de escritura (CUD)
- **Ventajas**: 
  - Change tracking
  - Migrations automáticas
  - Relaciones complejas
  - Validaciones a nivel de entidad

```csharp
// BusinessLayer/Security/Commands/UserCommandRepository.cs
public class UserCommandRepository
{
    private readonly DBcontext _context;

    public async Task<int> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }
}
```

### **Dapper** (Queries)
- **Uso**: Operaciones de lectura (R)
- **Ventajas**:
  - Alto rendimiento
  - SQL nativo
  - Mapeo directo
  - Control total sobre consultas

```csharp
// BusinessLayer/Security/Queries/UserQueryRepository.cs
public class UserQueryRepository
{
    public async Task<IEnumerable<User>> GetUsersAsync(string? email = null)
    {
        var sql = "SELECT * FROM Users WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(email))
            sql += " AND Email = @Email";

        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<User>(sql, new { Email = email });
    }
}
```

---

## 📝 Guías de Desarrollo

### 1. **Crear un Nuevo Módulo**

#### Paso 1: Crear Entidades
```csharp
// ModelLayer/{Module}/Entities/Product.cs
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}
```

#### Paso 2: Configurar DbContext
```csharp
// ModelLayer/DBcontext.cs
public class DBcontext : DbContext
{
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });
    }
}
```

#### Paso 3: Crear Commands (Entity Framework)
```csharp
// BusinessLayer/{Module}/Commands/CreateProductCommand.cs
public class CreateProductCommand
{
    private readonly DBcontext _context;

    public async Task<int> ExecuteAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product.Id;
    }
}
```

#### Paso 4: Crear Queries (Dapper)
```csharp
// BusinessLayer/{Module}/Queries/ProductQueryRepository.cs
public class ProductQueryRepository
{
    private readonly string _connectionString;

    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        const string sql = "SELECT * FROM Products WHERE IsActive = 1";
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<Product>(sql);
    }
}
```

#### Paso 5: Crear Validadores
```csharp
// BusinessLayer/{Module}/Validators/CreateProductValidator.cs
public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del producto es requerido")
            .MaximumLength(100)
            .WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("El precio debe ser mayor a 0");
    }
}
```

#### Paso 6: Crear Servicios de Aplicación
```csharp
// ApplicationLayer/{Module}/ProductService.cs
public class ProductService
{
    private readonly CreateProductCommand _createProductCommand;
    private readonly ProductQueryRepository _productQueryRepository;

    public async Task<int> CreateProductAsync(CreateProductRequest request)
    {
        return await _createProductCommand.ExecuteAsync(request);
    }

    public async Task<IEnumerable<Product>> GetProductsAsync()
    {
        return await _productQueryRepository.GetProductsAsync();
    }
}
```

#### Paso 7: Crear Controller
```csharp
// ApiLayer/Controllers/{Module}/ProductController.cs
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ValidationService _validationService;

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var validationResult = await _validationService.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(ResponseStructure<object>.ValidationError(
                string.Join(", ", validationResult.Errors)));
        }

        try
        {
            var productId = await _productService.CreateProductAsync(request);
            var response = ResponseStructure<int>.Success(productId, "Producto creado exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ResponseStructure<object>.Error(ex.Message));
        }
    }
}
```

#### Paso 8: Registrar Servicios
```csharp
// ApiLayer/Program.cs
builder.Services.AddScoped<CreateProductCommand>();
builder.Services.AddScoped<ProductQueryRepository>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IValidator<CreateProductRequest>, CreateProductValidator>();
```

### 2. **Estructura de Carpetas por Módulo**

```
{Module}/
├── ModelLayer/
│   └── {Module}/
│       └── Entities/
│           └── {Entity}.cs
├── BusinessLayer/
│   └── {Module}/
│       ├── Commands/
│       │   ├── Create{Entity}Command.cs
│       │   ├── Update{Entity}Command.cs
│       │   └── Delete{Entity}Command.cs
│       ├── Queries/
│       │   └── {Entity}QueryRepository.cs
│       └── Validators/
│           ├── Create{Entity}Validator.cs
│           └── Update{Entity}Validator.cs
├── ApplicationLayer/
│   └── {Module}/
│       └── {Entity}Service.cs
└── ApiLayer/
    └── Controllers/
        └── {Module}/
            └── {Entity}Controller.cs
```

---

## ⚙️ Configuración y Setup

### 1. **Connection Strings**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;Trusted_Connection=true;",
    "PostSales": "Server=...;Database=...;Trusted_Connection=true;",
    "PurdyApps": "Server=...;Database=...;Trusted_Connection=true;"
  }
}
```

### 2. **JWT Configuration**
```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "VHConsultor",
    "Audience": "VHConsultor-Users",
    "ExpirationMinutes": 60,
    "Claims": {
      "UserId": "user_id",
      "Role": "role",
      "CompanyId": "company_id",
      "BranchId": "branch_id"
    }
  }
}
```

### 3. **Dependencias Principales**
```xml
<!-- ApiLayer.csproj -->
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.17" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />

<!-- BusinessLayer.csproj -->
<PackageReference Include="Dapper" Version="2.1.66" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.6" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.2" />

<!-- ModelLayer.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.17" />
```

---

## 🎯 Mejores Prácticas

### 1. **Naming Conventions**
- **Entidades**: PascalCase (`User`, `Product`)
- **Commands**: `{Action}{Entity}Command` (`CreateUserCommand`)
- **Queries**: `{Entity}QueryRepository` (`UserQueryRepository`)
- **Validators**: `{Action}{Entity}Validator` (`CreateUserValidator`)
- **Controllers**: `{Entity}Controller` (`UserController`)

### 2. **Response Structure**
Siempre usar `ResponseStructure<T>` para respuestas consistentes:

```csharp
// Éxito
return Ok(ResponseStructure<User>.Success(user, "Usuario obtenido exitosamente"));

// Error
return BadRequest(ResponseStructure<object>.ValidationError("Email es requerido"));

// Error interno
return StatusCode(500, ResponseStructure<object>.Error("Error interno del servidor"));
```

### 3. **Manejo de Excepciones**
- Usar `GlobalExceptionHandler` para capturar excepciones no manejadas
- Logear errores usando `ErrorLogService`
- Retornar mensajes de error apropiados al cliente

### 4. **Validación**
- Validar en múltiples capas:
  - **Controller**: Validación de entrada
  - **Application**: Validación de reglas de negocio
  - **Entity**: Validación de datos

### 5. **Transacciones**
- Usar transacciones para operaciones complejas
- Implementar rollback en caso de errores
- Considerar patrones como Unit of Work para operaciones múltiples

### 6. **Performance**
- Usar Dapper para consultas complejas
- Implementar paginación en listados
- Usar `async/await` consistentemente
- Considerar caching para datos frecuentemente accedidos

### 7. **Seguridad**
- Validar JWT tokens en endpoints protegidos
- Usar HTTPS en producción
- Implementar rate limiting
- Sanitizar inputs del usuario

---

## 🚀 Comandos Útiles

### Desarrollo
```bash
# Restaurar paquetes
dotnet restore

# Compilar
dotnet build

# Ejecutar
dotnet run --project ApiLayer

# Ejecutar con hot reload
dotnet watch run --project ApiLayer
```

### Base de Datos
```bash
# Crear migración
dotnet ef migrations add InitialCreate --project ModelLayer --startup-project ApiLayer

# Aplicar migraciones
dotnet ef database update --project ModelLayer --startup-project ApiLayer

# Revertir migración
dotnet ef database update PreviousMigration --project ModelLayer --startup-project ApiLayer
```

---

## 📚 Recursos Adicionales

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Dapper Documentation](https://dapper-tutorial.net/)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [JWT Authentication in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [Clean Architecture Principles](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

**Última actualización**: Diciembre 2024  
**Versión del proyecto**: .NET 8.0
