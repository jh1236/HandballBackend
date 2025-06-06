using System.Text;
using HandballBackend.Database.Models;
using HandballBackend.Utils;

namespace HandballBackend.EndpointHelpers;

using BCrypt.Net;

public enum PermissionType {
    LoggedIn,
    Umpire,
    UmpireManager,
    Admin,
}

public static class PermissionHelper {
    public static int ToInt(this PermissionType permissionType) {
        return permissionType switch {
            PermissionType.LoggedIn => 0,
            PermissionType.Umpire => 2,
            PermissionType.UmpireManager => 4,
            PermissionType.Admin => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(permissionType), permissionType, null)
        };
    }

    public static PermissionType IntToPermissionType(int permissionType) {
        return permissionType switch {
            1 => PermissionType.LoggedIn,
            2 or 3 => PermissionType.Umpire,
            4 => PermissionType.UmpireManager,
            5 => PermissionType.Admin,
            _ => throw new ArgumentOutOfRangeException(nameof(permissionType), permissionType, null)
        };
    }

    private static bool PersonOrElse(HandballContext db, int personId, out Person person) {
        person = db.People.Find(personId);
        return person is not null;
    }

    private static int Time() {
        return Utilities.GetUnixSeconds();
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
        if (realPassword == null) {
            throw new ArgumentNullException(nameof(personId), "The given person has no password.");
        }

        return BCrypt.Verify(checkPassword, realPassword);
    }


    private static bool CheckToken(int personId, string token) {
        var db = new HandballContext();
        if (!PersonOrElse(db, personId, out var person)) {
            throw new KeyNotFoundException($"Person with id {personId} not found");
        }

        return person.SessionToken == token;
    }

    private static void ResetTokenForPerson(int personId) {
        var db = new HandballContext();
        if (!PersonOrElse(db, personId, out var person)) {
            throw new KeyNotFoundException($"Person with id {personId} not found");
        }

        person.SessionToken = null;
        person.TokenTimeout = null;
        db.SaveChanges();
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

        if (person.SessionToken is null || person.TokenTimeout < Time() + 60 * 60) //an hour
        {
            //our old token either didn't exist, wasn't valid or was about to expire.  New One!!

            var ret = GenerateToken();
            person.SessionToken = ret;
            if (longSession) {
                person.TokenTimeout = Time() + 60 * 60 * 24 * 7; //One week long token
            } else {
                person.TokenTimeout = Time() + 60 * 60 * 12; //Twelve hour token
            }

            db.SaveChanges();
        }

        return person;
    }

    public static Person? PersonByToken(string? token) {
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

    public static void Logout(int id) {
        ResetTokenForPerson(id);
    }
}