using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace ApprovalRequestsApi.Api.Auth;

public class ZitadelRolesClaimsTransformation : IClaimsTransformation
{
    private readonly ILogger<ZitadelRolesClaimsTransformation> _logger;

    public ZitadelRolesClaimsTransformation(ILogger<ZitadelRolesClaimsTransformation> logger)
    {
        _logger = logger;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var claimsIdentity = (ClaimsIdentity)principal.Identity!;

        // Log todos los claims para depuración
        _logger.LogInformation("=== Claims recibidos ===");
        foreach (var claim in principal.Claims)
        {
            _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
        }

        // Buscar roles en los claims de Zitadel
        // Pueden estar en diferentes claims dependiendo de cómo Zitadel los devuelve
        var roleClaims = principal.Claims
            .Where(c => c.Type.Contains("role", StringComparison.OrdinalIgnoreCase) ||
                       c.Type.Contains("urn:zitadel:iam:org:project", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Si encontramos claims de rol de Zitadel, extraer los nombres de rol
        foreach (var claim in roleClaims)
        {
            // El valor del claim puede ser JSON con los roles
            // Por ejemplo: {"Admin": {...}, "User": {...}}
            if (claim.Value.Contains("{") && claim.Value.Contains("}"))
            {
                // Intentar extraer los nombres de rol del JSON
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(claim.Value);
                    foreach (var property in json.RootElement.EnumerateObject())
                    {
                        // Agregar cada rol como claim de rol estándar
                        if (!claimsIdentity.HasClaim(ClaimTypes.Role, property.Name))
                        {
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, property.Name));
                        }
                    }
                }
                catch
                {
                    // Si no es JSON, usar el valor directamente
                    if (!claimsIdentity.HasClaim(ClaimTypes.Role, claim.Value))
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(claim.Value))
            {
                // Si no es JSON, usar el valor directamente
                if (!claimsIdentity.HasClaim(ClaimTypes.Role, claim.Value))
                {
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
