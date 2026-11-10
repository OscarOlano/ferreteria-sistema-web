using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;
using FerreteriaWeb.Services;

namespace FerreteriaWeb.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly FerreteriaDbContext _context;
        private readonly IPasswordHasher _hasher;

        public UsuariosController(FerreteriaDbContext context, IPasswordHasher hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.RolId)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();

            return View(usuarios);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Nombre");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            {
                ModelState.AddModelError("PasswordHash", "La contraseña debe contener al menos 6 caracteres.");
            }

            if (await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == usuario.Email.ToLower()))
            {
                ModelState.AddModelError("Email", "Ya existe un usuario registrado con este correo.");
            }

            if (ModelState.IsValid)
            {
                usuario.PasswordHash = _hasher.HashPassword(password);
                usuario.FechaCreacion = DateTime.Now;
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Usuario {usuario.NombreCompleto} registrado exitosamente con cifrado PBKDF2.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Nombre", usuario.RolId);
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Activo = !usuario.Activo;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Estado de {usuario.NombreCompleto} actualizado a {(usuario.Activo ? "Activo" : "Inactivo")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}

