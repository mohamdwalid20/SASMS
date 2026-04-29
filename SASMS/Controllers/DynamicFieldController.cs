using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SASMS.Data;
using SASMS.Models;

namespace SASMS.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class DynamicFieldController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DynamicFieldController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DynamicField
        public async Task<IActionResult> Index()
        {
            var fields = await _context.DynamicFields
                .OrderBy(f => f.Section)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();
            return View(fields);
        }

        // GET: DynamicField/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DynamicField/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DynamicField dynamicField)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dynamicField);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Dynamic field created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(dynamicField);
        }

        // GET: DynamicField/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var dynamicField = await _context.DynamicFields.FindAsync(id);
            if (dynamicField == null) return NotFound();

            return View(dynamicField);
        }

        // POST: DynamicField/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DynamicField dynamicField)
        {
            if (id != dynamicField.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dynamicField);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Dynamic field updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DynamicFieldExists(dynamicField.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(dynamicField);
        }

        // POST: DynamicField/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var dynamicField = await _context.DynamicFields.FindAsync(id);
            if (dynamicField != null)
            {
                // Check if there are values associated with this field
                var hasValues = await _context.DynamicFieldValues.AnyAsync(v => v.FieldId == id);
                if (hasValues)
                {
                    // Soft delete or just deactivate to preserve history?
                    // User said "يمسح حقل", so let's delete if no values, otherwise maybe deactivate
                    // But for simplicity in this requirement, let's delete
                    _context.DynamicFields.Remove(dynamicField);
                }
                else
                {
                    _context.DynamicFields.Remove(dynamicField);
                }
                
                await _context.SaveChangesAsync();
                TempData["Success"] = "Dynamic field deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DynamicFieldExists(int id)
        {
            return _context.DynamicFields.Any(e => e.Id == id);
        }
    }
}
