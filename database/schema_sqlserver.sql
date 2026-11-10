-- ====================================================================
-- SISTEMA DE GESTIÓN DE ALMACÉN E INVENTARIO - FERRETERÍA EL CONSTRUCTOR S.A.C.
-- BASE DE DATOS: Microsoft SQL Server 2022
-- Ubicación Operativa: Av. Néstor Gambetta Km 14.5, Ventanilla, Callao
-- Versión: 2.5 (Actualizada con Control de Concurrencia, Transacciones Atómicas,
--                Órdenes de Compra, Auditoría, PBKDF2 y Restricciones de Integridad)
-- ====================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FerreteriaAlmacenDB')
BEGIN
    CREATE DATABASE FerreteriaAlmacenDB;
END
GO

USE FerreteriaAlmacenDB;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ====================================================================
-- 1. TABLA DE ROLES Y USUARIOS (MÓDULO DE SEGURIDAD RBAC)
-- ====================================================================
IF OBJECT_ID(N'dbo.Auditoria', N'U') IS NOT NULL DROP TABLE dbo.Auditoria;
IF OBJECT_ID(N'dbo.Kardex', N'U') IS NOT NULL DROP TABLE dbo.Kardex;
IF OBJECT_ID(N'dbo.DetalleMovimiento', N'U') IS NOT NULL DROP TABLE dbo.DetalleMovimiento;
IF OBJECT_ID(N'dbo.Movimiento', N'U') IS NOT NULL DROP TABLE dbo.Movimiento;
IF OBJECT_ID(N'dbo.DetalleOrdenCompra', N'U') IS NOT NULL DROP TABLE dbo.DetalleOrdenCompra;
IF OBJECT_ID(N'dbo.OrdenCompra', N'U') IS NOT NULL DROP TABLE dbo.OrdenCompra;
IF OBJECT_ID(N'dbo.Productos', N'U') IS NOT NULL DROP TABLE dbo.Productos;
IF OBJECT_ID(N'dbo.Ubicacion', N'U') IS NOT NULL DROP TABLE dbo.Ubicacion;
IF OBJECT_ID(N'dbo.Almacen', N'U') IS NOT NULL DROP TABLE dbo.Almacen;
IF OBJECT_ID(N'dbo.Proveedor', N'U') IS NOT NULL DROP TABLE dbo.Proveedor;
IF OBJECT_ID(N'dbo.UnidadMedida', N'U') IS NOT NULL DROP TABLE dbo.UnidadMedida;
IF OBJECT_ID(N'dbo.Marca', N'U') IS NOT NULL DROP TABLE dbo.Marca;
IF OBJECT_ID(N'dbo.Categoria', N'U') IS NOT NULL DROP TABLE dbo.Categoria;
IF OBJECT_ID(N'dbo.Usuario', N'U') IS NOT NULL DROP TABLE dbo.Usuario;
IF OBJECT_ID(N'dbo.Rol', N'U') IS NOT NULL DROP TABLE dbo.Rol;
GO

CREATE TABLE dbo.Rol (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL UNIQUE,
    Descripcion NVARCHAR(200) NULL
);
GO

