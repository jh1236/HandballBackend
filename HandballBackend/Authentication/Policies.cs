using System.Security.Claims;
using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Authorization;

namespace HandballBackend.Authentication;

public static class Policies {
    public const string IsAdmin = nameof(IsAdmin);

    public static void RegisterPolicies(AuthorizationOptions options) {
        options.AddPolicy(IsAdmin, policy => policy
            .RequireAssertion(c =>
                c.User.HasClaim(c =>
                    c.Type == ClaimTypes.Role && c.Value == PermissionType.Admin.ToString())));
    }
}