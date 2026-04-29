using System.Collections.Generic;
using SASMS.Models;

namespace SASMS.Services
{
    public interface IBreadcrumbService
    {
        void Add(string title, string? url = null, bool isActive = false);
        void SetCustom(List<BreadcrumbItem> items);
        IEnumerable<BreadcrumbItem> GetBreadcrumbs();
        void Clear();
    }
}