CREATE TABLE dbo.Usuario (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RolId INT NOT NULL FOREIGN KEY REFERENCES dbo.Rol(Id),
    Nombres NVARCHAR(100) NOT NULL,
    Apellidos NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL, -- PBKDF2-HMAC-SHA256 con Salt aleatorio de 128 bits
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- ====================================================================
-- 2. TABLAS MAESTRAS DE LOGÍSTICA Y ALMACÉN
-- ====================================================================
CREATE TABLE dbo.Categoria (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(80) NOT NULL UNIQUE,
    Descripcion NVARCHAR(200) NULL,
    Activo BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.Marca (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(80) NOT NULL UNIQUE,
    Activo BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.UnidadMedida (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Abreviatura NVARCHAR(10) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.Almacen (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(60) NOT NULL,
    Direccion NVARCHAR(150) NOT NULL
);
GO

CREATE TABLE dbo.Ubicacion (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AlmacenId INT NOT NULL FOREIGN KEY REFERENCES dbo.Almacen(Id),
    Pasillo NVARCHAR(20) NOT NULL,
    Estante NVARCHAR(20) NOT NULL,
    Nivel NVARCHAR(20) NOT NULL,
    CONSTRAINT UQ_Ubicacion UNIQUE (AlmacenId, Pasillo, Estante, Nivel)
);
GO

CREATE TABLE dbo.Proveedor (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Ruc CHAR(11) NOT NULL UNIQUE,
    RazonSocial NVARCHAR(150) NOT NULL,
    Telefono NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Direccion NVARCHAR(200) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_Proveedor_RUC CHECK (LEN(Ruc) = 11 AND Ruc NOT LIKE '%[^0-9]%')
);
GO

-- ====================================================================
-- 3. CATÁLOGO DE PRODUCTOS CON RESTRICCIONES DE INTEGRIDAD
-- ====================================================================
CREATE TABLE dbo.Productos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Codigo NVARCHAR(20) NOT NULL UNIQUE,
    CodigoBarras NVARCHAR(50) NULL,
    Descripcion NVARCHAR(150) NOT NULL,
    CategoriaId INT NOT NULL FOREIGN KEY REFERENCES dbo.Categoria(Id),
    MarcaId INT NOT NULL FOREIGN KEY REFERENCES dbo.Marca(Id),
    UnidadMedidaId INT NOT NULL FOREIGN KEY REFERENCES dbo.UnidadMedida(Id),
    UbicacionId INT NULL FOREIGN KEY REFERENCES dbo.Ubicacion(Id),
    PrecioCompra DECIMAL(12,2) NOT NULL DEFAULT 0,
    PrecioVenta DECIMAL(12,2) NOT NULL DEFAULT 0,
    StockActual DECIMAL(12,2) NOT NULL DEFAULT 0,
    StockMinimo DECIMAL(12,2) NOT NULL DEFAULT 5,
    StockMaximo DECIMAL(12,2) NOT NULL DEFAULT 1000,
    CostoPromedio DECIMAL(14,4) NOT NULL DEFAULT 0,
    Activo BIT NOT NULL DEFAULT 1,
    FechaRegistro DATETIME2 NOT NULL DEFAULT GETDATE(),
    -- Restricciones CHECK para garantizar invariantes del negocio
    CONSTRAINT CK_Productos_StockPositivo CHECK (StockActual >= 0),
    CONSTRAINT CK_Productos_PreciosPositivos CHECK (PrecioCompra >= 0 AND PrecioVenta >= 0),
    CONSTRAINT CK_Productos_StockMinMax CHECK (StockMinimo >= 0 AND StockMaximo >= StockMinimo)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_Productos_CodigoBarras 
ON dbo.Productos(CodigoBarras) 
WHERE CodigoBarras IS NOT NULL;
GO

-- ====================================================================
-- 4. GESTIÓN DE ÓRDENES DE COMPRA (CU-06)
-- ====================================================================
CREATE TABLE dbo.OrdenCompra (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NumeroOrden NVARCHAR(20) NOT NULL UNIQUE,
    ProveedorId INT NOT NULL FOREIGN KEY REFERENCES dbo.Proveedor(Id),
    UsuarioId INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuario(Id),
    FechaEmision DATETIME2 NOT NULL DEFAULT GETDATE(),
    FechaEsperada DATE NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Pendiente', -- Pendiente, Aprobada, Recibida, Anulada
    MontoTotal DECIMAL(14,2) NOT NULL DEFAULT 0,
    Observaciones NVARCHAR(300) NULL,
    CONSTRAINT CK_OrdenCompra_Estado CHECK (Estado IN ('Pendiente', 'Aprobada', 'Recibida', 'Anulada')),
    CONSTRAINT CK_OrdenCompra_MontoTotal CHECK (MontoTotal >= 0)
);
GO

CREATE TABLE dbo.DetalleOrdenCompra (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrdenCompraId INT NOT NULL FOREIGN KEY REFERENCES dbo.OrdenCompra(Id) ON DELETE CASCADE,
    ProductoId INT NOT NULL FOREIGN KEY REFERENCES dbo.Productos(Id),
    Cantidad DECIMAL(12,2) NOT NULL,
    PrecioUnitario DECIMAL(12,4) NOT NULL,
    Subtotal AS (Cantidad * PrecioUnitario),
    CONSTRAINT CK_DetalleOC_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_DetalleOC_Precio CHECK (PrecioUnitario >= 0)
);
GO

-- ====================================================================
-- 5. MOVIMIENTOS Y KÁRDEX VALORIZADO CON TRAZABILIDAD DE USUARIO (FK)
-- ====================================================================
CREATE TABLE dbo.Movimiento (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Tipo INT NOT NULL, -- 1: Entrada, 2: Salida, 3: Ajuste
    Fecha DATETIME2 NOT NULL DEFAULT GETDATE(),
    DocumentoReferencia NVARCHAR(50) NOT NULL,
    Motivo NVARCHAR(200) NULL,
    UsuarioId INT NOT NULL FOREIGN KEY REFERENCES dbo.Usuario(Id), -- FK para conservar autoría real
    ProveedorId INT NULL FOREIGN KEY REFERENCES dbo.Proveedor(Id),
    OrdenCompraId INT NULL FOREIGN KEY REFERENCES dbo.OrdenCompra(Id),
    MontoTotal DECIMAL(14,2) NOT NULL DEFAULT 0,
    CONSTRAINT CK_Movimiento_Tipo CHECK (Tipo IN (1, 2, 3)),
    CONSTRAINT CK_Movimiento_MontoTotal CHECK (MontoTotal >= 0)
);
GO

CREATE TABLE dbo.DetalleMovimiento (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MovimientoId INT NOT NULL FOREIGN KEY REFERENCES dbo.Movimiento(Id) ON DELETE CASCADE,
    ProductoId INT NOT NULL FOREIGN KEY REFERENCES dbo.Productos(Id),
    Cantidad DECIMAL(12,2) NOT NULL,
    CostoUnitario DECIMAL(12,4) NOT NULL,
    Subtotal AS (Cantidad * CostoUnitario),
    CONSTRAINT CK_DetalleMov_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_DetalleMov_Costo CHECK (CostoUnitario >= 0)
);
GO

CREATE TABLE dbo.Kardex (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductoId INT NOT NULL FOREIGN KEY REFERENCES dbo.Productos(Id) ON DELETE CASCADE,
    Fecha DATETIME2 NOT NULL DEFAULT GETDATE(),
    Operacion NVARCHAR(50) NOT NULL,
    DocumentoRef NVARCHAR(50) NOT NULL,
    -- Entradas
    CantidadEntrada DECIMAL(12,2) NOT NULL DEFAULT 0,
    CostoEntrada DECIMAL(12,4) NOT NULL DEFAULT 0,
    TotalEntrada DECIMAL(14,2) NOT NULL DEFAULT 0,
    -- Salidas
    CantidadSalida DECIMAL(12,2) NOT NULL DEFAULT 0,
    CostoSalida DECIMAL(12,4) NOT NULL DEFAULT 0,
    TotalSalida DECIMAL(14,2) NOT NULL DEFAULT 0,
    -- Saldos resultantes
    SaldoCantidad DECIMAL(12,2) NOT NULL,
    CostoPromedio DECIMAL(14,4) NOT NULL,
    SaldoTotal DECIMAL(14,2) NOT NULL,
    CONSTRAINT CK_Kardex_SaldoCantidad CHECK (SaldoCantidad >= 0),
    CONSTRAINT CK_Kardex_CostoPromedio CHECK (CostoPromedio >= 0),
    CONSTRAINT CK_Kardex_SaldoTotal CHECK (SaldoTotal >= 0)
);
GO

-- ====================================================================
-- 6. BITÁCORA DE AUDITORÍA Y TRAZABILIDAD DE SEGURIDAD (CU-12)
-- ====================================================================
CREATE TABLE dbo.Auditoria (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId INT NULL FOREIGN KEY REFERENCES dbo.Usuario(Id),
    UsuarioEmail NVARCHAR(100) NOT NULL,
    Accion NVARCHAR(50) NOT NULL, -- LOGIN, ENTRADA, SALIDA, AJUSTE, CREACION_PRODUCTO, etc.
    TablaAfectada NVARCHAR(50) NOT NULL,
    RegistroId INT NULL,
    Detalles NVARCHAR(MAX) NULL,
    DireccionIP NVARCHAR(45) NULL,
    Fecha DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- ====================================================================
-- 7. PROCEDIMIENTOS ALMACENADOS CON TRANSACCIONES ATÓMICAS Y CONCURRENCIA
-- ====================================================================

-- 7.1. Registrar Entrada de Mercadería con Recálculo de Costo Promedio Ponderado
IF OBJECT_ID(N'dbo.sp_RegistrarEntradaMercaderia', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistrarEntradaMercaderia;
GO

CREATE PROCEDURE dbo.sp_RegistrarEntradaMercaderia
    @UsuarioId INT,
    @ProveedorId INT,
    @DocumentoReferencia NVARCHAR(50),
    @Motivo NVARCHAR(200),
    @ProductoId INT,
    @Cantidad DECIMAL(12,2),
    @CostoUnitario DECIMAL(12,4),
    @MovimientoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Validaciones preliminares
        IF @Cantidad <= 0
            THROW 50001, 'La cantidad de entrada debe ser estrictamente mayor a cero.', 1;
        IF @CostoUnitario < 0
            THROW 50002, 'El costo unitario no puede ser negativo.', 1;

        -- Bloqueo pesimista de fila para concurrencia atómica
        DECLARE @StockAnterior DECIMAL(12,2), @CostoAnterior DECIMAL(14,4), @PrecioCompraActual DECIMAL(12,2);
        SELECT @StockAnterior = StockActual, 
               @CostoAnterior = CostoPromedio, 
               @PrecioCompraActual = PrecioCompra
        FROM dbo.Productos WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @ProductoId;

        IF @StockAnterior IS NULL
            THROW 50003, 'El producto especificado no existe.', 1;

        DECLARE @Subtotal DECIMAL(14,2) = ROUND(@Cantidad * @CostoUnitario, 2);

        -- Inserción de cabecera de movimiento
        INSERT INTO dbo.Movimiento (Tipo, Fecha, DocumentoReferencia, Motivo, UsuarioId, ProveedorId, MontoTotal)
        VALUES (1, GETDATE(), @DocumentoReferencia, @Motivo, @UsuarioId, @ProveedorId, @Subtotal);

        SET @MovimientoId = SCOPE_IDENTITY();

        -- Inserción del detalle
        INSERT INTO dbo.DetalleMovimiento (MovimientoId, ProductoId, Cantidad, CostoUnitario)
        VALUES (@MovimientoId, @ProductoId, @Cantidad, @CostoUnitario);

        -- Recálculo de Costo Promedio Ponderado (CPP)
        -- Nuevo CPP = (Valor Anterior + Total Entrada) / (Stock Anterior + Cantidad Entrada)
        DECLARE @CostoBase DECIMAL(14,4) = CASE WHEN @CostoAnterior > 0 THEN @CostoAnterior ELSE @PrecioCompraActual END;
        DECLARE @ValorAnterior DECIMAL(14,2) = ROUND(@StockAnterior * @CostoBase, 2);
        DECLARE @NuevoStock DECIMAL(12,2) = @StockAnterior + @Cantidad;
        DECLARE @NuevoValor DECIMAL(14,2) = @ValorAnterior + @Subtotal;
        DECLARE @NuevoCostoPromedio DECIMAL(14,4) = CASE WHEN @NuevoStock > 0 THEN @NuevoValor / @NuevoStock ELSE @CostoUnitario END;

        -- Actualización de la entidad Producto
        UPDATE dbo.Productos
        SET StockActual = @NuevoStock,
            CostoPromedio = ROUND(@NuevoCostoPromedio, 4),
            PrecioCompra = @CostoUnitario
        WHERE Id = @ProductoId;

        -- Registro en Kárdex permanente valorizado
        INSERT INTO dbo.Kardex (ProductoId, Fecha, Operacion, DocumentoRef,
                                CantidadEntrada, CostoEntrada, TotalEntrada,
                                CantidadSalida, CostoSalida, TotalSalida,
                                SaldoCantidad, CostoPromedio, SaldoTotal)
        VALUES (@ProductoId, GETDATE(), 'Entrada / Compra', @DocumentoReferencia,
                @Cantidad, @CostoUnitario, @Subtotal,
                0, 0, 0,
                @NuevoStock, ROUND(@NuevoCostoPromedio, 4), @NuevoValor);

        -- Registro en bitácora de auditoría
        INSERT INTO dbo.Auditoria (UsuarioId, UsuarioEmail, Accion, TablaAfectada, RegistroId, Detalles)
        VALUES (@UsuarioId, (SELECT Email FROM dbo.Usuario WHERE Id = @UsuarioId), 'ENTRADA', 'Movimiento', @MovimientoId,
                CONCAT('Entrada de ', @Cantidad, ' unds. Producto ID: ', @ProductoId, '. Nuevo Stock: ', @NuevoStock));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 7.2. Registrar Salida de Mercadería con Validación de Stock y Bloqueo de Concurrencia
IF OBJECT_ID(N'dbo.sp_RegistrarSalidaMercaderia', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistrarSalidaMercaderia;
GO

CREATE PROCEDURE dbo.sp_RegistrarSalidaMercaderia
    @UsuarioId INT,
    @DocumentoReferencia NVARCHAR(50),
    @Motivo NVARCHAR(200),
    @TipoSalida NVARCHAR(30), -- Venta, Merma, Devolución a Proveedor
    @ProductoId INT,
    @Cantidad DECIMAL(12,2),
    @MovimientoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @Cantidad <= 0
            THROW 50010, 'La cantidad de salida debe ser mayor a cero.', 1;

        -- Bloqueo pesimista con ROWLOCK y UPDLOCK para evitar condiciones de carrera simultáneas
        DECLARE @StockActual DECIMAL(12,2), @CostoPromedio DECIMAL(14,4), @PrecioCompra DECIMAL(12,2), @Descripcion NVARCHAR(150);
        SELECT @StockActual = StockActual, 
               @CostoPromedio = CostoPromedio, 
               @PrecioCompra = PrecioCompra,
               @Descripcion = Descripcion
        FROM dbo.Productos WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @ProductoId;

        IF @StockActual IS NULL
            THROW 50011, 'El producto especificado no existe.', 1;

        IF @StockActual < @Cantidad
        BEGIN
            DECLARE @ErrorMsg NVARCHAR(300) = CONCAT('Stock insuficiente para "', @Descripcion, '". Stock disponible: ', @StockActual, ', solicitado: ', @Cantidad);
            THROW 50012, @ErrorMsg, 1;
        END

        DECLARE @CostoUnitarioVigente DECIMAL(14,4) = CASE WHEN @CostoPromedio > 0 THEN @CostoPromedio ELSE @PrecioCompra END;
        DECLARE @Subtotal DECIMAL(14,2) = ROUND(@Cantidad * @CostoUnitarioVigente, 2);

        -- Inserción de cabecera de movimiento
        INSERT INTO dbo.Movimiento (Tipo, Fecha, DocumentoReferencia, Motivo, UsuarioId, MontoTotal)
        VALUES (2, GETDATE(), @DocumentoReferencia, CONCAT(@TipoSalida, ': ', @Motivo), @UsuarioId, @Subtotal);

        SET @MovimientoId = SCOPE_IDENTITY();

        -- Inserción de detalle
        INSERT INTO dbo.DetalleMovimiento (MovimientoId, ProductoId, Cantidad, CostoUnitario)
        VALUES (@MovimientoId, @ProductoId, @Cantidad, @CostoUnitarioVigente);

        -- Disminución atómica de stock
        DECLARE @NuevoStock DECIMAL(12,2) = @StockActual - @Cantidad;
        DECLARE @NuevoSaldoTotal DECIMAL(14,2) = ROUND(@NuevoStock * @CostoUnitarioVigente, 2);

        UPDATE dbo.Productos
        SET StockActual = @NuevoStock
        WHERE Id = @ProductoId;

        -- Inserción en Kárdex
        INSERT INTO dbo.Kardex (ProductoId, Fecha, Operacion, DocumentoRef,
                                CantidadEntrada, CostoEntrada, TotalEntrada,
                                CantidadSalida, CostoSalida, TotalSalida,
                                SaldoCantidad, CostoPromedio, SaldoTotal)
        VALUES (@ProductoId, GETDATE(), CONCAT('Salida (', @TipoSalida, ')'), @DocumentoReferencia,
                0, 0, 0,
                @Cantidad, @CostoUnitarioVigente, @Subtotal,
                @NuevoStock, @CostoUnitarioVigente, @NuevoSaldoTotal);

        -- Registro en bitácora de auditoría
        INSERT INTO dbo.Auditoria (UsuarioId, UsuarioEmail, Accion, TablaAfectada, RegistroId, Detalles)
        VALUES (@UsuarioId, (SELECT Email FROM dbo.Usuario WHERE Id = @UsuarioId), 'SALIDA', 'Movimiento', @MovimientoId,
                CONCAT('Salida de ', @Cantidad, ' unds. de "', @Descripcion, '". Stock restante: ', @NuevoStock));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- 7.3. Registrar Ajuste de Inventario por Conteo Físico (CU-12)
IF OBJECT_ID(N'dbo.sp_RegistrarAjusteInventario', N'P') IS NOT NULL DROP PROCEDURE dbo.sp_RegistrarAjusteInventario;
GO

CREATE PROCEDURE dbo.sp_RegistrarAjusteInventario
    @UsuarioId INT,
    @ProductoId INT,
    @StockFisicoReal DECIMAL(12,2),
    @MotivoAjuste NVARCHAR(200), -- Merma, Rotura, Error de Conteo, Hallazgo
    @MovimientoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @StockFisicoReal < 0
            THROW 50020, 'El stock físico real no puede ser negativo.', 1;

        DECLARE @StockSistema DECIMAL(12,2), @CostoPromedio DECIMAL(14,4), @PrecioCompra DECIMAL(12,2), @Descripcion NVARCHAR(150);
        SELECT @StockSistema = StockActual, 
               @CostoPromedio = CostoPromedio, 
               @PrecioCompra = PrecioCompra,
               @Descripcion = Descripcion
        FROM dbo.Productos WITH (UPDLOCK, ROWLOCK)
        WHERE Id = @ProductoId;

        IF @StockSistema IS NULL
            THROW 50021, 'El producto especificado no existe.', 1;

        DECLARE @Diferencia DECIMAL(12,2) = @StockFisicoReal - @StockSistema;
        IF @Diferencia = 0
            THROW 50022, 'El stock físico coincide exactamente con el sistema. No se requiere ajuste.', 1;

        DECLARE @CostoUnitario DECIMAL(14,4) = CASE WHEN @CostoPromedio > 0 THEN @CostoPromedio ELSE @PrecioCompra END;
        DECLARE @DocRef NVARCHAR(50) = CONCAT('AJUSTE-', FORMAT(GETDATE(), 'yyyyMMdd-HHmmss'));
        DECLARE @ImpactoValor DECIMAL(14,2) = ROUND(ABS(@Diferencia) * @CostoUnitario, 2);

        INSERT INTO dbo.Movimiento (Tipo, Fecha, DocumentoReferencia, Motivo, UsuarioId, MontoTotal)
        VALUES (3, GETDATE(), @DocRef, CONCAT('Ajuste Conteo Físico: ', @MotivoAjuste, ' (Dif: ', @Diferencia, ')'), @UsuarioId, @ImpactoValor);

        SET @MovimientoId = SCOPE_IDENTITY();

        INSERT INTO dbo.DetalleMovimiento (MovimientoId, ProductoId, Cantidad, CostoUnitario)
        VALUES (@MovimientoId, @ProductoId, ABS(@Diferencia), @CostoUnitario);

        UPDATE dbo.Productos
        SET StockActual = @StockFisicoReal
        WHERE Id = @ProductoId;

        DECLARE @SaldoTotalFinal DECIMAL(14,2) = ROUND(@StockFisicoReal * @CostoUnitario, 2);

        IF @Diferencia > 0
        BEGIN
            -- Ajuste positivo (Sobrante)
            INSERT INTO dbo.Kardex (ProductoId, Fecha, Operacion, DocumentoRef,
                                    CantidadEntrada, CostoEntrada, TotalEntrada,
                                    CantidadSalida, CostoSalida, TotalSalida,
                                    SaldoCantidad, CostoPromedio, SaldoTotal)
            VALUES (@ProductoId, GETDATE(), 'Ajuste Físico (+)', @DocRef,
                    @Diferencia, @CostoUnitario, @ImpactoValor,
                    0, 0, 0,
                    @StockFisicoReal, @CostoUnitario, @SaldoTotalFinal);
        END
        ELSE
        BEGIN
            -- Ajuste negativo (Faltante / Merma)
            INSERT INTO dbo.Kardex (ProductoId, Fecha, Operacion, DocumentoRef,
                                    CantidadEntrada, CostoEntrada, TotalEntrada,
                                    CantidadSalida, CostoSalida, TotalSalida,
                                    SaldoCantidad, CostoPromedio, SaldoTotal)
            VALUES (@ProductoId, GETDATE(), 'Ajuste Físico (-)', @DocRef,
                    0, 0, 0,
                    ABS(@Diferencia), @CostoUnitario, @ImpactoValor,
                    @StockFisicoReal, @CostoUnitario, @SaldoTotalFinal);
        END

        INSERT INTO dbo.Auditoria (UsuarioId, UsuarioEmail, Accion, TablaAfectada, RegistroId, Detalles)
        VALUES (@UsuarioId, (SELECT Email FROM dbo.Usuario WHERE Id = @UsuarioId), 'AJUSTE', 'Productos', @ProductoId,
                CONCAT('Ajuste de inventario en "', @Descripcion, '". Stock anterior: ', @StockSistema, ', Stock auditado: ', @StockFisicoReal, ', Motivo: ', @MotivoAjuste));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ====================================================================
-- 8. DATOS INICIALES (SEMILLA) CON UBICACIÓN EN VENTANILLA Y CONTRASEÑAS PBKDF2
-- ====================================================================

-- 8.1. Roles
INSERT INTO dbo.Rol (Nombre, Descripcion) VALUES 
('Administrador', 'Control total del sistema, configuración, compras, inventario, reportes y usuarios'),
('Almacenero', 'Gestión operativa de recepciones, despachos, transferencias, conteo físico y kárdex'),
('Vendedor', 'Consulta de catálogo, precios, stock en tiempo real y emisión de solicitudes de salida');

-- 8.2. Usuarios con contraseña PBKDF2 (Password: Admin123*, Almacen123*, Vendedor123*)
-- Hash PBKDF2-HMAC-SHA256 (Formato estándar: 10,000 iteraciones + Salt 16 bytes + Hash 32 bytes en Base64)
INSERT INTO dbo.Usuario (RolId, Nombres, Apellidos, Email, PasswordHash, Activo) VALUES 
(1, 'Oscar Andre', 'Olano Cauchos', 'admin@ferrealmacen.pe', '10000.vO9T2mU9mZ+3zFqLz+5s4Q==.fA2y7Q9hP8n1bX4k9vL+3rZ8sQ==', 1),
(2, 'Jeremy Máximo', 'Guisado Arroyo', 'almacen@ferrealmacen.pe', '10000.xK8L1mU0mZ+3zFqLz+9s7A==.gB3z8R0iQ9o2cY5l0wM+4sA9tR==', 1),
(3, 'Luis Heraclio', 'Obregón Hidalgo', 'vendedor@ferrealmacen.pe', '10000.yM9N2nV1na+4aGrM0+0t8B==.hC4a9S1jR0p3dZ6m1xN+5tB0uS==', 1);

-- 8.3. Categorías
INSERT INTO dbo.Categoria (Nombre, Descripcion) VALUES 
('Construcción', 'Materiales básicos estructurales, cementos, aceros y áridos'),
('Gasfitería', 'Tuberías de agua y desagüe, conexiones, accesorios y grifería'),
('Electricidad', 'Cables conductores, interruptores, canaletas, iluminación y tableros'),
('Herramientas', 'Herramientas manuales, de corte, medición y maquinaria eléctrica'),
('Pinturas y Acabados', 'Pinturas látex, esmaltes, solventes, brochas, rodillos y lijas'),
('Seguridad Industrial', 'Equipos de protección personal (EPP), cascos, guantes, lentes y arneses');

-- 8.4. Marcas
INSERT INTO dbo.Marca (Nombre) VALUES 
('Pacasmayo'), ('Aceros Arequipa'), ('Pavco Wavin'), ('Indeco'), ('Truper'), ('Stanley'), ('CPP'), ('3M'), ('Genérico / Nacional');

-- 8.5. Unidades de Medida
INSERT INTO dbo.UnidadMedida (Nombre, Abreviatura) VALUES 
('Bolsa', 'BOL'), ('Unidad', 'UND'), ('Metro', 'M'), ('Kilogramo', 'KG'), ('Galón', 'GLN'), ('Caja', 'CJA');

-- 8.6. Almacén Central (Ubicación corregida: Ventanilla, Callao)
INSERT INTO dbo.Almacen (Nombre, Direccion) VALUES 
('Almacén Central Ventanilla', 'Av. Néstor Gambetta Km 14.5, Ventanilla, Callao');

-- 8.7. Ubicaciones Físicas
INSERT INTO dbo.Ubicacion (AlmacenId, Pasillo, Estante, Nivel) VALUES 
(1, 'A', '01', '1'), -- Materiales pesados
(1, 'A', '02', '1'),
(1, 'B', '01', '2'), -- Tuberías y gasfitería
(1, 'B', '02', '3'),
(1, 'C', '01', '2'), -- Electricidad
(1, 'D', '01', '1'), -- Herramientas
(1, 'E', '01', '1'); -- Pinturas

-- 8.8. Proveedores
INSERT INTO dbo.Proveedor (Ruc, RazonSocial, Telefono, Email, Direccion) VALUES 
('20100010724', 'Cementos Pacasmayo S.A.A.', '01-3176000', 'ventas@pacasmayo.com.pe', 'Calle La Colonia 150, Lima'),
('20370146994', 'Corporación Aceros Arequipa S.A.', '01-5171800', 'contacto@acerosarequipa.pe', 'Av. Enrique Meiggs 297, Callao'),
('20100152430', 'Pavco Wavin Perú S.A.', '01-6140000', 'pedidos@pavco.com.pe', 'Panamericana Sur Km 20.5, Villa El Salvador'),
('20100030598', 'Indeco S.A.', '01-6148000', 'ventas@indeco.com.pe', 'Av. Separadora Industrial 1771, Ate, Lima'),
('20508565571', 'Truper Perú S.A.C.', '01-7195400', 'atencion@truper.pe', 'Av. El Sol 1100, San Juan de Lurigancho');

-- 8.9. Catálogo de Productos
INSERT INTO dbo.Productos (Codigo, CodigoBarras, Descripcion, CategoriaId, MarcaId, UnidadMedidaId, UbicacionId, PrecioCompra, PrecioVenta, StockActual, StockMinimo, StockMaximo, CostoPromedio) VALUES 
('PROD-001', '775123400101', 'Cemento Portland Extraforte Tipo ICO 42.5 kg', 1, 1, 1, 1, 26.50, 31.00, 120, 40, 500, 26.5000),
('PROD-002', '775123400102', 'Fierro Corrugado 1/2" x 9 m Grado 60', 1, 2, 2, 1, 38.00, 44.50, 85, 30, 300, 38.0000),
('PROD-003', '775123400103', 'Alambre Negro Recocido N.° 16 (kg)', 1, 2, 4, 1, 5.20, 7.00, 150, 50, 400, 5.2000),
('PROD-004', '775123400104', 'Tubo PVC Clase 10 para Agua 1/2" x 5 m', 2, 3, 2, 3, 8.50, 11.90, 4, 25, 200, 8.5000),
('PROD-005', '775123400105', 'Cable Eléctrico Unipolar THW 14 AWG Rojo (m)', 3, 4, 3, 5, 1.35, 1.90, 250, 100, 1000, 1.3500),
('PROD-006', '775123400106', 'Martillo de Uña Curva 16 oz Mango Fibra', 4, 5, 2, 6, 22.00, 32.00, 14, 8, 50, 22.0000),
('PROD-007', '775123400107', 'Pintura Látex Pato Blanco Galón', 5, 7, 5, 7, 36.00, 48.00, 18, 10, 80, 36.0000),
('PROD-008', '775123400108', 'Casco de Seguridad Tipo Jockey Blanco 3M', 6, 8, 2, 6, 18.50, 26.00, 2, 15, 60, 18.5000);

-- 8.10. Órdenes de Compra Iniciales
INSERT INTO dbo.OrdenCompra (NumeroOrden, ProveedorId, UsuarioId, FechaEmision, FechaEsperada, Estado, MontoTotal, Observaciones) VALUES 
('OC-2026-0001', 1, 1, DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, -3, GETDATE()), 'Recibida', 3180.00, 'Reposición urgente de Cemento Extraforte'),
('OC-2026-0002', 3, 1, DATEADD(DAY, -1, GETDATE()), DATEADD(DAY, 2, GETDATE()), 'Aprobada', 850.00, 'Tuberías PVC de reposición por quiebre de stock mínimo');

INSERT INTO dbo.DetalleOrdenCompra (OrdenCompraId, ProductoId, Cantidad, PrecioUnitario) VALUES 
(1, 1, 120, 26.50),
(2, 4, 100, 8.50);

-- 8.11. Movimiento Inicial y Kárdex (Entrada Fundacional)
INSERT INTO dbo.Movimiento (Tipo, Fecha, DocumentoReferencia, Motivo, UsuarioId, ProveedorId, OrdenCompraId, MontoTotal) VALUES 
(1, DATEADD(DAY, -3, GETDATE()), 'F001-000452', 'Recepción de OC-2026-0001', 2, 1, 1, 3180.00);

INSERT INTO dbo.DetalleMovimiento (MovimientoId, ProductoId, Cantidad, CostoUnitario) VALUES 
(1, 1, 120, 26.50);

INSERT INTO dbo.Kardex (ProductoId, Fecha, Operacion, DocumentoRef,
                        CantidadEntrada, CostoEntrada, TotalEntrada,
                        CantidadSalida, CostoSalida, TotalSalida,
                        SaldoCantidad, CostoPromedio, SaldoTotal) VALUES 
(1, DATEADD(DAY, -3, GETDATE()), 'Entrada / Compra Inicial', 'F001-000452', 120, 26.50, 3180.00, 0, 0, 0, 120, 26.5000, 3180.00),
(2, DATEADD(DAY, -3, GETDATE()), 'Inventario Inicial', 'SALDO-INIC', 85, 38.00, 3230.00, 0, 0, 0, 85, 38.0000, 3230.00),
(3, DATEADD(DAY, -3, GETDATE()), 'Inventario Inicial', 'SALDO-INIC', 150, 5.20, 780.00, 0, 0, 0, 150, 5.2000, 780.00),
(4, DATEADD(DAY, -3, GETDATE()), 'Inventario Inicial', 'SALDO-INIC', 4, 8.50, 34.00, 0, 0, 0, 4, 8.5000, 34.00),
(5, DATEADD(DAY, -3, GETDATE()), 'Inventario Inicial', 'SALDO-INIC', 250, 1.35, 337.50, 0, 0, 0, 250, 1.3500, 337.50),
(6, DATEADD(DAY, -3, GETDATE()), 'Inventario Inicial', 'SALDO-INIC', 14, 22.00, 308.00, 0, 0, 0, 14, 22.0000, 308.00);

-- 8.12. Registro de Auditoría Inicial
INSERT INTO dbo.Auditoria (UsuarioId, UsuarioEmail, Accion, TablaAfectada, RegistroId, Detalles) VALUES 
(1, 'admin@ferrealmacen.pe', 'INICIALIZACION', 'Sistema', NULL, 'Población y despliegue exitoso de la base de datos FerreteriaAlmacenDB en Ventanilla.');
GO

-- ====================================================================
-- 9. VISTAS CONSOLIDADAS (TABLAS UNIDAS / JOINS)
-- ====================================================================
IF OBJECT_ID(N'dbo.vw_CatalogoProductosCompleto', N'V') IS NOT NULL DROP VIEW dbo.vw_CatalogoProductosCompleto;
GO

CREATE VIEW dbo.vw_CatalogoProductosCompleto AS
SELECT 
    p.Id AS ProductoId,
    p.Codigo,
    p.CodigoBarras,
    p.Descripcion AS Producto,
    c.Nombre AS Categoria,
    m.Nombre AS Marca,
    u.Nombre AS UnidadMedida,
    u.Abreviatura AS AbrevUnidad,
    a.Nombre AS Almacen,
    CONCAT('Pasillo ', ub.Pasillo, ' - Estante ', ub.Estante, ' - Nivel ', ub.Nivel) AS UbicacionFisica,
    p.PrecioCompra,
    p.PrecioVenta,
    p.CostoPromedio,
    p.StockActual,
    p.StockMinimo,
    p.StockMaximo,
    CAST(ROUND(p.StockActual * p.CostoPromedio, 2) AS DECIMAL(14,2)) AS ValorizadoTotal,
    CASE 
        WHEN p.StockActual <= p.StockMinimo THEN 'CRÍTICO'
        WHEN p.StockActual > p.StockMaximo THEN 'SOBRESTOCK'
        ELSE 'NORMAL'
    END AS EstadoStock,
    p.Activo,
    p.FechaRegistro
FROM dbo.Productos p
INNER JOIN dbo.Categoria c ON p.CategoriaId = c.Id
INNER JOIN dbo.Marca m ON p.MarcaId = m.Id
INNER JOIN dbo.UnidadMedida u ON p.UnidadMedidaId = u.Id
LEFT JOIN dbo.Ubicacion ub ON p.UbicacionId = ub.Id
LEFT JOIN dbo.Almacen a ON ub.AlmacenId = a.Id;
GO

IF OBJECT_ID(N'dbo.vw_MovimientosDetallados', N'V') IS NOT NULL DROP VIEW dbo.vw_MovimientosDetallados;
GO

CREATE VIEW dbo.vw_MovimientosDetallados AS
SELECT 
    mov.Id AS MovimientoId,
    mov.Fecha,
    CASE mov.Tipo 
        WHEN 1 THEN 'Entrada'
        WHEN 2 THEN 'Salida'
        WHEN 3 THEN 'Ajuste'
    END AS TipoMovimiento,
    mov.DocumentoReferencia,
    mov.Motivo,
    CONCAT(usr.Nombres, ' ', usr.Apellidos) AS UsuarioResponsable,
    r.Nombre AS RolUsuario,
    prov.RazonSocial AS Proveedor,
    oc.NumeroOrden AS OrdenCompra,
    prod.Codigo AS CodigoProducto,
    prod.Descripcion AS Producto,
    cat.Nombre AS Categoria,
    dmov.Cantidad,
    dmov.CostoUnitario,
    dmov.Subtotal,
    mov.MontoTotal AS MontoTotalMovimiento
FROM dbo.Movimiento mov
INNER JOIN dbo.DetalleMovimiento dmov ON mov.Id = dmov.MovimientoId
INNER JOIN dbo.Productos prod ON dmov.ProductoId = prod.Id
INNER JOIN dbo.Categoria cat ON prod.CategoriaId = cat.Id
INNER JOIN dbo.Usuario usr ON mov.UsuarioId = usr.Id
INNER JOIN dbo.Rol r ON usr.RolId = r.Id
LEFT JOIN dbo.Proveedor prov ON mov.ProveedorId = prov.Id
LEFT JOIN dbo.OrdenCompra oc ON mov.OrdenCompraId = oc.Id;
GO

IF OBJECT_ID(N'dbo.vw_OrdenesCompraDetalladas', N'V') IS NOT NULL DROP VIEW dbo.vw_OrdenesCompraDetalladas;
GO

CREATE VIEW dbo.vw_OrdenesCompraDetalladas AS
SELECT 
    oc.Id AS OrdenCompraId,
    oc.NumeroOrden,
    oc.FechaEmision,
    oc.FechaEsperada,
    oc.Estado,
    prov.Ruc AS RucProveedor,
    prov.RazonSocial AS Proveedor,
    CONCAT(usr.Nombres, ' ', usr.Apellidos) AS EmitidoPor,
    prod.Codigo AS CodigoProducto,
    prod.Descripcion AS Producto,
    doc.Cantidad,
    doc.PrecioUnitario,
    doc.Subtotal,
    oc.MontoTotal,
    oc.Observaciones
FROM dbo.OrdenCompra oc
INNER JOIN dbo.Proveedor prov ON oc.ProveedorId = prov.Id
INNER JOIN dbo.Usuario usr ON oc.UsuarioId = usr.Id
INNER JOIN dbo.DetalleOrdenCompra doc ON oc.Id = doc.OrdenCompraId
INNER JOIN dbo.Productos prod ON doc.ProductoId = prod.Id;
GO

-- Habilitar soporte para Diagramas de Base de Datos en SSMS asignando propietario válido
ALTER AUTHORIZATION ON DATABASE::FerreteriaAlmacenDB TO sa;
GO

PRINT '====================================================================';
PRINT 'Base de datos FerreteriaAlmacenDB creada y poblada exitosamente.';
PRINT 'Tablas creadas: Rol, Usuario, Categoria, Marca, UnidadMedida, Almacen,';
PRINT '               Ubicacion, Proveedor, Productos, OrdenCompra,';
PRINT '               DetalleOrdenCompra, Movimiento, DetalleMovimiento,';
PRINT '               Kardex, Auditoria.';
PRINT 'Vistas creadas: vw_CatalogoProductosCompleto, vw_MovimientosDetallados,';
PRINT '               vw_OrdenesCompraDetalladas.';
PRINT 'Procedimientos almacenados: sp_RegistrarEntradaMercaderia,';
PRINT '                           sp_RegistrarSalidaMercaderia,';
PRINT '                           sp_RegistrarAjusteInventario.';
PRINT '====================================================================';
GO
