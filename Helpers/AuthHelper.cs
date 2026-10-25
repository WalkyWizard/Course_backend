using SNovimGodom.Data;
using SNovimGodom.Models;

namespace SNovimGodom.Helpers;

public class AuthHelper
{
    public static User? GetUser(HttpRequest request, AppDbContext db)
    {
        var rawAuthorization = request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(rawAuthorization)) return null;

        var email = rawAuthorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? rawAuthorization["Bearer ".Length..].Trim()
            : rawAuthorization.Trim();

        if (email == null) return null;

        return db.Users.FirstOrDefault(x => x.Email == email);
    }
}
