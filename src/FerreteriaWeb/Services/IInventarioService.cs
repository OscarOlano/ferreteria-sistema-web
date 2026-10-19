using FerreteriaWeb.Models;

namespace FerreteriaWeb.Services
{
    public class DashboardViewModel
    {
        public decimal ValorTotalInventario { get; set; }
        public int TotalProductos { get; set; }
        public int ProductosStockBajo { get; set; }
        public int MovimientosHoy { get; set; }
        public List<Producto> UltimosProductos { get; set; } = new();
        public List<Producto> AlertasStock { get; set; } = new();
        public List<Movimiento> UltimosMovimientos { get; set; } = new();
        public Dictionary<string, int> ProductosPorCategoria { get; set; } = new();
    }

    public class EntradaItemDto
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class RegistrarEntradaDto
    {
        public int? ProveedorId { get; set; }
        public string DocumentoReferencia { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public string Usuario { get; set; } = "Administrador";
        public List<EntradaItemDto> Items { get; set; } = new();
    }

    public class SalidaItemDto
    {
        public int ProductoId { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class RegistrarSalidaDto
    {
        public string TipoSalida { get; set; } = "Venta / Despacho";
        public string DocumentoReferencia { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public string Usuario { get; set; } = "Almacenero";
        public List<SalidaItemDto> Items { get; set; } = new();
    }

    public interface IInventarioService
    {
        Task<DashboardViewModel> GetDashboardMetricsAsync();
        Task<(bool Exito, string Mensaje)> RegistrarEntradaAsync(RegistrarEntradaDto dto);
        Task<(bool Exito, string Mensaje)> RegistrarSalidaAsync(RegistrarSalidaDto dto);
        Task<List<Kardex>> GetKardexByProductoAsync(int productoId, DateTime? desde = null, DateTime? hasta = null);
        Task<List<Producto>> GetAlertasStockAsync();
    }
}
