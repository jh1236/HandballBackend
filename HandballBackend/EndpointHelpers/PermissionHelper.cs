using System.Text;
using HandballBackend.Models;

namespace HandballBackend.EndpointHelpers;

using BCrypt.Net;

public enum PermissionType {
    LoggedIn,
    Umpire,
    UmpireManager,
    Admin,
}

public static class PermissionHelper {
    private static int PermissionTypeToInt(PermissionType permissionType) {
        return permissionType switch {
            PermissionType.LoggedIn => 0,
            PermissionType.Umpire => 2,
            PermissionType.UmpireManager => 4,
            PermissionType.Admin => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(permissionType), permissionType, null)
        };
    }

    private static bool PersonOrElse(HandballContext db, int personId, out Person person) {
        person = db.People.Find(personId);
        return person is not null;
    }

    private static int Time() {
        return (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static string GenerateToken() {
        return Guid.NewGuid().ToString("N");
    }

    private static string Encrypt(string password) {
        var salt = BCrypt.GenerateSalt(12);
        var pwd = BCrypt.HashPassword(password, salt);
        return pwd;
    }


    private static bool CheckPassword(int personId, string checkPassword) {
        var db = new HandballContext();
        if (!PersonOrElse(db, personId, out var person)) {
            return false;
        }

        var realPassword = person.Password;
        return BCrypt.Verify(checkPassword, realPassword);
    }


    private static bool CheckToken(int personId, string token) {
        var db = new HandballContext();
        if (!PersonOrElse(db, personId, out var person)) {
            throw new KeyNotFoundException($"Person with id {personId} not found");
        }

        return person.SessionToken == token;
    }

    private static string? GetToken() {
        // Access the current HTTP context
        var httpContext = new HttpContextAccessor().HttpContext;
        if (httpContext == null) {
            return null;
        }

        // Get the Authorization header
        var authHeader = httpContext.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ")) {
            return null;
        }

        // Extract the token from the header
        return authHeader["Bearer ".Length..].Trim();
    }

    private static Person? GetPersonFromToken(string? token) {
        var db = new HandballContext();
        if (token == null) {
            return null;
        }

        var person = db.People.FirstOrDefault(p => p.SessionToken == token);

        if (person == null) return null;

        if (person.TokenTimeout < Time()) {
            ResetTokenForPerson(person.Id);
            return null;
        }

        return person;
    }

    public static Person? GetUser() {
        return GetPersonFromToken(GetToken());
    }

    public static bool HasPermission(PermissionType permission) {
        var perms = PermissionTypeToInt(permission);
        var user = GetUser();
        if (user == null) return false;
        return user.PermissionLevel >= perms;
    }

    public static void SetPassword(int personId, string password) {
        var db = new HandballContext();
        if (!PersonOrElse(db, personId, out var person)) {
            throw new KeyNotFoundException($"Person with id {personId} not found");
        }

        person.Password = Encrypt(password);
        db.SaveChanges();
    }

    public static Person? Login(int personId, string password, bool longSession = false) {
        var pwCheck = CheckPassword(personId, password);
        var db = new HandballContext();
        if (!pwCheck) {
            return null;
        }

        if (!PersonOrElse(db, personId, out var person)) {
            return null;
        }

        var ret = GenerateToken();
        person.SessionToken = ret;
        if (longSession) {
            person.TokenTimeout = Time() + 60 * 60 * 24 * 7; //One week long token
        } else {
            person.TokenTimeout = Time() + 60 * 60 * 2; //Two hour token
        }

        db.SaveChanges();
        return person;
    }

    public static void ResetTokenForPerson(int personId) {
        var db = new HandballContext();
        if (!PersonOrElse(db, personId, out var person)) {
            throw new KeyNotFoundException($"Person with id {personId} not found");
        }

        person.SessionToken = null;
        person.TokenTimeout = null;
        db.SaveChanges();
    }
}