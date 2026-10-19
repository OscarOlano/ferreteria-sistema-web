using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;

namespace FerreteriaWeb.Controllers
{
    public class ProductosController : Controller
    {
        private readonly FerreteriaDbContext _context;

        public ProductosController(FerreteriaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? buscar, int? categoriaId, string? estado)
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.UnidadMedida)
                .Include(p => p.Ubicacion)
                .Where(p => p.Activo);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();
                query = query.Where(p => p.Descripcion.ToLower().Contains(b) 
                                      || p.Codigo.ToLower().Contains(b) 
                                      || (p.CodigoBarras != null && p.CodigoBarras.Contains(b)));
            }

            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado == "bajo")
                    query = query.Where(p => p.StockActual <= p.StockMinimo && p.StockActual > 0);
                else if (estado == "agotado")
                    query = query.Where(p => p.StockActual <= 0);
                else if (estado == "normal")
                    query = query.Where(p => p.StockActual > p.StockMinimo);
            }

            ViewBag.Categorias = new SelectList(await _context.Categorias.Where(c => c.Activo).ToListAsync(), "Id", "Nombre", categoriaId);
            ViewBag.Buscar = buscar;
            ViewBag.Estado = estado;

            var productos = await query.OrderBy(p => p.Descripcion).ToListAsync();
            return View(productos);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.UnidadMedida)
                .Include(p => p.Ubicacion)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        public async Task<IActionResult> Create()
        {
            await CargarCombosAsync();
            return View(new Producto { Codigo = $"P-{DateTime.Now.Ticks % 10000:0000}", StockMinimo = 5, StockMaximo = 500 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (await _context.Productos.AnyAsync(p => p.Codigo == producto.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un producto con este código.");
            }

            if (ModelState.IsValid)
            {
                producto.CostoPromedio = producto.PrecioCompra;
                _context.Add(producto);
                await _context.SaveChangesAsync();

                // Kárdex inicial si tiene stock
                if (producto.StockActual > 0)
                {
                    _context.Kardex.Add(new Kardex
                    {
                        ProductoId = producto.Id,
                        Fecha = DateTime.Now,
                        Operacion = "Inventario Inicial",
                        DocumentoRef = "ALTA-PROD",
                        CantidadEntrada = producto.StockActual,
                        CostoEntrada = producto.PrecioCompra,
                        TotalEntrada = producto.StockActual * producto.PrecioCompra,
                        SaldoCantidad = producto.StockActual,
                        CostoPromedio = producto.PrecioCompra,
                        SaldoTotal = producto.StockActual * producto.PrecioCompra
                    });
                    await _context.SaveChangesAsync();
                }

                TempData["Mensaje"] = $"Producto '{producto.Descripcion}' registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            await CargarCombosAsync(producto);
            return View(producto);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            await CargarCombosAsync(producto);
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (id != producto.Id) return NotFound();

            if (await _context.Productos.AnyAsync(p => p.Codigo == producto.Codigo && p.Id != id))
            {
                ModelState.AddModelError("Codigo", "Ya existe otro producto con este código.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var prodOriginal = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (prodOriginal != null)
                    {
                        // Conservamos el stock actual y costo promedio recalculado
                        producto.StockActual = prodOriginal.StockActual;
                        producto.CostoPromedio = prodOriginal.CostoPromedio;
                    }

                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = $"Producto '{producto.Descripcion}' actualizado con éxito.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Productos.AnyAsync(e => e.Id == producto.Id))
                        return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await CargarCombosAsync(producto);
            return View(producto);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Marca)
                .Include(p => p.UnidadMedida)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                producto.Activo = false; // Borrado lógico para proteger historial y kárdex
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"El producto '{producto.Descripcion}' fue desactivado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombosAsync(Producto? p = null)
        {
            ViewBag.CategoriaId = new SelectList(await _context.Categorias.Where(c => c.Activo).ToListAsync(), "Id", "Nombre", p?.CategoriaId);
            ViewBag.MarcaId = new SelectList(await _context.Marcas.Where(m => m.Activo).ToListAsync(), "Id", "Nombre", p?.MarcaId);
            ViewBag.UnidadMedidaId = new SelectList(await _context.UnidadesMedida.ToListAsync(), "Id", "Nombre", p?.UnidadMedidaId);
            
            var ubicaciones = await _context.Ubicaciones
                .Select(u => new { u.Id, Descripcion = $"{u.Pasillo}-{u.Estante}-{u.Nivel}" })
                .ToListAsync();
            ViewBag.UbicacionId = new SelectList(ubicaciones, "Id", "Descripcion", p?.UbicacionId);
        }
    }
}
