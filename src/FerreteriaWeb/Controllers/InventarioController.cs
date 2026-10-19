using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;
using FerreteriaWeb.Services;

namespace FerreteriaWeb.Controllers
{
    public class InventarioController : Controller
    {
        private readonly FerreteriaDbContext _context;
        private readonly IInventarioService _inventarioService;

        public InventarioController(FerreteriaDbContext context, IInventarioService inventarioService)
        {
            _context = context;
            _inventarioService = inventarioService;
        }

        // ================= ENTRADA DE MERCADERÍA =================
        public async Task<IActionResult> Entrada()
        {
            await CargarCombosEntradaAsync();
            return View(new RegistrarEntradaDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entrada(RegistrarEntradaDto dto)
        {
            if (dto.Items == null || !dto.Items.Any(i => i.ProductoId > 0 && i.Cantidad > 0))
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un producto e ingresar una cantidad mayor a cero.");
                await CargarCombosEntradaAsync();
                return View(dto);
            }

            dto.Items = dto.Items.Where(i => i.ProductoId > 0 && i.Cantidad > 0).ToList();

            var resultado = await _inventarioService.RegistrarEntradaAsync(dto);
            if (resultado.Exito)
            {
                TempData["Mensaje"] = resultado.Mensaje;
                return RedirectToAction(nameof(Movimientos));
            }

            ModelState.AddModelError("", resultado.Mensaje);
            await CargarCombosEntradaAsync();
            return View(dto);
        }

        // ================= SALIDA DE MERCADERÍA =================
        public async Task<IActionResult> Salida()
        {
            await CargarCombosSalidaAsync();
            return View(new RegistrarSalidaDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salida(RegistrarSalidaDto dto)
        {
            if (dto.Items == null || !dto.Items.Any(i => i.ProductoId > 0 && i.Cantidad > 0))
            {
                ModelState.AddModelError("", "Debe seleccionar al menos un producto con cantidad válida.");
                await CargarCombosSalidaAsync();
                return View(dto);
            }

            dto.Items = dto.Items.Where(i => i.ProductoId > 0 && i.Cantidad > 0).ToList();

            var resultado = await _inventarioService.RegistrarSalidaAsync(dto);
            if (resultado.Exito)
            {
                TempData["Mensaje"] = resultado.Mensaje;
                return RedirectToAction(nameof(Movimientos));
            }

            ModelState.AddModelError("", resultado.Mensaje);
            await CargarCombosSalidaAsync();
            return View(dto);
        }

        // ================= KÁRDEX VALORIZADO =================
        public async Task<IActionResult> Kardex(int? productoId, DateTime? desde, DateTime? hasta)
        {
            var productos = await _context.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Descripcion)
                .ToListAsync();

            ViewBag.ProductoId = new SelectList(productos, "Id", "Descripcion", productoId);
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

            Producto? productoSeleccionado = null;
            List<Kardex> movimientosKardex = new();

            if (productoId.HasValue && productoId.Value > 0)
            {
                productoSeleccionado = productos.FirstOrDefault(p => p.Id == productoId.Value);
                movimientosKardex = await _inventarioService.GetKardexByProductoAsync(productoId.Value, desde, hasta);
            }
            else if (productos.Any())
            {
                // Por defecto muestra el primer producto
                productoSeleccionado = productos.First();
                ViewBag.ProductoId = new SelectList(productos, "Id", "Descripcion", productoSeleccionado.Id);
                movimientosKardex = await _inventarioService.GetKardexByProductoAsync(productoSeleccionado.Id, desde, hasta);
            }

            ViewBag.ProductoSeleccionado = productoSeleccionado;
            return View(movimientosKardex);
        }

        // ================= ALERTAS DE STOCK MÍNIMO =================
        public async Task<IActionResult> Alertas()
        {
            var productosEnAlerta = await _inventarioService.GetAlertasStockAsync();
            return View(productosEnAlerta);
        }

        // ================= HISTORIAL DE MOVIMIENTOS =================
        public async Task<IActionResult> Movimientos()
        {
            var movimientos = await _context.Movimientos
                .Include(m => m.Proveedor)
                .Include(m => m.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p!.UnidadMedida)
                .OrderByDescending(m => m.Fecha)
                .Take(50)
                .ToListAsync();

            return View(movimientos);
        }

        private async Task CargarCombosEntradaAsync()
        {
            ViewBag.Proveedores = new SelectList(await _context.Proveedores.Where(p => p.Activo).ToListAsync(), "Id", "RazonSocial");
            ViewBag.ProductosJson = await _context.Productos
                .Where(p => p.Activo)
                .Select(p => new { id = p.Id, codigo = p.Codigo, descripcion = p.Descripcion, precio = p.PrecioCompra, stock = p.StockActual })
                .ToListAsync();
        }

        private async Task CargarCombosSalidaAsync()
        {
            ViewBag.ProductosJson = await _context.Productos
                .Where(p => p.Activo && p.StockActual > 0)
                .Select(p => new { id = p.Id, codigo = p.Codigo, descripcion = p.Descripcion, stock = p.StockActual, precio = p.PrecioVenta })
                .ToListAsync();
        }
    }
}
