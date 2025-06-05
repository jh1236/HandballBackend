using System.Security.Claims;
using System.Text.Encodings.Web;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HandballBackend.Authentication;

public class TokenAuthenticator : AuthenticationHandler<AuthenticationSchemeOptions> {
    public TokenAuthenticator(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) {
    }

    //RiderIgnore
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.ContainsKey("Authorization")) {
            return AuthenticateResult.NoResult();
        }

        if (PermissionHelper.TryGetUser(out var person)) {
            return AuthenticateResult.Fail("Invalid Token");
        }

        List<Claim> claims = [
            new Claim(ClaimTypes.NameIdentifier, person.SearchableName),
            new Claim(ClaimTypes.Name, person.Name)
        ];
        claims.AddRange(Enum.GetValues(typeof(PermissionType))
            .Cast<PermissionType>()
            .Where(permission => permission.ToInt() <= person.PermissionLevel)
            .Select(permission => new Claim(ClaimTypes.Role, permission.ToString())));


        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}