using FerreteriaWeb.Models;

namespace FerreteriaWeb.Data
{
    public static class DbInitializer
    {
        public static void Initialize(FerreteriaDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Productos.Any())
            {
                return; // Ya fue inicializada
            }

            // 1. Almacén y Ubicaciones
            var almacenPrincipal = new Almacen
            {
                Nombre = "Almacén Central Principal",
                Direccion = "Av. San Martín 450, Cajamarca"
            };
            context.Almacenes.Add(almacenPrincipal);
            context.SaveChanges();

            var ubicaciones = new List<Ubicacion>
            {
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "A", Estante = "01", Nivel = "1" }, // Materiales pesados
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "A", Estante = "02", Nivel = "1" },
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "B", Estante = "01", Nivel = "2" }, // Tuberías y gasfitería
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "B", Estante = "02", Nivel = "3" },
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "C", Estante = "01", Nivel = "2" }, // Electricidad
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "D", Estante = "01", Nivel = "1" }, // Herramientas
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "E", Estante = "01", Nivel = "1" }, // Pinturas
            };
            context.Ubicaciones.AddRange(ubicaciones);
            context.SaveChanges();

            // 2. Categorías
            var catConstruccion = new Categoria { Nombre = "Construcción", Descripcion = "Materiales básicos estructurales y cementicios" };
            var catGasfiteria = new Categoria { Nombre = "Gasfitería", Descripcion = "Tuberías, conexiones y grifería" };
            var catElectricidad = new Categoria { Nombre = "Electricidad", Descripcion = "Cables, interruptores, canaletas e iluminación" };
            var catHerramientas = new Categoria { Nombre = "Herramientas", Descripcion = "Herramientas manuales y eléctricas" };
            var catPinturas = new Categoria { Nombre = "Pinturas y Acabados", Descripcion = "Pinturas, solventes, brochas y lijas" };
            var catSeguridad = new Categoria { Nombre = "Seguridad Industrial", Descripcion = "EPP, cascos, guantes y lentes" };

            context.Categorias.AddRange(catConstruccion, catGasfiteria, catElectricidad, catHerramientas, catPinturas, catSeguridad);
            context.SaveChanges();

            // 3. Marcas
            var marcas = new List<Marca>
            {
                new() { Nombre = "Pacasmayo" },
                new() { Nombre = "Aceros Arequipa" },
                new() { Nombre = "Pavco Wavin" },
                new() { Nombre = "Indeco" },
                new() { Nombre = "Truper" },
                new() { Nombre = "Stanley" },
                new() { Nombre = "CPP" },
                new() { Nombre = "3M" },
                new() { Nombre = "Genérico / Nacional" }
            };
            context.Marcas.AddRange(marcas);
            context.SaveChanges();

            // 4. Unidades de Medida
            var undBolsa = new UnidadMedida { Nombre = "Bolsa", Abreviatura = "BOL" };
            var undUnidad = new UnidadMedida { Nombre = "Unidad", Abreviatura = "UND" };
            var undMetro = new UnidadMedida { Nombre = "Metro", Abreviatura = "M" };
            var undKilo = new UnidadMedida { Nombre = "Kilogramo", Abreviatura = "KG" };
            var undGalon = new UnidadMedida { Nombre = "Galón", Abreviatura = "GLN" };
            var undCaja = new UnidadMedida { Nombre = "Caja", Abreviatura = "CJA" };

            context.UnidadesMedida.AddRange(undBolsa, undUnidad, undMetro, undKilo, undGalon, undCaja);
            context.SaveChanges();

            // 5. Proveedores
            var proveedores = new List<Proveedor>
            {
                new()
                {
                    Ruc = "20100010724",
                    RazonSocial = "Cementos Pacasmayo S.A.A.",
                    Telefono = "01-3176000",
                    Email = "ventas@pacasmayo.com.pe",
                    Direccion = "Calle La Colonia 150, Lima"
                },
                new()
                {
                    Ruc = "20370146994",
                    RazonSocial = "Corporación Aceros Arequipa S.A.",
                    Telefono = "01-5171800",
                    Email = "contacto@acerosarequipa.pe",
                    Direccion = "Av. Enrique Meiggs 297, Callao"
                },
                new()
                {
                    Ruc = "20100152430",
                    RazonSocial = "Pavco Wavin Perú S.A.",
                    Telefono = "01-6140000",
                    Email = "pedidos@pavco.com.pe",
                    Direccion = "Autopista Panamericana Sur Km 20.5, Villa El Salvador"
                },
                new()
                {
                    Ruc = "20100030598",
                    RazonSocial = "Indeco S.A.",
                    Telefono = "01-6148000",
                    Email = "ventas@indeco.com.pe",
                    Direccion = "Av. Elmer Faucett 3350, Callao"
                },
                new()
                {
                    Ruc = "20508934512",
                    RazonSocial = "Distribuidora Ferretera Truper Perú S.A.C.",
                    Telefono = "01-7193200",
                    Email = "atencion@truper.pe",
                    Direccion = "Av. Argentina 2850, Lima"
                }
            };
            context.Proveedores.AddRange(proveedores);
            context.SaveChanges();

            // 6. Productos representativos
            var productos = new List<Producto>
            {
                new()
                {
                    Codigo = "PROD-001",
                    CodigoBarras = "775123400101",
                    Descripcion = "Cemento Portland Extraforte Tipo ICO 42.5 kg",
                    CategoriaId = catConstruccion.Id,
                    MarcaId = marcas[0].Id, // Pacasmayo
                    UnidadMedidaId = undBolsa.Id,
                    UbicacionId = ubicaciones[0].Id,
                    PrecioCompra = 26.50m,
                    PrecioVenta = 31.00m,
                    StockActual = 120,
                    StockMinimo = 40,
                    StockMaximo = 500,
                    CostoPromedio = 26.50m
                },
                new()
                {
                    Codigo = "PROD-002",
                    CodigoBarras = "775123400102",
                    Descripcion = "Fierro Corrugado 1/2\" x 9 m Grado 60",
                    CategoriaId = catConstruccion.Id,
                    MarcaId = marcas[1].Id, // Aceros Arequipa
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[0].Id,
                    PrecioCompra = 38.00m,
                    PrecioVenta = 44.50m,
                    StockActual = 85,
                    StockMinimo = 30,
                    StockMaximo = 300,
                    CostoPromedio = 38.00m
                },
                new()
                {
                    Codigo = "PROD-003",
                    CodigoBarras = "775123400103",
                    Descripcion = "Alambre Negro Recocido N.° 16 (kg)",
                    CategoriaId = catConstruccion.Id,
                    MarcaId = marcas[1].Id,
                    UnidadMedidaId = undKilo.Id,
                    UbicacionId = ubicaciones[1].Id,
                    PrecioCompra = 5.20m,
                    PrecioVenta = 7.00m,
                    StockActual = 150,
                    StockMinimo = 50,
                    StockMaximo = 400,
                    CostoPromedio = 5.20m
                },
                new()
                {
                    Codigo = "PROD-004",
                    CodigoBarras = "775123400104",
                    Descripcion = "Tubo PVC Clase 10 para Agua 1/2\" x 5 m",
                    CategoriaId = catGasfiteria.Id,
                    MarcaId = marcas[2].Id, // Pavco
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[2].Id,
                    PrecioCompra = 8.50m,
                    PrecioVenta = 11.90m,
                    StockActual = 4, // Alerta: bajo stock
                    StockMinimo = 25,
                    StockMaximo = 200,
                    CostoPromedio = 8.50m
                },
                new()
                {
                    Codigo = "PROD-005",
                    CodigoBarras = "775123400105",
                    Descripcion = "Tubo PVC Desagüe 4\" x 3 m Pesado",
                    CategoriaId = catGasfiteria.Id,
                    MarcaId = marcas[2].Id,
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[2].Id,
                    PrecioCompra = 24.00m,
                    PrecioVenta = 32.00m,
                    StockActual = 18,
                    StockMinimo = 15,
                    StockMaximo = 100,
                    CostoPromedio = 24.00m
                },
                new()
                {
                    Codigo = "PROD-006",
                    CodigoBarras = "775123400106",
                    Descripcion = "Cable Eléctrico Unipolar THW 14 AWG Rojo (m)",
                    CategoriaId = catElectricidad.Id,
                    MarcaId = marcas[3].Id, // Indeco
                    UnidadMedidaId = undMetro.Id,
                    UbicacionId = ubicaciones[4].Id,
                    PrecioCompra = 1.35m,
                    PrecioVenta = 1.90m,
                    StockActual = 250,
                    StockMinimo = 100,
                    StockMaximo = 1000,
                    CostoPromedio = 1.35m
                },
                new()
                {
                    Codigo = "PROD-007",
                    CodigoBarras = "775123400107",
                    Descripcion = "Interruptor Simple con Placa Blanco",
                    CategoriaId = catElectricidad.Id,
                    MarcaId = marcas[8].Id,
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[4].Id,
                    PrecioCompra = 4.80m,
                    PrecioVenta = 7.50m,
                    StockActual = 6, // Alerta: bajo stock
                    StockMinimo = 20,
                    StockMaximo = 150,
                    CostoPromedio = 4.80m
                },
                new()
                {
                    Codigo = "PROD-008",
                    CodigoBarras = "775123400108",
                    Descripcion = "Martillo de Uña Curva 16 oz Mango Fibra de Vidrio",
                    CategoriaId = catHerramientas.Id,
                    MarcaId = marcas[4].Id, // Truper
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[5].Id,
                    PrecioCompra = 22.00m,
                    PrecioVenta = 32.00m,
                    StockActual = 14,
                    StockMinimo = 8,
                    StockMaximo = 50,
                    CostoPromedio = 22.00m
                },
                new()
                {
                    Codigo = "PROD-009",
                    CodigoBarras = "775123400109",
                    Descripcion = "Huincha Métrica 5 m / 16 ft Cinta Ancha",
                    CategoriaId = catHerramientas.Id,
                    MarcaId = marcas[5].Id, // Stanley
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[5].Id,
                    PrecioCompra = 14.50m,
                    PrecioVenta = 21.00m,
                    StockActual = 22,
                    StockMinimo = 10,
                    StockMaximo = 60,
                    CostoPromedio = 14.50m
                },
                new()
                {
                    Codigo = "PROD-010",
                    CodigoBarras = "775123400110",
                    Descripcion = "Pintura Látex Pato Blanco Galón",
                    CategoriaId = catPinturas.Id,
                    MarcaId = marcas[6].Id, // CPP
                    UnidadMedidaId = undGalon.Id,
                    UbicacionId = ubicaciones[6].Id,
                    PrecioCompra = 36.00m,
                    PrecioVenta = 48.00m,
                    StockActual = 30,
                    StockMinimo = 12,
                    StockMaximo = 100,
                    CostoPromedio = 36.00m
                },
                new()
                {
                    Codigo = "PROD-011",
                    CodigoBarras = "775123400111",
                    Descripcion = "Brocha Cerda Rubia 2.5\"",
                    CategoriaId = catPinturas.Id,
                    MarcaId = marcas[4].Id,
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[6].Id,
                    PrecioCompra = 4.20m,
                    PrecioVenta = 6.50m,
                    StockActual = 3, // Alerta: bajo stock
                    StockMinimo = 15,
                    StockMaximo = 80,
                    CostoPromedio = 4.20m
                },
                new()
                {
                    Codigo = "PROD-012",
                    CodigoBarras = "775123400112",
                    Descripcion = "Casco de Seguridad Tipo Jockey con Tafilete Blanco",
                    CategoriaId = catSeguridad.Id,
                    MarcaId = marcas[7].Id, // 3M
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[5].Id,
                    PrecioCompra = 18.00m,
                    PrecioVenta = 26.00m,
                    StockActual = 25,
                    StockMinimo = 10,
                    StockMaximo = 70,
                    CostoPromedio = 18.00m
                }
            };
            context.Productos.AddRange(productos);
            context.SaveChanges();

            // 7. Movimientos y Kárdex iniciales
            var movInicial = new Movimiento
            {
                Tipo = TipoMovimiento.Entrada,
                Fecha = DateTime.Now.AddDays(-10),
                DocumentoReferencia = "FAC-E001-000452",
                ProveedorId = proveedores[0].Id,
                Motivo = "Carga inicial de inventario - Apertura de Almacén",
                Usuario = "Administrador",
                MontoTotal = 3180.00m
            };
            context.Movimientos.Add(movInicial);
            context.SaveChanges();

            // Kárdex de los productos
            foreach (var prod in productos)
            {
                context.Kardex.Add(new Kardex
                {
                    ProductoId = prod.Id,
                    Fecha = DateTime.Now.AddDays(-10),
                    Operacion = "Inventario Inicial",
                    DocumentoRef = "INV-001",
                    CantidadEntrada = prod.StockActual,
                    CostoEntrada = prod.CostoPromedio,
                    TotalEntrada = prod.StockActual * prod.CostoPromedio,
                    CantidadSalida = 0,
                    CostoSalida = 0,
                    TotalSalida = 0,
                    SaldoCantidad = prod.StockActual,
                    CostoPromedio = prod.CostoPromedio,
                    SaldoTotal = prod.StockActual * prod.CostoPromedio
                });
            }
            context.SaveChanges();
        }
    }
}
