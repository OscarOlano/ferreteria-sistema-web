using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FerreteriaWeb.Services;

namespace FerreteriaWeb.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IInventarioService _inventarioService;

        public HomeController(IInventarioService inventarioService)
        {
            _inventarioService = inventarioService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _inventarioService.GetDashboardMetricsAsync();
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
