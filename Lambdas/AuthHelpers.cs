using Amazon.Lambda.APIGatewayEvents;
using System.Collections.Generic;

namespace Lambdas;

internal static class AuthHelpers
{
    public static string? GetUserId(APIGatewayProxyRequest? request)
    {
        var claims = request?.RequestContext?.Authorizer?.Claims;
        if (claims is null)
            return null;

        return claims.TryGetValue("sub", out var sub) ? sub : null;
    }

    public static Dictionary<string, string> CreateCorsHeaders(APIGatewayProxyRequest? request, params string[] allowedMethods)
    {
        var origin = "*";
        if (request?.Headers is not null)
        {
            if (request.Headers.TryGetValue("origin", out var lowerOrigin) && !string.IsNullOrWhiteSpace(lowerOrigin))
                origin = lowerOrigin;
            else if (request.Headers.TryGetValue("Origin", out var upperOrigin) && !string.IsNullOrWhiteSpace(upperOrigin))
                origin = upperOrigin;
        }

        return new Dictionary<string, string>
        {
            ["Access-Control-Allow-Origin"] = origin,
            ["Access-Control-Allow-Headers"] = "Content-Type,Authorization",
            ["Access-Control-Allow-Methods"] = string.Join(',', allowedMethods),
            ["Vary"] = "Origin",
            ["Content-Type"] = "application/json"
        };
    }
}
