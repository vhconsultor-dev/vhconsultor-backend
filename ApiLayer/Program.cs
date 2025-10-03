using System.Text.Json.Serialization;
using FluentValidation;
using BusinessLayer.Shared;
using Microsoft.EntityFrameworkCore;
using ApiLayer.Tools;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ModelLayer.Security;

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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
#endregion

#region JWT Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
if (jwtSettings == null)
{
    throw new InvalidOperationException("JWT configuration is missing");
}
var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
#endregion

#region ScopedServices
builder.Services.AddScoped<ApplicationLayer.Shared.ValidationService>();
builder.Services.AddScoped<ApplicationLayer.Shared.IDatabaseConfigurationService, ApplicationLayer.Shared.DatabaseConfigurationService>();
builder.Services.AddScoped<ApplicationLayer.Security.JwtService>();
builder.Services.AddScoped<ApplicationLayer.Security.AuthenticationService>();
builder.Services.AddScoped<ApplicationLayer.Security.UserService>();

// Error Logging Services
builder.Services.AddScoped<BusinessLayer.Shared.Commands.IErrorLogCommandRepository, BusinessLayer.Shared.Commands.ErrorLogCommandRepository>();
builder.Services.AddScoped<ApplicationLayer.Shared.IErrorLogService, ApplicationLayer.Shared.ErrorLogService>();
#endregion

#region ScopedInterfazAndRepository

// Security CQRS Repositories
builder.Services.AddScoped<BusinessLayer.Security.Queries.JwtCredentialsQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Security.Queries.SystemCredentialsQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Security.Queries.UserQueryRepository>();
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
app.UseGlobalExceptionHandler(); // Debe ir antes de Authentication y Authorization
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
#endregion
