using System.Text.Json.Serialization;
using FluentValidation;
using BusinessLayer.Shared;
using Microsoft.EntityFrameworkCore;
using ApiLayer.Tools;


var builder = WebApplication.CreateBuilder(args);

#region Environments
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();
#endregion

#region ConfigurationController
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
#endregion

#region AutoMapperConfig
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// TODO: Add AutoMapperProfile when created
// builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
#endregion

#region Database Configuration
// Registrar DBcontext
builder.Services.AddDbContext<ModelLayer.DBcontext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("VH-DB")));

// Registrar ConnectionResolver
builder.Services.AddScoped<ModelLayer.Shared.IConnectionResolver>(serviceProvider =>
{
    var isDevelopment = builder.Environment.IsDevelopment();
    return new ModelLayer.Shared.ConnectionResolver(builder.Configuration, isDevelopment);
});
#endregion

#region DataBaseConnect
// TODO: Add Entity Framework configuration when DBContext is created
// builder.Services.AddDbContext<DBContext_LD_OPERACION>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("LD_OPERACION")));
#endregion

#region KeyVault Configuration
if (builder.Environment.IsProduction())
{
    // TODO: Add KeyVault configuration when needed
    // var keyVaultManager = KeyVaultManager.GetInstance(builder.Configuration);
    // await keyVaultManager.LoadAllSecretsAsync();
    // builder.Services.AddSingleton(keyVaultManager);
}
#endregion

#region FluentValidation
// Validación manual en los controllers, no automática
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Ecommerce.Commands.CreateCustomerSubmissionRequest>, BusinessLayer.Ecommerce.Validators.CreateCustomerSubmissionValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.GenerateJwtCommand>, BusinessLayer.Shared.Validators.GenerateJwtValidator>();

// Corporate Validators
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateCustomerRequest>, BusinessLayer.Corporate.Validators.CreateCustomerValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateCustomerRequest>, BusinessLayer.Corporate.Validators.UpdateCustomerValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateContractRequest>, BusinessLayer.Corporate.Validators.CreateContractValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateContractRequest>, BusinessLayer.Corporate.Validators.UpdateContractValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateContractServiceRequest>, BusinessLayer.Corporate.Validators.CreateContractServiceValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateContractServiceRequest>, BusinessLayer.Corporate.Validators.UpdateContractServiceValidator>();
#endregion

#region JWT Configuration
// Configurar JWT Settings
builder.Services.Configure<BusinessLayer.Shared.JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Configurar JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<BusinessLayer.Shared.JwtSettings>();
        
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Configurar Authorization
builder.Services.AddAuthorization();
#endregion


#region ScopedServices
builder.Services.AddScoped<ApplicationLayer.Shared.ValidationService>();
builder.Services.AddScoped<ApplicationLayer.Shared.IDatabaseConfigurationService, ApplicationLayer.Shared.DatabaseConfigurationService>();

// Error Logging Services
builder.Services.AddScoped<BusinessLayer.Shared.Commands.IErrorLogCommandRepository, BusinessLayer.Shared.Commands.ErrorLogCommandRepository>();
builder.Services.AddScoped<ApplicationLayer.Shared.IErrorLogService, ApplicationLayer.Shared.ErrorLogService>();

// Security Services
builder.Services.AddScoped<ApplicationLayer.Shared.JwtService>();

// Ecommerce Services
builder.Services.AddScoped<ApplicationLayer.Ecommerce.CustomerSubmissionService>();

// Corporate Services
builder.Services.AddScoped<ApplicationLayer.Corporate.CustomerService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.CountryService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.IndustrySectorService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.PaymentMethodService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.ServiceService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.FeeTypeService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.CurrencyService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.ContractTypeService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.ContractService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.ContractServiceService>();
#endregion

#region ScopedInterfazAndRepository

// Security CQRS Repositories

// Ecommerce CQRS Repositories
builder.Services.AddScoped<BusinessLayer.Ecommerce.Commands.CreateCustomerSubmissionCommand>();

// Corporate CQRS Repositories
// Commands
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateCustomerCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateCustomerCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteCustomerCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateLastContactDateCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateContractCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateContractCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteContractCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateContractServiceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateContractServiceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteContractServiceCommand>();
// Queries
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.CustomerQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.CountryQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.IndustrySectorQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.PaymentMethodQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.ServiceQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.FeeTypeQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.CurrencyQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.ContractTypeQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.ContractQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.ContractServiceQueryRepository>();
#endregion

#region SwaggerConfig
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "VHConsultor API",
        Version = "v1",
        Description = "API para VHConsultor - Sistema de Consultoría",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "VHConsultor Team",
            Email = "support@vhconsultor.com"
        }
    });

    // Configurar autenticación JWT en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
var app = builder.Build();

// Configurar Swagger para desarrollo y producción
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VHConsultor API v1");
        c.RoutePrefix = "swagger"; // Ruta por defecto: /swagger
        c.DocumentTitle = "VHConsultor API Documentation";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
    });

    // Configurar ruta personalizada /VHBackend/swagger
    app.MapGet("/VHBackend/swagger", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}
#endregion

#region MiddleWares
app.UseCors(builder => builder
    .WithOrigins("https://pss.purdyseguros.com", "https://localhost:5001", "http://localhost:5009")
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler();
app.MapControllers();
app.Run();
#endregion
