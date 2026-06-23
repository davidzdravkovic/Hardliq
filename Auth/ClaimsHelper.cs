using System.Security.Claims;

namespace TaskManager.Auth;

public static class ClaimsHelper
{
    public static AuthenticatedUser GetAuthenticatedUser(ClaimsPrincipal user)
    {
        var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User id claim is missing.");

        if (!int.TryParse(idValue, out var id))
            throw new InvalidOperationException("User id claim is not a valid integer.");

        var username = user.FindFirstValue(ClaimTypes.Name)
            ?? throw new InvalidOperationException("Username claim is missing.");

        return new AuthenticatedUser(id, username);
    }
}
