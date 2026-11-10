using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;
using FerreteriaWeb.Services;

namespace FerreteriaWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly FerreteriaDbContext _context;
        private readonly IPasswordHasher _hasher;

        public AccountController(FerreteriaDbContext context, IPasswordHasher hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());

            if (usuario == null || !usuario.Activo)
            {
                ModelState.AddModelError(string.Empty, "Credenciales inválidas o cuenta de usuario inactiva.");
                return View(model);
            }

            bool passValida = _hasher.VerifyPassword(model.Password, usuario.PasswordHash);
            if (!passValida)
            {
                ModelState.AddModelError(string.Empty, "Contraseña incorrecta.");
                return View(model);
            }

            // Construcción de Claims de Seguridad
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.NombreCompleto),
                new(ClaimTypes.Email, usuario.Email),
                new(ClaimTypes.Role, usuario.Rol?.Nombre ?? "Usuario")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Registro en bitácora de auditoría
            _context.Auditorias.Add(new Auditoria
            {
                UsuarioId = usuario.Id,
                UsuarioEmail = usuario.Email,
                Accion = "LOGIN",
                TablaAfectada = "Usuario",
                RegistroId = usuario.Id,
                Detalles = $"Inicio de sesión exitoso con rol {usuario.Rol?.Nombre}.",
                DireccionIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                Fecha = DateTime.Now
            });
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(idClaim, out int usuarioId);

            if (!string.IsNullOrEmpty(email))
            {
                _context.Auditorias.Add(new Auditoria
                {
                    UsuarioId = usuarioId > 0 ? usuarioId : null,
                    UsuarioEmail = email,
                    Accion = "LOGOUT",
                    TablaAfectada = "Usuario",
                    RegistroId = usuarioId,
                    Detalles = "Cierre de sesión seguro.",
                    DireccionIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                    Fecha = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

