FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar archivos de proyecto
COPY ["src/ApprovalRequestsApi.Api/ApprovalRequestsApi.Api.csproj", "src/ApprovalRequestsApi.Api/"]
COPY ["src/ApprovalRequestsApi.Application/ApprovalRequestsApi.Application.csproj", "src/ApprovalRequestsApi.Application/"]
COPY ["src/ApprovalRequestsApi.Domain/ApprovalRequestsApi.Domain.csproj", "src/ApprovalRequestsApi.Domain/"]
COPY ["src/ApprovalRequestsApi.Infrastructure/ApprovalRequestsApi.Infrastructure.csproj", "src/ApprovalRequestsApi.Infrastructure/"]

# Restaurar dependencias
RUN dotnet restore "src/ApprovalRequestsApi.Api/ApprovalRequestsApi.Api.csproj"

# Copiar código fuente
COPY . .

# Build y publish
WORKDIR "/src/src/ApprovalRequestsApi.Api"
RUN dotnet publish "ApprovalRequestsApi.Api.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApprovalRequestsApi.Api.dll"]
