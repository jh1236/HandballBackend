using HandballBackend.EndpointHelpers;
using HandballBackend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HandballBackend.Authentication;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TournamentAuthorizeAttribute(PermissionType requiredRole, string routeKey = "searchable")
    : Attribute, IAuthorizationFilter {
    public void OnAuthorization(AuthorizationFilterContext context) {
        var db = new HandballContext();
        var token = context.HttpContext.User.Claims.First(c => c.Type == CustomClaimTypes.Token).Value;
        var person = PermissionHelper.PersonByToken(token);

        if (person is null) {
            context.Result = new UnauthorizedResult();
            return;
        }

        var tournamentSearch = context.HttpContext.Request.Query["tournament"].FirstOrDefault();
        if (tournamentSearch is null) {

            if (!context.RouteData.Values.TryGetValue(routeKey, out var rawSearchable) ||
                rawSearchable is not string searchable) {
                //this is not a tournament-specific request
                if (person.PermissionLevel.ToInt() < requiredRole.ToInt()) {
                    context.Result = new ForbidResult();
                }
                return;
            } else {
                tournamentSearch = searchable;
            }
        }

        if (!Utilities.TournamentOrElse(db, tournamentSearch, out var tournament)) {
            context.Result = new BadRequestObjectResult("Invalid tournament");
            return;
        }

        if (person.PermissionLevel >= PermissionType.Admin) {
            return;
        }

        var to = db.TournamentOfficials.First(to =>
            to.TournamentId == tournament!.Id &&
            to.Official.PersonId == person.Id);

        if (to.Role.ToInt() < requiredRole.ToInt()) {
            context.Result = new ForbidResult();
        }
    }
}