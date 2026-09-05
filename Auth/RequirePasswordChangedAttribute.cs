using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace patient_api.Auth;

// Server-side enforcement that a patient still on the default password cannot reach
// vitals/tasks/profile endpoints — the Angular UI also blocks this, but that's not trustworthy.
public class RequirePasswordChangedAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.GetMustChangePassword())
        {
            context.Result = new ObjectResult(new { error = "Password change required." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
