using System.Security.Claims;

namespace patient_api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static int GetPatientId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("patient_id")?.Value
            ?? throw new InvalidOperationException("Token has no patient_id claim.");
        return int.Parse(claim);
    }

    public static bool GetMustChangePassword(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("must_change_password")?.Value;
        return claim == "true";
    }
}
