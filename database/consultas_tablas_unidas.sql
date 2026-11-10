-- ====================================================================
-- CONSULTAS DE TODAS LAS TABLAS UNIDAS (FerreteriaAlmacenDB)
-- Presiona F5 (o el botón "Ejecutar") en SQL Server Management Studio
-- ====================================================================
USE FerreteriaAlmacenDB;
GO

-- 1. CATÁLOGO COMPLETO DE PRODUCTOS (Productos + Categoria + Marca + UnidadMedida + Ubicacion + Almacen)
SELECT * FROM dbo.vw_CatalogoProductosCompleto;

-- 2. MOVIMIENTOS DETALLADOS (Movimiento + DetalleMovimiento + Productos + Categoria + Usuario + Rol + Proveedor + OrdenCompra)
SELECT * FROM dbo.vw_MovimientosDetallados;

-- 3. ÓRDENES DE COMPRA DETALLADAS (OrdenCompra + DetalleOrdenCompra + Proveedor + Usuario + Productos)
SELECT * FROM dbo.vw_OrdenesCompraDetalladas;

-- 4. KÁRDEX VALORIZADO UNIDO CON PRODUCTOS Y CATEGORÍAS
SELECT 
    k.Id AS KardexId,
    k.Fecha,
    p.Codigo,
    p.Descripcion AS Producto,
    c.Nombre AS Categoria,
    u.Abreviatura AS Unidad,
    k.Operacion,
    k.DocumentoRef,
    k.CantidadEntrada,
    k.CostoEntrada,
    k.TotalEntrada,
    k.CantidadSalida,
    k.CostoSalida,
    k.TotalSalida,
    k.SaldoCantidad,
    k.CostoPromedio,
    k.SaldoTotal
FROM dbo.Kardex k
INNER JOIN dbo.Productos p ON k.ProductoId = p.Id
INNER JOIN dbo.Categoria c ON p.CategoriaId = c.Id
INNER JOIN dbo.UnidadMedida u ON p.UnidadMedidaId = u.Id
ORDER BY k.Fecha DESC, k.Id DESC;

-- 5. AUDITORÍA UNIDA CON USUARIOS Y ROLES
SELECT 
    a.Id AS AuditoriaId,
    a.Fecha,
    a.Accion,
    a.TablaAfectada,
    a.RegistroId,
    CONCAT(u.Nombres, ' ', u.Apellidos) AS Usuario,
    r.Nombre AS Rol,
    a.UsuarioEmail,
    a.Detalles,
    a.DireccionIP
FROM dbo.Auditoria a
LEFT JOIN dbo.Usuario u ON a.UsuarioId = u.Id
LEFT JOIN dbo.Rol r ON u.RolId = r.Id
ORDER BY a.Fecha DESC;

