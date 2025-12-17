using ApprovalRequestsApi.Application.DTOs.Responses;
using ApprovalRequestsApi.Application.Interfaces;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zitadel.Api;
using Zitadel.Authentication;
using Zitadel.Management.V1;

namespace ApprovalRequestsApi.Application.Services;

public class ZitadelUserInfoService : IUserInfoService
{
    private readonly ITokenProvider _tokenProvider;
    private readonly string _zitadelAuthority;
    private readonly string? _organizationId;
    private readonly ILogger<ZitadelUserInfoService> _logger;
    private readonly IMemoryCache _cache;
    private readonly ManagementService.ManagementServiceClient _managementClient;

    public ZitadelUserInfoService(
        ITokenProvider tokenProvider,
        IConfiguration configuration,
        ILogger<ZitadelUserInfoService> logger,
        IMemoryCache cache)
    {
        _tokenProvider = tokenProvider;
        _zitadelAuthority = configuration["Zitadel:Authority"]!;
        _organizationId = configuration["Zitadel:OrganizationId"]; // Opcional: contexto organizacional
        _logger = logger;
        _cache = cache;
        
        try
        {
            _logger.LogInformation("Inicializando ZitadelUserInfoService con Authority: {Authority}, OrganizationId: {OrgId}", 
                _zitadelAuthority, _organizationId ?? "(service account default org)");
            
            // Usar ManagementService con contexto organizacional
            var options = new Clients.Options(_zitadelAuthority, _tokenProvider)
            {
                // Especificar organización si está configurada
                Organization = _organizationId
            };
            _managementClient = Clients.ManagementService(options);
            
            _logger.LogInformation("ZitadelUserInfoService inicializado correctamente con ManagementService");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar ZitadelUserInfoService: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<UserInfoDto?> GetUserInfoAsync(string userId)
    {
        var cacheKey = $"user_info_{userId}";

        if (_cache.TryGetValue(cacheKey, out UserInfoDto? cachedUser))
        {
            return cachedUser;
        }

        try
        {
            _logger.LogInformation("=== INICIO: Obteniendo información del usuario {UserId} desde Zitadel Management API ===", userId);

            // Llamar a la API de Management de Zitadel usando el SDK
            var request = new GetUserByIDRequest
            {
                Id = userId
            };

            _logger.LogInformation("Enviando request a Zitadel Management API para usuario {UserId}", userId);
            var response = await _managementClient.GetUserByIDAsync(request);
            _logger.LogInformation("Respuesta recibida de Zitadel para usuario {UserId}", userId);
            
            if (response?.User == null)
            {
                _logger.LogWarning("Usuario {UserId} - La respuesta no contiene datos de usuario", userId);
                return CreateUnknownUserInfo(userId);
            }

            _logger.LogInformation("Usuario {UserId} encontrado en Zitadel. Tipo: {UserType}", 
                userId, 
                response.User.Human != null ? "Human" : (response.User.Machine != null ? "Machine" : "Unknown"));

            // Extraer información del usuario
            var userInfo = ExtractUserInfo(response.User);

            // Cachear por 15 minutos
            _cache.Set(cacheKey, userInfo, TimeSpan.FromMinutes(15));

            _logger.LogInformation("=== ÉXITO: Usuario {UserId} obtenido correctamente: {Name} ({Email}) ===", 
                userId, userInfo.Name, userInfo.Email);

            return userInfo;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("=== ERROR: Usuario {UserId} no encontrado en Zitadel (404) ===", userId);
            return CreateUnknownUserInfo(userId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            _logger.LogError(ex, "=== ERROR PERMISOS: Sin permisos para obtener usuario {UserId}. " +
                "Verifica que la service account tenga rol ORG_OWNER o ORG_OWNER_VIEWER ===", userId);
            return CreateUnknownUserInfo(userId);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "=== ERROR gRPC: Error al obtener usuario {UserId}. Status: {Status}, Detail: {Detail}, " +
                "StatusCode: {StatusCode}, Trailers: {Trailers} ===", 
                userId, ex.Status, ex.Status.Detail, ex.StatusCode, 
                string.Join(", ", ex.Trailers.Select(t => $"{t.Key}={t.Value}")));
            return CreateUnknownUserInfo(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR GENERAL: Error inesperado al obtener información del usuario {UserId}. " +
                "Tipo: {ExceptionType}, Mensaje: {Message}, StackTrace: {StackTrace} ===", 
                userId, ex.GetType().Name, ex.Message, ex.StackTrace);
            return CreateUnknownUserInfo(userId);
        }
    }

    private UserInfoDto ExtractUserInfo(Zitadel.User.V1.User user)
    {
        var userInfo = new UserInfoDto
        {
            UserId = user.Id
        };

        // Zitadel puede tener diferentes tipos de usuarios (Human, Machine)
        if (user.Human != null)
        {
            // Usuario humano
            var profile = user.Human.Profile;
            
            // Nombre: priorizar DisplayName, luego FirstName + LastName, finalmente UserName
            if (!string.IsNullOrWhiteSpace(profile?.DisplayName))
            {
                userInfo.Name = profile.DisplayName;
            }
            else if (!string.IsNullOrWhiteSpace(profile?.FirstName) || !string.IsNullOrWhiteSpace(profile?.LastName))
            {
                userInfo.Name = $"{profile?.FirstName ?? ""} {profile?.LastName ?? ""}".Trim();
            }
            else
            {
                userInfo.Name = user.UserName ?? "Usuario Desconocido";
            }
            
            // Email: usar el email verificado si existe
            userInfo.Email = user.Human.Email?.Email_ ?? user.PreferredLoginName ?? "no-email@example.com";
        }
        else if (user.Machine != null)
        {
            // Usuario máquina/servicio
            userInfo.Name = user.Machine.Name ?? "Service Account";
            userInfo.Email = $"{user.Machine.Name ?? "service"}@machine-account";
        }
        else
        {
            // Fallback
            userInfo.Name = user.UserName ?? "Usuario Desconocido";
            userInfo.Email = user.PreferredLoginName ?? $"{user.UserName}@example.com";
        }

        return userInfo;
    }

    private static UserInfoDto CreateUnknownUserInfo(string userId)
    {
        return new UserInfoDto
        {
            UserId = userId,
            Name = "Usuario Desconocido",
            Email = "unknown@email.com"
        };
    }

    public async Task<Dictionary<string, UserInfoDto>> GetUsersInfoAsync(
        IEnumerable<string> userIds)
    {
        var tasks = userIds.Distinct().Select(async id =>
            new { Id = id, Info = await GetUserInfoAsync(id) });

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r.Info != null)
            .ToDictionary(r => r.Id, r => r.Info!);
    }
}
