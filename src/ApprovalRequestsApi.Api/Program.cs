using ApprovalRequestsApi.Api.Auth;
using ApprovalRequestsApi.Application.Interfaces;
using ApprovalRequestsApi.Application.Services;
using ApprovalRequestsApi.Application.Validators;
using ApprovalRequestsApi.Infrastructure.Data;
using ApprovalRequestsApi.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Zitadel.Api;
using Zitadel.Credentials;

var builder = WebApplication.CreateBuilder(args);

// Controllers y Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serializar enums como strings en lugar de números
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Approval Requests API", Version = "v1" });
});

// IMPORTANTE: Se necesitan 2 credenciales diferentes:
// 1. Application credentials para validar tokens (AddZitadelIntrospection)
// 2. Service Account credentials para consultar API de Zitadel (ITokenProvider)

// 1. Cargar Application credentials (para validar tokens entrantes)
var applicationCredentials = await Application.LoadFromJsonFileAsync(
    "./config/service-account.json");

// 2. Cargar Service Account credentials (para consultar usuarios de Zitadel)
var serviceAccount = await ServiceAccount.LoadFromJsonFileAsync(
    builder.Configuration["Zitadel:ServiceAccountPath"]!);

// Configurar HttpClient global para aceptar certificados en desarrollo
if (builder.Environment.IsDevelopment())
{
    // Configurar validación de certificados más permisiva para desarrollo
    builder.Services.ConfigureAll<HttpClientHandler>(handler =>
    {
        handler.ServerCertificateCustomValidationCallback =
            (message, cert, chain, sslPolicyErrors) =>
            {
                // Log para diagnóstico
                if (cert != null)
                {
                    Console.WriteLine($"Certificate validation - Subject: {cert.Subject}, Errors: {sslPolicyErrors}");
                }
                // En desarrollo, aceptar todos los certificados
                return true;
            };
    });
}

// Configurar autenticación con Zitadel usando JWT Bearer
builder.Services
    .AddAuthentication("ZitadelJWT")
    .AddJwtBearer("ZitadelJWT", options =>
    {
        options.Authority = builder.Configuration["Zitadel:Authority"];
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Zitadel:Authority"],
            ValidateAudience = true,
            // Aceptar varios audiences: el frontend, el project ID y la API
            ValidAudiences = new[] {
                "351351811153657858",  // Frontend client ID
                "350339429128208386",  // Project ID
                "351346786931113986"   // API client ID
            },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        // En desarrollo, aceptar certificados SSL sin validación
        if (builder.Environment.IsDevelopment())
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
            };
        }
    });

builder.Services
    .AddAuthorization(options =>
    {
        // Política para usuarios normales y admins
        options.AddPolicy("UserPolicy", policy =>
            policy.RequireRole("User", "Admin"));

        // Política solo para admins
        options.AddPolicy("AdminPolicy", policy =>
            policy.RequireRole("Admin"));
    });

// Transformar claims para mapear roles de Zitadel a claims de rol estándar
builder.Services.AddTransient<Microsoft.AspNetCore.Authentication.IClaimsTransformation, ZitadelRolesClaimsTransformation>();

// Registrar ITokenProvider para Service Account (para consultar API de Zitadel)
// Según documentación: https://zitadel.com/docs/guides/integrate/zitadel-apis/example-zitadel-api-with-dot-net
builder.Services.AddSingleton<ITokenProvider>(sp =>
    ITokenProvider.ServiceAccount(
        builder.Configuration["Zitadel:Authority"]!,
        serviceAccount, // ServiceAccount para consultar API
        new ServiceAccount.AuthOptions { ApiAccess = true, ProjectAudiences = { "zitadel" } }));

// Base de datos PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios de aplicación
builder.Services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
builder.Services.AddScoped<IApprovalRequestService, ApprovalRequestService>();
builder.Services.AddScoped<IUserInfoService, ZitadelUserInfoService>();

// Caché en memoria para información de usuarios
builder.Services.AddMemoryCache();

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateApprovalRequestValidator>();

// CORS (opcional, configurar según necesidades)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Approval Requests API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();  // IMPORTANTE: antes de UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
