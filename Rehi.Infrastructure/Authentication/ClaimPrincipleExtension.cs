using System.Security.Claims;

namespace Rehi.Infrastructure.Authentication;

public static class ClaimPrincipleExtension
{
    // public static Guid GetUserId(this ClaimsPrincipal? principal)
    // {
    //     string? userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    //
    //     return Guid.TryParse(userId, out Guid parsedUserId) ?
    //         parsedUserId :
    //         throw new ApplicationException("User id is unavailable");
    // }
    
    public static string GetEmail(this ClaimsPrincipal? principal)
    {
        string? email = principal?.FindFirstValue(ClaimTypes.Email);

        return email ?? throw new ApplicationException("User email is unavailable");
    }
    
}