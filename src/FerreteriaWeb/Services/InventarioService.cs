using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Data;
using FerreteriaWeb.Models;

namespace FerreteriaWeb.Services
{
    public class InventarioService : IInventarioService
    {
        private readonly FerreteriaDbContext _context;

        public InventarioService(FerreteriaDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardMetricsAsync()
        {
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.UnidadMedida)
                .Where(p => p.Activo)
                .ToListAsync();

            var hoy = DateTime.Today;
            var movimientosHoy = await _context.Movimientos
                .Where(m => m.Fecha >= hoy)
                .CountAsync();

            var ultimosMovimientos = await _context.Movimientos
                .Include(m => m.Proveedor)
                .Include(m => m.Detalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(m => m.Fecha)
                .Take(5)
                .ToListAsync();

            var categoriasCount = productos
                .GroupBy(p => p.Categoria?.Nombre ?? "Sin categoría")
                .ToDictionary(g => g.Key, g => g.Count());

            var alertas = productos
                .Where(p => p.StockActual <= p.StockMinimo)
                .OrderBy(p => p.StockActual)
                .ToList();

            var valorTotal = productos.Sum(p => p.ValorTotalInventario);

            return new DashboardViewModel
            {
                ValorTotalInventario = valorTotal,
                TotalProductos = productos.Count,
                ProductosStockBajo = alertas.Count,
                MovimientosHoy = movimientosHoy,
                UltimosProductos = productos.OrderByDescending(p => p.Id).Take(5).ToList(),
                AlertasStock = alertas,
                UltimosMovimientos = ultimosMovimientos,
                ProductosPorCategoria = categoriasCount
            };
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarEntradaAsync(RegistrarEntradaDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return (false, "Debe agregar al menos un producto a la entrada.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal montoTotal = 0;
                var movimiento = new Movimiento
                {
                    Tipo = TipoMovimiento.Entrada,
                    Fecha = DateTime.Now,
                    DocumentoReferencia = string.IsNullOrWhiteSpace(dto.DocumentoReferencia) ? "SIN-DOC" : dto.DocumentoReferencia.Trim(),
                    ProveedorId = dto.ProveedorId,
                    Motivo = dto.Motivo ?? "Entrada de mercadería",
                    Usuario = string.IsNullOrWhiteSpace(dto.Usuario) ? "Almacenero" : dto.Usuario
                };

                _context.Movimientos.Add(movimiento);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Items)
                {
                    if (item.Cantidad <= 0) continue;

                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null) continue;

                    var costoUnit = item.CostoUnitario > 0 ? item.CostoUnitario : producto.PrecioCompra;
                    var subtotal = item.Cantidad * costoUnit;
                    montoTotal += subtotal;

                    // 1. Detalle del movimiento
                    var detalle = new DetalleMovimiento
                    {
                        MovimientoId = movimiento.Id,
                        ProductoId = producto.Id,
                        Cantidad = item.Cantidad,
                        CostoUnitario = costoUnit
                    };
                    _context.DetallesMovimiento.Add(detalle);

                    // 2. Recálculo de Costo Promedio Ponderado
                    // Nuevo Costo Promedio = (Saldo Valor Actual + Total Entrada) / (Saldo Cantidad Actual + Cantidad Entrada)
                    var stockAnterior = producto.StockActual;
                    var valorAnterior = stockAnterior * (producto.CostoPromedio > 0 ? producto.CostoPromedio : producto.PrecioCompra);
                    var nuevoStock = stockAnterior + item.Cantidad;
                    var nuevoValor = valorAnterior + subtotal;
                    var nuevoCostoPromedio = nuevoStock > 0 ? nuevoValor / nuevoStock : costoUnit;

                    producto.StockActual = nuevoStock;
                    producto.CostoPromedio = Math.Round(nuevoCostoPromedio, 4);
                    producto.PrecioCompra = costoUnit; // Actualiza último precio de compra

                    // 3. Registro en Kárdex
                    var kardex = new Kardex
                    {
                        ProductoId = producto.Id,
                        Fecha = DateTime.Now,
                        Operacion = "Entrada / Compra",
                        DocumentoRef = movimiento.DocumentoReferencia,
                        CantidadEntrada = item.Cantidad,
                        CostoEntrada = costoUnit,
                        TotalEntrada = subtotal,
                        CantidadSalida = 0,
                        CostoSalida = 0,
                        TotalSalida = 0,
                        SaldoCantidad = nuevoStock,
                        CostoPromedio = producto.CostoPromedio,
                        SaldoTotal = Math.Round(nuevoValor, 2)
                    };
                    _context.Kardex.Add(kardex);
                }

                movimiento.MontoTotal = montoTotal;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Entrada registrada correctamente con N.° de Operación #{movimiento.Id}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error al registrar entrada: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarSalidaAsync(RegistrarSalidaDto dto)
        {
            if (dto.Items == null || !dto.Items.Any())
                return (false, "Debe agregar al menos un producto a la salida.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validación previa de stock
                foreach (var item in dto.Items)
                {
                    var prod = await _context.Productos.FindAsync(item.ProductoId);
                    if (prod == null) return (false, $"El producto con ID {item.ProductoId} no existe.");
                    if (prod.StockActual < item.Cantidad)
                    {
                        return (false, $"Stock insuficiente para '{prod.Descripcion}'. Stock disponible: {prod.StockActual}, solicitado: {item.Cantidad}.");
                    }
                }

                decimal montoTotal = 0;
                var movimiento = new Movimiento
                {
                    Tipo = TipoMovimiento.Salida,
                    Fecha = DateTime.Now,
                    DocumentoReferencia = string.IsNullOrWhiteSpace(dto.DocumentoReferencia) ? "SAL-GEN" : dto.DocumentoReferencia.Trim(),
                    Motivo = $"{dto.TipoSalida}: {dto.Motivo}".Trim(),
                    Usuario = string.IsNullOrWhiteSpace(dto.Usuario) ? "Almacenero" : dto.Usuario
                };

                _context.Movimientos.Add(movimiento);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Items)
                {
                    if (item.Cantidad <= 0) continue;

                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null) continue;
                    var costoUnitarioVigente = producto.CostoPromedio > 0 ? producto.CostoPromedio : producto.PrecioCompra;
                    var subtotal = item.Cantidad * costoUnitarioVigente;
                    montoTotal += subtotal;

                    // 1. Detalle del movimiento
                    var detalle = new DetalleMovimiento
                    {
                        MovimientoId = movimiento.Id,
                        ProductoId = producto.Id,
                        Cantidad = item.Cantidad,
                        CostoUnitario = costoUnitarioVigente
                    };
                    _context.DetallesMovimiento.Add(detalle);

                    // 2. Disminución de stock
                    var nuevoStock = producto.StockActual - item.Cantidad;
                    producto.StockActual = nuevoStock;
                    var nuevoSaldoValor = nuevoStock * costoUnitarioVigente;

                    // 3. Registro en Kárdex
                    var kardex = new Kardex
                    {
                        ProductoId = producto.Id,
                        Fecha = DateTime.Now,
                        Operacion = $"Salida ({dto.TipoSalida})",
                        DocumentoRef = movimiento.DocumentoReferencia,
                        CantidadEntrada = 0,
                        CostoEntrada = 0,
                        TotalEntrada = 0,
                        CantidadSalida = item.Cantidad,
                        CostoSalida = costoUnitarioVigente,
                        TotalSalida = subtotal,
                        SaldoCantidad = nuevoStock,
                        CostoPromedio = producto.CostoPromedio,
                        SaldoTotal = Math.Round(nuevoSaldoValor, 2)
                    };
                    _context.Kardex.Add(kardex);
                }

                movimiento.MontoTotal = montoTotal;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Salida registrada con éxito. N.° de comprobante #{movimiento.Id}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error al registrar salida: {ex.Message}");
            }
        }

        public async Task<List<Kardex>> GetKardexByProductoAsync(int productoId, DateTime? desde = null, DateTime? hasta = null)
        {
            var query = _context.Kardex
                .Include(k => k.Producto)
                .Where(k => k.ProductoId == productoId);

            if (desde.HasValue)
                query = query.Where(k => k.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(k => k.Fecha <= hasta.Value.Date.AddDays(1).AddTicks(-1));

            return await query.OrderBy(k => k.Fecha).ToListAsync();
        }

        public async Task<List<Producto>> GetAlertasStockAsync()
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.UnidadMedida)
                .Include(p => p.Ubicacion)
                .Where(p => p.Activo && p.StockActual <= p.StockMinimo)
                .OrderBy(p => p.StockActual)
                .ToListAsync();
        }
    }
}
