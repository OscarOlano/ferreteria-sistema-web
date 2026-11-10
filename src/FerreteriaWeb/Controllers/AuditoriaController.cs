using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;

namespace FerreteriaWeb.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly FerreteriaDbContext _context;

        public AuditoriaController(FerreteriaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.Auditorias
                .Include(a => a.Usuario)
                .OrderByDescending(a => a.Fecha)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}

