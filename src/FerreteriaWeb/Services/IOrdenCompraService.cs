using FerreteriaWeb.Models;

namespace FerreteriaWeb.Services
{
    public interface IOrdenCompraService
    {
        Task<List<OrdenCompra>> GetAllAsync();
        Task<OrdenCompra?> GetByIdAsync(int id);
        Task<(bool Exito, string Mensaje, int Id)> CrearOrdenAsync(RegistrarOrdenCompraDto dto, int usuarioId, string ip = "127.0.0.1");
        Task<(bool Exito, string Mensaje)> CambiarEstadoAsync(int id, string nuevoEstado, int usuarioId, string ip = "127.0.0.1");
    }
}

