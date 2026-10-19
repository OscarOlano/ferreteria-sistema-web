using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;

namespace FerreteriaWeb.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly FerreteriaDbContext _context;

        public ProveedoresController(FerreteriaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new Proveedor());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            if (await _context.Proveedores.AnyAsync(p => p.Ruc == proveedor.Ruc))
            {
                ModelState.AddModelError("Ruc", "Ya existe un proveedor registrado con este RUC.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(proveedor);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Proveedor '{proveedor.RazonSocial}' registrado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Proveedor proveedor)
        {
            if (id != proveedor.Id) return NotFound();

            if (await _context.Proveedores.AnyAsync(p => p.Ruc == proveedor.Ruc && p.Id != id))
            {
                ModelState.AddModelError("Ruc", "Ya existe otro proveedor con este RUC.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(proveedor);
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Proveedor '{proveedor.RazonSocial}' actualizado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);
            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                proveedor.Activo = false;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"Proveedor '{proveedor.RazonSocial}' desactivado.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
