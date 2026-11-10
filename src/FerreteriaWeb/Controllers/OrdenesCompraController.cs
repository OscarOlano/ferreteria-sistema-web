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
    public class OrdenesCompraController : Controller
    {
        private readonly IOrdenCompraService _ordenCompraService;
        private readonly FerreteriaDbContext _context;

        public OrdenesCompraController(IOrdenCompraService ordenCompraService, FerreteriaDbContext context)
        {
            _ordenCompraService = ordenCompraService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ordenes = await _ordenCompraService.GetAllAsync();
            return View(ordenes);
        }

        public async Task<IActionResult> Details(int id)
        {
            var orden = await _ordenCompraService.GetByIdAsync(id);
            if (orden == null) return NotFound();
            return View(orden);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Proveedores = new SelectList(await _context.Proveedores.Where(p => p.Activo).ToListAsync(), "Id", "RazonSocial");
            ViewBag.Productos = await _context.Productos.Where(p => p.Activo).OrderBy(p => p.Descripcion).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegistrarOrdenCompraDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int usuarioId);

            if (usuarioId == 0)
                return Unauthorized();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var resultado = await _ordenCompraService.CrearOrdenAsync(dto, usuarioId, ip);

            if (resultado.Exito)
            {
                TempData["SuccessMessage"] = resultado.Mensaje;
                return RedirectToAction(nameof(Details), new { id = resultado.Id });
            }

            TempData["ErrorMessage"] = resultado.Mensaje;
            ViewBag.Proveedores = new SelectList(await _context.Proveedores.Where(p => p.Activo).ToListAsync(), "Id", "RazonSocial", dto.ProveedorId);
            ViewBag.Productos = await _context.Productos.Where(p => p.Activo).OrderBy(p => p.Descripcion).ToListAsync();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, string nuevoEstado)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdStr, out int usuarioId);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var resultado = await _ordenCompraService.CambiarEstadoAsync(id, nuevoEstado, usuarioId, ip);

            if (resultado.Exito)
                TempData["SuccessMessage"] = resultado.Mensaje;
            else
                TempData["ErrorMessage"] = resultado.Mensaje;

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Imprimir(int id)
        {
            var orden = await _ordenCompraService.GetByIdAsync(id);
            if (orden == null) return NotFound();
            return View(orden);
        }
    }
}

