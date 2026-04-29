using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SASMS.Models;
using SASMS.Services;

namespace SASMS.Filters
{
    public class BreadcrumbFilter : IActionFilter
    {
        private readonly IBreadcrumbService _breadcrumbService;

        public BreadcrumbFilter(IBreadcrumbService breadcrumbService)
        {
            _breadcrumbService = breadcrumbService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Initial breadcrumb generation
            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString() ?? "";

            // Add Dashboard as Root
            _breadcrumbService.Add("Dashboard", "/Land/Dashboard");

            // Add Controller if it's not Land (Home)
            if (controller != "Land")
            {
                _breadcrumbService.Add(controller, $"/{controller}/Index");
            }

            // Add Action if it's not Index or Dashboard
            if (action != "Index" && action != "Dashboard")
            {
                _breadcrumbService.Add(action, null, true);
            }
            else if (controller != "Land")
            {
                 // Mark the controller as active if we are on its Index
                 var breadcrumbs = _breadcrumbService.GetBreadcrumbs().ToList();
                 if (breadcrumbs.Count > 0)
                 {
                     breadcrumbs.Last().IsActive = true;
                     breadcrumbs.Last().Url = null;
                 }
            }
            else
            {
                // We are on Land/Dashboard
                var breadcrumbs = _breadcrumbService.GetBreadcrumbs().ToList();
                if (breadcrumbs.Count > 0)
                {
                    breadcrumbs.Last().IsActive = true;
                    breadcrumbs.Last().Url = null;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ViewResult viewResult)
            {
                // Check for manual overrides in ViewData
                if (viewResult.ViewData["CustomBreadcrumb"] is List<BreadcrumbItem> customItems)
                {
                    _breadcrumbService.SetCustom(customItems);
                }
                else if (viewResult.ViewData["CustomBreadcrumb"] is string customTitle)
                {
                    _breadcrumbService.Clear();
                    _breadcrumbService.Add("Dashboard", "/Land/Dashboard");
                    _breadcrumbService.Add(customTitle, null, true);
                }
            }
        }
    }
}
