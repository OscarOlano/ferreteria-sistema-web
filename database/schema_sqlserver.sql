-- ====================================================================
-- SISTEMA DE GESTIÓN DE ALMACÉN E INVENTARIO - FERRETERÍA EL CONSTRUCTOR
-- BASE DE DATOS: Microsoft SQL Server 2022
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

-- 1. Tabla de Roles y Usuarios
IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rol (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(50) NOT NULL
    );

    CREATE TABLE dbo.Usuario (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RolId INT NOT NULL FOREIGN KEY REFERENCES dbo.Rol(Id),
        Nombres NVARCHAR(100) NOT NULL,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(256) NOT NULL,
        Activo BIT NOT NULL DEFAULT 1
    );
END
GO

-- 2. Tablas Maestras
IF OBJECT_ID(N'dbo.Categoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categoria (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL,
        Descripcion NVARCHAR(200) NULL,
        Activo BIT NOT NULL DEFAULT 1
    );

    CREATE TABLE dbo.Marca (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL,
        Activo BIT NOT NULL DEFAULT 1
    );

    CREATE TABLE dbo.UnidadMedida (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(50) NOT NULL,
        Abreviatura NVARCHAR(10) NOT NULL
    );

    CREATE TABLE dbo.Almacen (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(60) NOT NULL,
        Direccion NVARCHAR(150) NULL
    );

    CREATE TABLE dbo.Ubicacion (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AlmacenId INT NOT NULL FOREIGN KEY REFERENCES dbo.Almacen(Id),
        Pasillo NVARCHAR(20) NOT NULL,
        Estante NVARCHAR(20) NOT NULL,
        Nivel NVARCHAR(20) NOT NULL
    );

    CREATE TABLE dbo.Proveedor (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Ruc CHAR(11) NOT NULL UNIQUE,
        RazonSocial NVARCHAR(150) NOT NULL,
        Telefono NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        Direccion NVARCHAR(200) NULL,
        Activo BIT NOT NULL DEFAULT 1
    );
END
GO

-- 3. Catálogo de Productos
IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
BEGIN
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
        FechaRegistro DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 4. Movimientos y Kárdex Valorizado (Costo Promedio Ponderado)
IF OBJECT_ID(N'dbo.Movimiento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Movimiento (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Tipo INT NOT NULL, -- 1: Entrada, 2: Salida, 3: Ajuste
        Fecha DATETIME2 NOT NULL DEFAULT GETDATE(),
        DocumentoReferencia NVARCHAR(50) NOT NULL,
        Motivo NVARCHAR(200) NULL,
        Usuario NVARCHAR(60) NOT NULL DEFAULT 'Administrador',
        ProveedorId INT NULL FOREIGN KEY REFERENCES dbo.Proveedor(Id),
        MontoTotal DECIMAL(14,2) NOT NULL DEFAULT 0
    );

    CREATE TABLE dbo.DetalleMovimiento (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MovimientoId INT NOT NULL FOREIGN KEY REFERENCES dbo.Movimiento(Id) ON DELETE CASCADE,
        ProductoId INT NOT NULL FOREIGN KEY REFERENCES dbo.Productos(Id),
        Cantidad DECIMAL(12,2) NOT NULL,
        CostoUnitario DECIMAL(12,4) NOT NULL
    );

    CREATE TABLE dbo.Kardex (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductoId INT NOT NULL FOREIGN KEY REFERENCES dbo.Productos(Id) ON DELETE CASCADE,
        Fecha DATETIME2 NOT NULL DEFAULT GETDATE(),
        Operacion NVARCHAR(30) NOT NULL,
        DocumentoRef NVARCHAR(50) NOT NULL,
        -- Entradas
        CantidadEntrada DECIMAL(12,2) NOT NULL DEFAULT 0,
        CostoEntrada DECIMAL(12,4) NOT NULL DEFAULT 0,
        TotalEntrada DECIMAL(14,2) NOT NULL DEFAULT 0,
        -- Salidas
        CantidadSalida DECIMAL(12,2) NOT NULL DEFAULT 0,
        CostoSalida DECIMAL(12,4) NOT NULL DEFAULT 0,
        TotalSalida DECIMAL(14,2) NOT NULL DEFAULT 0,
        -- Saldos
        SaldoCantidad DECIMAL(12,2) NOT NULL,
        CostoPromedio DECIMAL(12,4) NOT NULL,
        SaldoTotal DECIMAL(14,2) NOT NULL
    );
