using FerreteriaWeb.Models;

namespace FerreteriaWeb.Services
{
    public interface IInventarioService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync();
        Task<(bool Exito, string Mensaje)> RegistrarEntradaAsync(RegistrarEntradaDto dto, int usuarioId, string ip = "127.0.0.1");
        Task<(bool Exito, string Mensaje)> RegistrarSalidaAsync(RegistrarSalidaDto dto, int usuarioId, string ip = "127.0.0.1");
        Task<(bool Exito, string Mensaje)> RegistrarAjusteAsync(RegistrarAjusteDto dto, int usuarioId, string ip = "127.0.0.1");
        Task<List<Kardex>> GetKardexByProductoAsync(int productoId, DateTime? desde = null, DateTime? hasta = null);
        Task<List<Producto>> GetAlertasStockAsync();
        Task<List<Movimiento>> GetMovimientosRecientesAsync(int top = 50);
    }
}
