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
builder.Services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles)
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

// Configurar límites para multipart/form-data (archivos)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10 MB en bytes
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
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

// Amazon Validators
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Amazon.Commands.GenerateAccessTokenCommand>, BusinessLayer.Amazon.Validators.GenerateAccessTokenValidator>();

// Auth Validators
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.LoginCommand>, BusinessLayer.Shared.Validators.LoginCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.CreateUserCommand>, BusinessLayer.Shared.Validators.CreateUserCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.ChangePasswordCommand>, BusinessLayer.Shared.Validators.ChangePasswordCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.LockAccountCommand>, BusinessLayer.Shared.Validators.LockAccountCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.UnlockAccountCommand>, BusinessLayer.Shared.Validators.UnlockAccountCommandValidator>();

// Permission Validators
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.CreatePermissionRequest>, BusinessLayer.Shared.Validators.CreatePermissionValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Shared.Commands.UpdatePermissionRequest>, BusinessLayer.Shared.Validators.UpdatePermissionValidator>();

// Corporate Validators
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateCustomerRequest>, BusinessLayer.Corporate.Validators.CreateCustomerValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateCustomerRequest>, BusinessLayer.Corporate.Validators.UpdateCustomerValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateContractRequest>, BusinessLayer.Corporate.Validators.CreateContractValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateContractRequest>, BusinessLayer.Corporate.Validators.UpdateContractValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateContractServiceRequest>, BusinessLayer.Corporate.Validators.CreateContractServiceValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateContractServiceRequest>, BusinessLayer.Corporate.Validators.UpdateContractServiceValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.GenerateContractPdfRequest>, BusinessLayer.Corporate.Validators.GenerateContractPdfRequestValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.SendContractEmailRequest>, BusinessLayer.Corporate.Validators.SendContractEmailRequestValidator>();
// Pricing Validators
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateServiceBudgetRangeCommand>, BusinessLayer.Corporate.Validators.CreateServiceBudgetRangeCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateServiceBudgetRangeCommand>, BusinessLayer.Corporate.Validators.UpdateServiceBudgetRangeCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateServiceAdBudgetRangeCommand>, BusinessLayer.Corporate.Validators.CreateServiceAdBudgetRangeCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateServiceAdBudgetRangeCommand>, BusinessLayer.Corporate.Validators.UpdateServiceAdBudgetRangeCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CalculatePercentageCommand>, BusinessLayer.Corporate.Validators.CalculatePercentageCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CalculateFixedCommand>, BusinessLayer.Corporate.Validators.CalculateFixedCommandValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CalculatePricingCommand>, BusinessLayer.Corporate.Validators.CalculatePricingCommandValidator>();
// Invoice Validators
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.GenerateInvoicesRequest>, BusinessLayer.Corporate.Validators.GenerateInvoicesValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.MarkInvoiceAsPaidRequest>, BusinessLayer.Corporate.Validators.MarkInvoiceAsPaidValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateAmazonMarketplaceRequest>, BusinessLayer.Corporate.Validators.CreateAmazonMarketplaceValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateAmazonMarketplaceRequest>, BusinessLayer.Corporate.Validators.UpdateAmazonMarketplaceValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.CreateAmazonAccountRequest>, BusinessLayer.Corporate.Validators.CreateAmazonAccountValidator>();
builder.Services.AddScoped<FluentValidation.IValidator<BusinessLayer.Corporate.Commands.UpdateAmazonAccountRequest>, BusinessLayer.Corporate.Validators.UpdateAmazonAccountValidator>();
#endregion

#region Azure Storage Configuration
// Configurar Azure Storage Settings
builder.Services.Configure<BusinessLayer.Shared.AzureStorageSettings>(builder.Configuration.GetSection("AzureStorage"));
builder.Services.AddScoped<BusinessLayer.Shared.Services.AzureBlobStorageService>();
#endregion

#region Amazon Configuration
// Configurar Amazon Settings
builder.Services.Configure<BusinessLayer.Amazon.AmazonSettings>(builder.Configuration.GetSection("Amazon"));
// Registrar HttpClient para Amazon con configuración específica
builder.Services.AddHttpClient<ApplicationLayer.Amazon.AmazonAuthService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
#endregion

