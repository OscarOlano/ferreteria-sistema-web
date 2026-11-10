using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;
using FerreteriaWeb.Services;

namespace FerreteriaWeb.Controllers
{
    [Authorize]
    public class InventarioController : Controller
    {
        private readonly FerreteriaDbContext _context;
        private readonly IInventarioService _inventarioService;

        public InventarioController(FerreteriaDbContext context, IInventarioService inventarioService)
        {
            _context = context;
            _inventarioService = inventarioService;
        }

        private int GetCurrentUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(idStr, out int userId);
            return userId;
        }

        private string GetClientIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }

        // ================= ENTRADA DE MERCADERÍA =================
        [Authorize(Roles = "Administrador,Almacenero")]
        public async Task<IActionResult> Entrada()
        {
            await CargarCombosEntradaAsync();
            return View(new RegistrarEntradaDto());
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Almacenero")]
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

            int userId = GetCurrentUserId();
            var resultado = await _inventarioService.RegistrarEntradaAsync(dto, userId, GetClientIp());
            if (resultado.Exito)
            {
                TempData["SuccessMessage"] = resultado.Mensaje;
                return RedirectToAction(nameof(Movimientos));
            }

            ModelState.AddModelError("", resultado.Mensaje);
            await CargarCombosEntradaAsync();
            return View(dto);
        }

        // ================= SALIDA DE MERCADERÍA =================
        [Authorize(Roles = "Administrador,Almacenero,Vendedor")]
        public async Task<IActionResult> Salida()
        {
            await CargarCombosSalidaAsync();
            return View(new RegistrarSalidaDto());
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Almacenero,Vendedor")]
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

            int userId = GetCurrentUserId();
            var resultado = await _inventarioService.RegistrarSalidaAsync(dto, userId, GetClientIp());
            if (resultado.Exito)
            {
                TempData["SuccessMessage"] = resultado.Mensaje;
                return RedirectToAction(nameof(Movimientos));
            }

            ModelState.AddModelError("", resultado.Mensaje);
            await CargarCombosSalidaAsync();
            return View(dto);
        }

        // ================= AJUSTE DE INVENTARIO (CU-12) =================
        [Authorize(Roles = "Administrador,Almacenero")]
        [HttpGet]
        public async Task<IActionResult> Ajuste()
        {
            ViewBag.Productos = await _context.Productos
                .Where(p => p.Activo)
                .OrderBy(p => p.Descripcion)
                .ToListAsync();
            return View(new RegistrarAjusteDto());
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Almacenero")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ajuste(RegistrarAjusteDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Productos = await _context.Productos.Where(p => p.Activo).OrderBy(p => p.Descripcion).ToListAsync();
                return View(dto);
            }

            int userId = GetCurrentUserId();
            var resultado = await _inventarioService.RegistrarAjusteAsync(dto, userId, GetClientIp());
            if (resultado.Exito)
            {
                TempData["SuccessMessage"] = resultado.Mensaje;
                return RedirectToAction(nameof(Movimientos));
            }

            ModelState.AddModelError("", resultado.Mensaje);
            ViewBag.Productos = await _context.Productos.Where(p => p.Activo).OrderBy(p => p.Descripcion).ToListAsync();
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
                .Include(m => m.Usuario)
                .Include(m => m.Proveedor)
                .Include(m => m.OrdenCompra)
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
            ViewBag.OrdenesCompra = new SelectList(await _context.OrdenesCompra.Where(o => o.Estado == "Pendiente" || o.Estado == "Aprobada").ToListAsync(), "Id", "NumeroOrden");
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
