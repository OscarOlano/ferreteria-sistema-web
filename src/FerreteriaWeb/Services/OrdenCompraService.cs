using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;

namespace FerreteriaWeb.Services
{
    public class OrdenCompraService : IOrdenCompraService
    {
        private readonly FerreteriaDbContext _context;

        public OrdenCompraService(FerreteriaDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrdenCompra>> GetAllAsync()
        {
            return await _context.OrdenesCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Usuario)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(o => o.FechaEmision)
                .ToListAsync();
        }

        public async Task<OrdenCompra?> GetByIdAsync(int id)
        {
            return await _context.OrdenesCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Usuario)
                .Include(o => o.Detalles)
                    .ThenInclude(d => d.Producto)
                        .ThenInclude(p => p!.UnidadMedida)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<(bool Exito, string Mensaje, int Id)> CrearOrdenAsync(RegistrarOrdenCompraDto dto, int usuarioId, string ip = "127.0.0.1")
        {
            if (dto.Items == null || !dto.Items.Any())
                return (false, "Debe agregar al menos un producto a la orden de compra.", 0);

            var user = await _context.Usuarios.FindAsync(usuarioId);
            if (user == null)
                return (false, "Usuario no autorizado.", 0);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var year = DateTime.Now.Year;
                var count = await _context.OrdenesCompra.CountAsync(o => o.FechaEmision.Year == year) + 1;
                var numeroOrden = $"OC-{year}-{count:D4}";

                decimal montoTotal = 0;
                var oc = new OrdenCompra
                {
                    NumeroOrden = numeroOrden,
                    ProveedorId = dto.ProveedorId,
                    UsuarioId = user.Id,
                    FechaEmision = DateTime.Now,
                    FechaEsperada = dto.FechaEsperada,
                    Estado = "Pendiente",
                    Observaciones = dto.Observaciones
                };

                _context.OrdenesCompra.Add(oc);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Items)
                {
                    if (item.Cantidad <= 0) continue;

                    var prod = await _context.Productos.FindAsync(item.ProductoId);
                    if (prod == null) continue;

                    var precio = item.CostoUnitario > 0 ? item.CostoUnitario : prod.PrecioCompra;
                    var subtotal = item.Cantidad * precio;
                    montoTotal += subtotal;

                    var det = new DetalleOrdenCompra
                    {
                        OrdenCompraId = oc.Id,
                        ProductoId = prod.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = precio
                    };
                    _context.DetallesOrdenCompra.Add(det);
                }

                oc.MontoTotal = montoTotal;

                // Auditoría
                _context.Auditorias.Add(new Auditoria
                {
                    UsuarioId = user.Id,
                    UsuarioEmail = user.Email,
                    Accion = "CREAR_ORDEN_COMPRA",
                    TablaAfectada = "OrdenCompra",
                    RegistroId = oc.Id,
                    Detalles = $"Generación de orden {numeroOrden} a proveedor por S/ {montoTotal:N2}.",
                    DireccionIP = ip,
                    Fecha = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Orden de compra {numeroOrden} generada con éxito.", oc.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error al crear orden de compra: {ex.Message}", 0);
            }
        }

        public async Task<(bool Exito, string Mensaje)> CambiarEstadoAsync(int id, string nuevoEstado, int usuarioId, string ip = "127.0.0.1")
        {
            var oc = await _context.OrdenesCompra.FindAsync(id);
            if (oc == null)
                return (false, "Orden de compra no encontrada.");

            var user = await _context.Usuarios.FindAsync(usuarioId);
            var estadoAnterior = oc.Estado;
            oc.Estado = nuevoEstado;

            _context.Auditorias.Add(new Auditoria
            {
                UsuarioId = user?.Id,
                UsuarioEmail = user?.Email ?? "system",
                Accion = "ESTADO_ORDEN_COMPRA",
                TablaAfectada = "OrdenCompra",
                RegistroId = oc.Id,
                Detalles = $"Cambio de estado de '{estadoAnterior}' a '{nuevoEstado}' para {oc.NumeroOrden}.",
                DireccionIP = ip,
                Fecha = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return (true, $"Orden {oc.NumeroOrden} actualizada a estado '{nuevoEstado}'.");
        }
    }
}
