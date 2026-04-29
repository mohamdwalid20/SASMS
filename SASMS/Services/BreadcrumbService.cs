using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using SASMS.Models;

namespace SASMS.Services
{
    public class BreadcrumbService : IBreadcrumbService
    {
        private readonly List<BreadcrumbItem> _breadcrumbs = new();
        private readonly IStringLocalizer<SharedResources> _localizer;

        public BreadcrumbService(IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
        }

        public void Add(string title, string? url = null, bool isActive = false)
        {
            _breadcrumbs.Add(new BreadcrumbItem 
            { 
                Title = FormatTitle(title), 
                Url = url, 
                IsActive = isActive 
            });
        }

        public void SetCustom(List<BreadcrumbItem> items)
        {
            _breadcrumbs.Clear();
            _breadcrumbs.AddRange(items);
        }

        public IEnumerable<BreadcrumbItem> GetBreadcrumbs()
        {
            return _breadcrumbs;
        }

        public void Clear()
        {
            _breadcrumbs.Clear();
        }

        private string FormatTitle(string title)
        {
            // Attempt localization first
            var localized = _localizer[title];
            if (!localized.ResourceNotFound)
            {
                return localized.Value;
            }

            // Fallback: Human-readable spacing (e.g. StudentPortal -> Student Portal)
            // Splitting camel case
            return Regex.Replace(title, "([a-z])([A-Z])", "$1 $2");
        }
    }
}
