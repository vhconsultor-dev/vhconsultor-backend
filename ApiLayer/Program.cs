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
// Queries
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.CustomerQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.CountryQueryRepository>();
#endregion

#region SwaggerConfig
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
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