#region CraftMyPDF Configuration
// Configurar CraftMyPDF Settings
builder.Services.Configure<BusinessLayer.Shared.CraftMyPdfSettings>(builder.Configuration.GetSection("CraftMyPdf"));
// Registrar HttpClient para CraftMyPDF
builder.Services.AddHttpClient("CraftMyPdf", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
// Registrar el servicio CraftMyPDF
builder.Services.AddScoped<ApplicationLayer.Shared.CraftMyPdfService>();
#endregion

#region SendGrid Configuration
// Configurar SendGrid Settings
builder.Services.Configure<BusinessLayer.Shared.SendGridSettings>(builder.Configuration.GetSection("SendGrid"));
// Registrar el servicio SendGrid
builder.Services.AddScoped<ApplicationLayer.Shared.SendGridService>();
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
builder.Services.AddScoped<ApplicationLayer.Shared.AuthService>();
builder.Services.AddScoped<ApplicationLayer.Shared.RBACService>();
builder.Services.AddScoped<ApplicationLayer.Shared.PermissionService>();

// Ecommerce Services
builder.Services.AddScoped<ApplicationLayer.Ecommerce.CustomerSubmissionService>();

// Amazon Services
builder.Services.AddScoped<ApplicationLayer.Amazon.AmazonAuthService>();

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
builder.Services.AddScoped<ApplicationLayer.Corporate.PricingService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.PricingServiceService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.BusinessTypeService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.PlatformService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.InvoiceService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.BillingReportService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.AmazonMarketplaceService>();
builder.Services.AddScoped<ApplicationLayer.Corporate.AmazonAccountService>();
#endregion

#region ScopedInterfazAndRepository

// Security CQRS Repositories
// Commands
builder.Services.AddScoped<BusinessLayer.Shared.Commands.UserAuthCommandRepository>();
builder.Services.AddScoped<BusinessLayer.Shared.Commands.RBACCommandRepository>();
builder.Services.AddScoped<BusinessLayer.Shared.Commands.CreatePermissionCommand>();
builder.Services.AddScoped<BusinessLayer.Shared.Commands.UpdatePermissionCommand>();
builder.Services.AddScoped<BusinessLayer.Shared.Commands.DeletePermissionCommand>();
// Queries
builder.Services.AddScoped<BusinessLayer.Shared.Queries.UserQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Shared.Queries.UserLoginHistoryQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Shared.Queries.RBACQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Shared.Queries.PermissionQueryRepository>();

// Ecommerce CQRS Repositories
builder.Services.AddScoped<BusinessLayer.Ecommerce.Commands.CreateCustomerSubmissionCommand>();

// Amazon CQRS Repositories
builder.Services.AddScoped<BusinessLayer.Amazon.Commands.AmazonTokenCommandRepository>();
builder.Services.AddScoped<BusinessLayer.Amazon.Queries.AmazonTokenQueryRepository>();

// Corporate CQRS Repositories
// Commands
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateCustomerCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateCustomerCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteCustomerCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateLastContactDateCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateContractCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateContractCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteContractCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UploadSignedContractDocumentCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateContractServiceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateContractServiceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteContractServiceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.GenerateInvoicesCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateManualInvoiceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.GeneratePercentageInvoicesCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateInvoiceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.MarkInvoiceAsPaidCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteInvoicesCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UploadInvoiceAttachmentCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.DeleteInvoiceAttachmentCommand>();
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
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.PricingCommandRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.PricingQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.PricingServiceQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.BusinessTypeQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.PlatformQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.InvoiceQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.BillingReportQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.AmazonMarketplaceQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateAmazonMarketplaceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateAmazonMarketplaceCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Queries.AmazonAccountQueryRepository>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.CreateAmazonAccountCommand>();
builder.Services.AddScoped<BusinessLayer.Corporate.Commands.UpdateAmazonAccountCommand>();
#endregion

#region SwaggerConfig
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "VHConsultor API",
        Version = "v1",
        // Actualizar versión al subir cambios (ej.: 1.2.0.3, 1.2.0.4, 1.3.0.0)
        Description = "Versión: 1.2.0.2\n\n" +
            "API para VHConsultor - Sistema de Consultoría.\n\n" +
            "---\n\n" +
            "**ADVERTENCIA — ACCESO NO AUTORIZADO PROHIBIDO / WARNING — UNAUTHORIZED ACCESS PROHIBITED**\n\n" +
            "**Español:** Queda estrictamente prohibido el ingreso y el uso de esta plataforma o de cualquiera de sus APIs por personas no autorizadas por VHConsultor. " +
            "El acceso, consulta o consumo de estos recursos sin autorización expresa no está permitido y puede constituir un delito. " +
            "El acceso ilícito a sistemas informáticos está tipificado como delito en instrumentos internacionales vinculantes, entre ellos el Convenio sobre la Ciberdelincuencia del Consejo de Europa (Convenio de Budapest, 2001), Artículo 2 — Acceso ilícito, y en las leyes penales aplicables en las jurisdicciones de los usuarios y de VHConsultor. " +
            "VHConsultor se reserva el derecho de denunciar y perseguir cualquier uso no autorizado, incluidas las acciones legales y la cooperación con autoridades.\n\n" +
            "**English:** Unauthorized access to and use of this platform or any of its APIs by persons not authorized by VHConsultor is strictly prohibited. " +
            "Accessing, querying or consuming these resources without express authorization is not permitted and may constitute a criminal offence. " +
            "Illegal access to computer systems is criminalized under binding international instruments, including the Council of Europe Convention on Cybercrime (Budapest Convention, 2001), Article 2 — Illegal access, and under applicable criminal laws in the users’ and VHConsultor’s jurisdictions. " +
            "VHConsultor reserves the right to report and pursue any unauthorized use, including legal action and cooperation with authorities.",
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
    .WithOrigins(
        "https://localhost:5001", 
        "http://localhost:5009",
        "https://vhcorporate.com",
        "http://vhcorporate.com",
        "https://www.vhcorporate.com",
        "http://www.vhcorporate.com"
    )
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler();
app.MapControllers();
app.Run();
#endregion
