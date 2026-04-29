using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace SASMS.Middleware
{
    public class CultureMiddleware
    {
        private readonly RequestDelegate _next;

        public CultureMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var cultureQuery = context.Request.Query["culture"];
            var cultureCookie = context.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
            
            string culture = "en"; // Default

            // 1. Priority: Query String (good for debugging)
            if (!string.IsNullOrEmpty(cultureQuery))
            {
                culture = cultureQuery;
            }
            // 2. Priority: Cookie
            else if (!string.IsNullOrEmpty(cultureCookie))
            {
                // Cookie value format is usually "c=ar|uic=ar"
                var parts = CookieRequestCultureProvider.ParseCookieValue(cultureCookie);
                if (parts != null && parts.Cultures.Count > 0)
                {
                    culture = parts.Cultures.First().Value;
                }
            }

            // Ensure culture is supported
            if (culture != "ar" && culture != "en")
            {
                culture = "en";
            }

            var cultureInfo = new CultureInfo(culture);
            
            // Critical: Set thread culture manually
            CultureInfo.CurrentCulture = cultureInfo;
            CultureInfo.CurrentUICulture = cultureInfo;

            await _next(context);
        }
    }
}
