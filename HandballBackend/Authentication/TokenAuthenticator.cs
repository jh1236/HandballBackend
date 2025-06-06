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

    //RiderIgnore
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.ContainsKey("Authorization")) {
            return AuthenticateResult.NoResult();
        }

        var token = Request.Headers.Authorization.ToString().Split(" ")[1];
        var person = PermissionHelper.PersonByToken(token);

        if (person == null || person.TokenTimeout < Utilities.GetUnixSeconds()) {
            return AuthenticateResult.Fail("Invalid Token");
        }

        List<Claim> claims = [
            new Claim(CustomClaimTypes.SearchableName, person.SearchableName),
            new Claim(ClaimTypes.Name, person.Name),
            new Claim(CustomClaimTypes.UserId, person.Id.ToString()),
            new Claim(CustomClaimTypes.Token, person.SessionToken!)
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