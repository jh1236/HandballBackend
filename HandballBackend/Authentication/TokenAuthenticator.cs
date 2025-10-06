using System.Security.Claims;
using System.Text.Encodings.Web;
using HandballBackend.Database.Models;
using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HandballBackend.Authentication;

public class TokenAuthenticator : AuthenticationHandler<AuthenticationSchemeOptions> {
    public TokenAuthenticator(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.ContainsKey("Authorization")) {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = Request.Headers.Authorization.ToString().Split(" ")[1];
        var person = PermissionHelper.PersonByToken(token);

        if (person == null || person.TokenTimeout < Utilities.GetUnixSeconds()) {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Token"));
        }

        List<Claim> claims = [
            new(CustomClaimTypes.SearchableName, person.SearchableName),
            new(ClaimTypes.Name, person.Name),
            new(CustomClaimTypes.UserId, person.Id.ToString()),
            new(CustomClaimTypes.Token, person.SessionToken!)
        ];
        claims.AddRange(Enum.GetValues<PermissionType>()
            .Where(permission => permission <= person.PermissionLevel)
            .Select(permission => new Claim(ClaimTypes.Role, permission.ToString())));


        var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}