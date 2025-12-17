# Approval Requests API

API REST para gestionar solicitudes de aprobación con autenticación JWT mediante Zitadel.

## Descripción

Sistema de solicitudes de aprobación que permite a los usuarios crear solicitudes y a los administradores aprobarlas o rechazarlas. La API implementa autenticación y autorización basada en roles usando Zitadel como proveedor de identidad.

## Arquitectura

El proyecto sigue una arquitectura limpia (Clean Architecture) con las siguientes capas:

- **Api**: Controladores y configuración de la aplicación web
- **Application**: Lógica de negocio, servicios, DTOs y validadores
- **Domain**: Entidades y enums del dominio
- **Infrastructure**: Acceso a datos, repositorios y configuración de EF Core

## Tecnologías

- **.NET 9.0**
- **PostgreSQL** - Base de datos
- **Entity Framework Core 8.0** - ORM
- **Zitadel** - Autenticación y autorización JWT
- **FluentValidation** - Validación de DTOs
- **Swagger/OpenAPI** - Documentación de la API
- **Docker** - Contenedorización

## Requisitos previos

- .NET 9.0 SDK
- Docker y Docker Compose
- Cuenta de Zitadel configurada con:
  - Application credentials
  - Service Account credentials
  - Roles: `User` y `Admin`

## Configuración

### 1. Archivos de configuración

Crear los siguientes archivos en el directorio `config/`:

#### `service-account.json` (Application credentials)
```json
{
  "type": "application",
  "keyId": "YOUR_KEY_ID",
  "key": "YOUR_PRIVATE_KEY",
  "appId": "YOUR_APP_ID",
  "clientId": "YOUR_CLIENT_ID"
}
```

#### `service-account-api.json` (Service Account credentials)
```json
{
  "type": "serviceaccount",
  "keyId": "YOUR_KEY_ID",
  "key": "YOUR_PRIVATE_KEY",
  "userId": "YOUR_USER_ID"
}
```

### 2. Variables de entorno

Las siguientes variables se configuran en [docker-compose.yml](docker-compose.yml):

```yaml
ASPNETCORE_ENVIRONMENT: Development
ASPNETCORE_URLS: http://+:8081
ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=approval_requests_db;Username=postgres;Password=postgres_password"
Zitadel__Authority: "https://zitadel.farinter.com"
Zitadel__ServiceAccountPath: "/app/config/service-account-api.json"
```

## Instalación y ejecución

### Usando Docker Compose (recomendado)

```bash
# Construir y levantar los servicios
docker-compose up -d

# Ver logs
docker-compose logs -f api

# Detener los servicios
docker-compose down
```

La API estará disponible en: `http://localhost:8081`

Swagger UI: `http://localhost:8081/swagger`

### Ejecución local

```bash
# Restaurar dependencias
dotnet restore

# Aplicar migraciones
cd src/ApprovalRequestsApi.Api
dotnet ef database update --project ../ApprovalRequestsApi.Infrastructure

# Ejecutar la aplicación
dotnet run
```

## Endpoints

### Solicitudes de aprobación

| Método | Endpoint | Descripción | Roles requeridos |
|--------|----------|-------------|------------------|
| POST | `/api/approval-requests` | Crear solicitud | User, Admin |
| GET | `/api/approval-requests/my-requests` | Obtener mis solicitudes | User, Admin |
| GET | `/api/approval-requests` | Listar todas las solicitudes | Admin |
| GET | `/api/approval-requests/{id}` | Obtener solicitud por ID | User, Admin |
| PATCH | `/api/approval-requests/{id}/review` | Revisar solicitud | Admin |
| POST | `/api/approval-requests/{id}/approve` | Aprobar solicitud | Admin |
| POST | `/api/approval-requests/{id}/reject` | Rechazar solicitud | Admin |

### Ejemplos de uso

#### Crear solicitud
```bash
curl -X POST http://localhost:8081/api/approval-requests \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Solicitud de vacaciones",
    "description": "Solicito 5 días de vacaciones del 1 al 5 de enero"
  }'
```

#### Revisar solicitud (aprobar/rechazar)
```bash
curl -X PATCH http://localhost:8081/api/approval-requests/{id}/review \
  -H "Authorization: Bearer ADMIN_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "status": "approved",
    "adminComments": "Aprobado"
  }'
```

## Estructura del proyecto

```
ApprovalRequestsApi/
├── src/
│   ├── ApprovalRequestsApi.Api/          # Capa de presentación
│   │   ├── Controllers/                   # Controladores REST
│   │   ├── Auth/                          # Transformación de claims
│   │   └── Program.cs                     # Configuración de la app
│   ├── ApprovalRequestsApi.Application/   # Capa de aplicación
│   │   ├── DTOs/                          # Data Transfer Objects
│   │   ├── Interfaces/                    # Interfaces de servicios
│   │   ├── Services/                      # Lógica de negocio
│   │   └── Validators/                    # Validadores FluentValidation
│   ├── ApprovalRequestsApi.Domain/        # Capa de dominio
│   │   ├── Entities/                      # Entidades del dominio
│   │   └── Enums/                         # Enumeraciones
│   └── ApprovalRequestsApi.Infrastructure/ # Capa de infraestructura
│       ├── Data/                          # DbContext y configuración
│       ├── Migrations/                    # Migraciones de EF Core
│       └── Repositories/                  # Implementación de repositorios
├── config/                                # Archivos de configuración de Zitadel
├── docker-compose.yml                     # Orquestación de contenedores
└── ApprovalRequestsApi.sln               # Solución de Visual Studio
```

## Modelo de datos

### ApprovalRequest

```csharp
public class ApprovalRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string RequesterId { get; set; }          // Zitadel user ID
    public ApprovalStatus Status { get; set; }       // Pending, Approved, Rejected
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerId { get; set; }          // Zitadel user ID
    public string? AdminComments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Estados de aprobación

```csharp
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
```

## Características

- **Autenticación JWT**: Integración con Zitadel
- **Autorización basada en roles**: User y Admin
- **Validación de datos**: FluentValidation
- **Paginación**: Soporte de búsqueda con paginación
- **Caché**: Información de usuarios en memoria
- **Documentación**: Swagger/OpenAPI
- **Contenedorización**: Docker y Docker Compose
- **Base de datos**: PostgreSQL con Entity Framework Core
- **Clean Architecture**: Separación de responsabilidades

## Desarrollo

### Crear nueva migración

```bash
cd src/ApprovalRequestsApi.Api
dotnet ef migrations add MigrationName --project ../ApprovalRequestsApi.Infrastructure
```

### Aplicar migraciones

```bash
dotnet ef database update --project ../ApprovalRequestsApi.Infrastructure
```

### Revertir última migración

```bash
dotnet ef database update PreviousMigrationName --project ../ApprovalRequestsApi.Infrastructure
```

## Licencia

[Especificar licencia del proyecto]

## Contacto

[Información de contacto del equipo]
