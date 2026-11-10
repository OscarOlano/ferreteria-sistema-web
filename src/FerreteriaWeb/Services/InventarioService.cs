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
                .Include(m => m.Usuario)
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
            var ordenesPendientes = await _context.OrdenesCompra.CountAsync(o => o.Estado == "Pendiente" || o.Estado == "Aprobada");

            return new DashboardViewModel
            {
                ValorTotalInventario = valorTotal,
                TotalProductos = productos.Count,
                ProductosStockBajo = alertas.Count,
                MovimientosHoy = movimientosHoy,
                TotalOrdenesPendientes = ordenesPendientes,
                UltimosProductos = productos.OrderByDescending(p => p.Id).Take(5).ToList(),
                AlertasStock = alertas,
                UltimosMovimientos = ultimosMovimientos,
                ProductosPorCategoria = categoriasCount
            };
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarEntradaAsync(RegistrarEntradaDto dto, int usuarioId, string ip = "127.0.0.1")
        {
            if (dto.Items == null || !dto.Items.Any())
                return (false, "Debe agregar al menos un producto a la entrada de mercadería.");

            var user = await _context.Usuarios.FindAsync(usuarioId);
            if (user == null)
                return (false, "Usuario no autorizado para registrar movimientos.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal montoTotal = 0;
                var movimiento = new Movimiento
                {
                    Tipo = TipoMovimiento.Entrada,
                    Fecha = DateTime.Now,
                    DocumentoReferencia = string.IsNullOrWhiteSpace(dto.DocumentoReferencia) ? "GR-INTERNA" : dto.DocumentoReferencia.Trim(),
                    ProveedorId = dto.ProveedorId,
                    OrdenCompraId = dto.OrdenCompraId,
                    Motivo = string.IsNullOrWhiteSpace(dto.Motivo) ? "Recepción de mercadería de almacén" : dto.Motivo.Trim(),
                    UsuarioId = user.Id
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

                    // 2. Recálculo atómico de Costo Promedio Ponderado (CPP)
                    var stockAnterior = producto.StockActual;
                    var costoAnterior = producto.CostoPromedio > 0 ? producto.CostoPromedio : producto.PrecioCompra;
                    var valorAnterior = stockAnterior * costoAnterior;
                    var nuevoStock = stockAnterior + item.Cantidad;
                    var nuevoValor = valorAnterior + subtotal;
                    var nuevoCostoPromedio = nuevoStock > 0 ? nuevoValor / nuevoStock : costoUnit;

                    producto.StockActual = nuevoStock;
                    producto.CostoPromedio = Math.Round(nuevoCostoPromedio, 4);
                    producto.PrecioCompra = costoUnit; // Actualiza último precio de compra

                    // 3. Registro en Kárdex permanente valorizado
                    var kardex = new Kardex
                    {
                        ProductoId = producto.Id,
                        Fecha = DateTime.Now,
                        Operacion = "Entrada / Recepción Almacén",
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

                // Si viene asociada a una orden de compra, actualizar estado a Recibida
                if (dto.OrdenCompraId.HasValue)
                {
                    var oc = await _context.OrdenesCompra.FindAsync(dto.OrdenCompraId.Value);
                    if (oc != null)
                    {
                        oc.Estado = "Recibida";
                    }
                }

                // Bitácora de Auditoría
                _context.Auditorias.Add(new Auditoria
                {
                    UsuarioId = user.Id,
                    UsuarioEmail = user.Email,
                    Accion = "ENTRADA_ALMACEN",
                    TablaAfectada = "Movimiento",
                    RegistroId = movimiento.Id,
                    Detalles = $"Entrada registrada con doc {movimiento.DocumentoReferencia} por S/ {montoTotal:N2}.",
                    DireccionIP = ip,
                    Fecha = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Entrada registrada correctamente. Operación #{movimiento.Id} generada.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error al registrar entrada: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarSalidaAsync(RegistrarSalidaDto dto, int usuarioId, string ip = "127.0.0.1")
        {
            if (dto.Items == null || !dto.Items.Any())
                return (false, "Debe agregar al menos un producto para registrar la salida.");

            var user = await _context.Usuarios.FindAsync(usuarioId);
            if (user == null)
                return (false, "Usuario no autorizado para registrar despachos.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validación estricta de stock disponible (Regla de concurrencia obligatoria)
                foreach (var item in dto.Items)
                {
                    var prod = await _context.Productos.FindAsync(item.ProductoId);
                    if (prod == null) return (false, $"El producto con ID {item.ProductoId} no existe.");
                    if (prod.StockActual < item.Cantidad)
                    {
                        return (false, $"Stock insuficiente para '{prod.Descripcion}'. Disponible: {prod.StockActual}, Solicitado: {item.Cantidad}.");
                    }
                }

                decimal montoTotal = 0;
                var movimiento = new Movimiento
                {
                    Tipo = TipoMovimiento.Salida,
                    Fecha = DateTime.Now,
                    DocumentoReferencia = string.IsNullOrWhiteSpace(dto.DocumentoReferencia) ? "VALE-SALIDA" : dto.DocumentoReferencia.Trim(),
                    Motivo = $"{dto.TipoSalida}: {dto.Motivo}".Trim(),
                    UsuarioId = user.Id
                };

                _context.Movimientos.Add(movimiento);
                await _context.SaveChangesAsync();

                foreach (var item in dto.Items)
                {
                    if (item.Cantidad <= 0) continue;

                    var producto = await _context.Productos.FindAsync(item.ProductoId);
                    if (producto == null) continue;

                    // Re-verificar concurrencia justo antes de debitar
                    if (producto.StockActual < item.Cantidad)
                    {
                        throw new InvalidOperationException($"Conflicto de concurrencia: El stock de '{producto.Descripcion}' fue modificado simultáneamente.");
                    }

                    var costoUnitarioVigente = producto.CostoPromedio > 0 ? producto.CostoPromedio : producto.PrecioCompra;
                    var subtotal = item.Cantidad * costoUnitarioVigente;
                    montoTotal += subtotal;

                    // 1. Detalle de movimiento
                    var detalle = new DetalleMovimiento
                    {
                        MovimientoId = movimiento.Id,
                        ProductoId = producto.Id,
                        Cantidad = item.Cantidad,
                        CostoUnitario = costoUnitarioVigente
                    };
                    _context.DetallesMovimiento.Add(detalle);

                    // 2. Disminución atómica de stock
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

                // Bitácora de Auditoría
                _context.Auditorias.Add(new Auditoria
                {
                    UsuarioId = user.Id,
                    UsuarioEmail = user.Email,
                    Accion = "SALIDA_DESPACHO",
                    TablaAfectada = "Movimiento",
                    RegistroId = movimiento.Id,
                    Detalles = $"Salida de mercadería registrada con doc {movimiento.DocumentoReferencia} por S/ {montoTotal:N2}.",
                    DireccionIP = ip,
                    Fecha = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Despacho / Salida registrado con éxito. Vale #{movimiento.Id}.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error al registrar salida: {ex.Message}");
            }
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarAjusteAsync(RegistrarAjusteDto dto, int usuarioId, string ip = "127.0.0.1")
        {
            if (dto.StockFisicoReal < 0)
                return (false, "El stock físico no puede ser negativo.");

            var user = await _context.Usuarios.FindAsync(usuarioId);
            if (user == null)
                return (false, "Usuario no autorizado.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.FindAsync(dto.ProductoId);
                if (producto == null)
                    return (false, "Producto no encontrado.");

                var stockAnterior = producto.StockActual;
                var diferencia = dto.StockFisicoReal - stockAnterior;

                if (diferencia == 0)
                    return (false, "El stock auditado coincide exactamente con el stock del sistema. No se generó ajuste.");

                var costoUnit = producto.CostoPromedio > 0 ? producto.CostoPromedio : producto.PrecioCompra;
                var impactoMonto = Math.Round(Math.Abs(diferencia) * costoUnit, 2);
                var docRef = $"AJUSTE-{DateTime.Now:yyyyMMddHHmm}";

                // Movimiento
                var movimiento = new Movimiento
                {
                    Tipo = TipoMovimiento.Ajuste,
                    Fecha = DateTime.Now,
                    DocumentoReferencia = docRef,
                    Motivo = $"Ajuste Conteo Físico: {dto.MotivoAjuste} (Dif: {(diferencia > 0 ? "+" : "")}{diferencia})",
                    UsuarioId = user.Id,
                    MontoTotal = impactoMonto
                };
                _context.Movimientos.Add(movimiento);
                await _context.SaveChangesAsync();

                _context.DetallesMovimiento.Add(new DetalleMovimiento
                {
                    MovimientoId = movimiento.Id,
                    ProductoId = producto.Id,
                    Cantidad = Math.Abs(diferencia),
                    CostoUnitario = costoUnit
                });

                producto.StockActual = dto.StockFisicoReal;
                var saldoTotal = Math.Round(dto.StockFisicoReal * costoUnit, 2);

                // Kardex
                var kardex = new Kardex
                {
                    ProductoId = producto.Id,
                    Fecha = DateTime.Now,
                    Operacion = diferencia > 0 ? "Ajuste Físico (+ Sobrante)" : "Ajuste Físico (- Merma/Faltante)",
                    DocumentoRef = docRef,
                    CantidadEntrada = diferencia > 0 ? diferencia : 0,
                    CostoEntrada = diferencia > 0 ? costoUnit : 0,
                    TotalEntrada = diferencia > 0 ? impactoMonto : 0,
                    CantidadSalida = diferencia < 0 ? Math.Abs(diferencia) : 0,
                    CostoSalida = diferencia < 0 ? costoUnit : 0,
                    TotalSalida = diferencia < 0 ? impactoMonto : 0,
                    SaldoCantidad = dto.StockFisicoReal,
                    CostoPromedio = producto.CostoPromedio,
                    SaldoTotal = saldoTotal
                };
                _context.Kardex.Add(kardex);

                // Bitácora de Auditoría
                _context.Auditorias.Add(new Auditoria
                {
                    UsuarioId = user.Id,
                    UsuarioEmail = user.Email,
                    Accion = "AJUSTE_INVENTARIO",
                    TablaAfectada = "Producto",
                    RegistroId = producto.Id,
                    Detalles = $"Ajuste físico en '{producto.Descripcion}'. Stock anterior: {stockAnterior}, Auditado: {dto.StockFisicoReal}, Motivo: {dto.MotivoAjuste}.",
                    DireccionIP = ip,
                    Fecha = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, $"Ajuste registrado exitosamente. Stock conciliado a {dto.StockFisicoReal} unidades.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error al registrar ajuste: {ex.Message}");
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

        public async Task<List<Movimiento>> GetMovimientosRecientesAsync(int top = 50)
        {
            return await _context.Movimientos
                .Include(m => m.Usuario)
                .Include(m => m.Proveedor)
                .Include(m => m.Detalles)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(m => m.Fecha)
                .Take(top)
                .ToListAsync();
        }
    }
}
