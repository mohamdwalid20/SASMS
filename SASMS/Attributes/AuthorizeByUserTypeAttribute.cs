using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SASMS.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeByUserTypeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _allowedUserTypes;

        public AuthorizeByUserTypeAttribute(params string[] allowedUserTypes)
        {
            _allowedUserTypes = allowedUserTypes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
                return;
            }

            var userType = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (userType == null || !_allowedUserTypes.Contains(userType))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}