END
GO

-- ====================================================================
-- DATOS INICIALES (SEMILLA)
-- ====================================================================
INSERT INTO dbo.Rol (Nombre) VALUES ('Administrador'), ('Almacenero'), ('Vendedor');

INSERT INTO dbo.Categoria (Nombre, Descripcion) VALUES 
('Construcción', 'Materiales básicos estructurales y cementicios'),
('Gasfitería', 'Tuberías, conexiones y grifería'),
('Electricidad', 'Cables, interruptores, canaletas e iluminación'),
('Herramientas', 'Herramientas manuales y eléctricas'),
('Pinturas y Acabados', 'Pinturas, solventes, brochas y lijas');

INSERT INTO dbo.Marca (Nombre) VALUES 
('Pacasmayo'), ('Aceros Arequipa'), ('Pavco Wavin'), ('Indeco'), ('Truper'), ('Stanley'), ('CPP');

INSERT INTO dbo.UnidadMedida (Nombre, Abreviatura) VALUES 
('Bolsa', 'BOL'), ('Unidad', 'UND'), ('Metro', 'M'), ('Kilogramo', 'KG'), ('Galón', 'GLN');

INSERT INTO dbo.Almacen (Nombre, Direccion) VALUES 
('Almacén Central Principal', 'Av. San Martín 450, Cajamarca');

INSERT INTO dbo.Ubicacion (AlmacenId, Pasillo, Estante, Nivel) VALUES 
(1, 'A', '01', '1'),
(1, 'B', '01', '2'),
(1, 'C', '01', '2'),
(1, 'D', '01', '1');

INSERT INTO dbo.Proveedor (Ruc, RazonSocial, Telefono, Email, Direccion) VALUES 
('20100010724', 'Cementos Pacasmayo S.A.A.', '01-3176000', 'ventas@pacasmayo.com.pe', 'Calle La Colonia 150, Lima'),
('20370146994', 'Corporación Aceros Arequipa S.A.', '01-5171800', 'contacto@acerosarequipa.pe', 'Av. Enrique Meiggs 297, Callao'),
('20100152430', 'Pavco Wavin Perú S.A.', '01-6140000', 'pedidos@pavco.com.pe', 'Panamericana Sur Km 20.5, VES');

INSERT INTO dbo.Productos (Codigo, CodigoBarras, Descripcion, CategoriaId, MarcaId, UnidadMedidaId, UbicacionId, PrecioCompra, PrecioVenta, StockActual, StockMinimo, StockMaximo, CostoPromedio) VALUES 
('PROD-001', '775123400101', 'Cemento Portland Extraforte Tipo ICO 42.5 kg', 1, 1, 1, 1, 26.50, 31.00, 120, 40, 500, 26.5000),
('PROD-002', '775123400102', 'Fierro Corrugado 1/2" x 9 m Grado 60', 1, 2, 2, 1, 38.00, 44.50, 85, 30, 300, 38.0000),
('PROD-003', '775123400103', 'Alambre Negro Recocido N.° 16 (kg)', 1, 2, 4, 1, 5.20, 7.00, 150, 50, 400, 5.2000),
('PROD-004', '775123400104', 'Tubo PVC Clase 10 para Agua 1/2" x 5 m', 2, 3, 2, 2, 8.50, 11.90, 4, 25, 200, 8.5000),
('PROD-005', '775123400105', 'Cable Eléctrico Unipolar THW 14 AWG Rojo (m)', 3, 4, 3, 3, 1.35, 1.90, 250, 100, 1000, 1.3500),
('PROD-006', '775123400106', 'Martillo de Uña Curva 16 oz Mango Fibra', 4, 5, 2, 4, 22.00, 32.00, 14, 8, 50, 22.0000);

PRINT 'Base de datos FerreteriaAlmacenDB creada y poblada exitosamente.';
GO
