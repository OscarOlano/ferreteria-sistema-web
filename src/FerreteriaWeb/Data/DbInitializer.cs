using FerreteriaWeb.Models;
using FerreteriaWeb.Services;

namespace FerreteriaWeb.Data
{
    public static class DbInitializer
    {
        public static void Initialize(FerreteriaDbContext context, IPasswordHasher hasher)
        {
            // Asegura la creación del esquema
            context.Database.EnsureCreated();

            if (context.Usuarios.Any())
            {
                return; // Base de datos ya poblada
            }

            // 1. Roles del Sistema (RBAC)
            var rolAdmin = new Rol 
            { 
                Nombre = "Administrador", 
                Descripcion = "Control total del sistema, configuración, compras, inventario, reportes y usuarios" 
            };
            var rolAlmacen = new Rol 
            { 
                Nombre = "Almacenero", 
                Descripcion = "Gestión operativa de recepciones, despachos, transferencias, conteo físico y kárdex" 
            };
            var rolVendedor = new Rol 
            { 
                Nombre = "Vendedor", 
                Descripcion = "Consulta de catálogo, precios, stock en tiempo real y emisión de solicitudes de salida" 
            };

            context.Roles.AddRange(rolAdmin, rolAlmacen, rolVendedor);
            context.SaveChanges();

            // 2. Usuarios del Sistema con Criptografía PBKDF2
            var userAdmin = new Usuario
            {
                RolId = rolAdmin.Id,
                Nombres = "Oscar Andre",
                Apellidos = "Olano Cauchos",
                Email = "admin@ferrealmacen.pe",
                PasswordHash = hasher.HashPassword("Admin123*"),
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            var userAlmacen = new Usuario
            {
                RolId = rolAlmacen.Id,
                Nombres = "Jeremy Máximo",
                Apellidos = "Guisado Arroyo",
                Email = "almacen@ferrealmacen.pe",
                PasswordHash = hasher.HashPassword("Almacen123*"),
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            var userVendedor = new Usuario
            {
                RolId = rolVendedor.Id,
                Nombres = "Luis Heraclio",
                Apellidos = "Obregón Hidalgo",
                Email = "vendedor@ferrealmacen.pe",
                PasswordHash = hasher.HashPassword("Vendedor123*"),
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            context.Usuarios.AddRange(userAdmin, userAlmacen, userVendedor);
            context.SaveChanges();

            // 3. Almacén Central (Ubicación corregida: Ventanilla, Callao)
            var almacenPrincipal = new Almacen
            {
                Nombre = "Almacén Central Ventanilla",
                Direccion = "Av. Néstor Gambetta Km 14.5, Ventanilla, Callao"
            };
            context.Almacenes.Add(almacenPrincipal);
            context.SaveChanges();

            // 4. Ubicaciones Físicas
            var ubicaciones = new List<Ubicacion>
            {
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "A", Estante = "01", Nivel = "1" }, // Cemento y áridos
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "A", Estante = "02", Nivel = "1" }, // Fierro y acero
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "B", Estante = "01", Nivel = "2" }, // Tuberías y gasfitería
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "B", Estante = "02", Nivel = "3" }, // Conexiones PVC
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "C", Estante = "01", Nivel = "2" }, // Cables eléctricos
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "D", Estante = "01", Nivel = "1" }, // Herramientas manuales
                new() { AlmacenId = almacenPrincipal.Id, Pasillo = "E", Estante = "01", Nivel = "1" }, // Pinturas y solventes
            };
            context.Ubicaciones.AddRange(ubicaciones);
            context.SaveChanges();

            // 5. Categorías
            var catConstruccion = new Categoria { Nombre = "Construcción", Descripcion = "Materiales básicos estructurales y cementicios" };
            var catGasfiteria = new Categoria { Nombre = "Gasfitería", Descripcion = "Tuberías, conexiones y grifería" };
            var catElectricidad = new Categoria { Nombre = "Electricidad", Descripcion = "Cables, interruptores, canaletas e iluminación" };
            var catHerramientas = new Categoria { Nombre = "Herramientas", Descripcion = "Herramientas manuales y eléctricas" };
            var catPinturas = new Categoria { Nombre = "Pinturas y Acabados", Descripcion = "Pinturas, solventes, brochas y lijas" };
            var catSeguridad = new Categoria { Nombre = "Seguridad Industrial", Descripcion = "EPP, cascos, guantes y arneses" };

            context.Categorias.AddRange(catConstruccion, catGasfiteria, catElectricidad, catHerramientas, catPinturas, catSeguridad);
            context.SaveChanges();

            // 6. Marcas
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

            // 7. Unidades de Medida
            var undBolsa = new UnidadMedida { Nombre = "Bolsa", Abreviatura = "BOL" };
            var undUnidad = new UnidadMedida { Nombre = "Unidad", Abreviatura = "UND" };
            var undMetro = new UnidadMedida { Nombre = "Metro", Abreviatura = "M" };
            var undKilo = new UnidadMedida { Nombre = "Kilogramo", Abreviatura = "KG" };
            var undGalon = new UnidadMedida { Nombre = "Galón", Abreviatura = "GLN" };
            var undCaja = new UnidadMedida { Nombre = "Caja", Abreviatura = "CJA" };

            context.UnidadesMedida.AddRange(undBolsa, undUnidad, undMetro, undKilo, undGalon, undCaja);
            context.SaveChanges();

            // 8. Proveedores
            var prov1 = new Proveedor
            {
                Ruc = "20100010724",
                RazonSocial = "Cementos Pacasmayo S.A.A.",
                Telefono = "01-3176000",
                Email = "ventas@pacasmayo.com.pe",
                Direccion = "Calle La Colonia 150, Lima"
            };
            var prov2 = new Proveedor
            {
                Ruc = "20370146994",
                RazonSocial = "Corporación Aceros Arequipa S.A.",
                Telefono = "01-5171800",
                Email = "contacto@acerosarequipa.pe",
                Direccion = "Av. Enrique Meiggs 297, Callao"
            };
            var prov3 = new Proveedor
            {
                Ruc = "20100152430",
                RazonSocial = "Pavco Wavin Perú S.A.",
                Telefono = "01-6140000",
                Email = "pedidos@pavco.com.pe",
                Direccion = "Panamericana Sur Km 20.5, Villa El Salvador"
            };
            var prov4 = new Proveedor
            {
                Ruc = "20100030598",
                RazonSocial = "Indeco S.A.",
                Telefono = "01-6148000",
                Email = "ventas@indeco.com.pe",
                Direccion = "Av. Separadora Industrial 1771, Ate, Lima"
            };
            var prov5 = new Proveedor
            {
                Ruc = "20508565571",
                RazonSocial = "Truper Perú S.A.C.",
                Telefono = "01-7195400",
                Email = "atencion@truper.pe",
                Direccion = "Av. El Sol 1100, San Juan de Lurigancho"
            };

            context.Proveedores.AddRange(prov1, prov2, prov3, prov4, prov5);
            context.SaveChanges();

            // 9. Catálogo de Productos
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
                    CostoPromedio = 26.5000m
                },
                new()
                {
                    Codigo = "PROD-002",
                    CodigoBarras = "775123400102",
                    Descripcion = "Fierro Corrugado 1/2\" x 9 m Grado 60",
                    CategoriaId = catConstruccion.Id,
                    MarcaId = marcas[1].Id, // Aceros Arequipa
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[1].Id,
                    PrecioCompra = 38.00m,
                    PrecioVenta = 44.50m,
                    StockActual = 85,
                    StockMinimo = 30,
                    StockMaximo = 300,
                    CostoPromedio = 38.0000m
                },
                new()
                {
                    Codigo = "PROD-003",
                    CodigoBarras = "775123400103",
                    Descripcion = "Alambre Negro Recocido N.° 16 (kg)",
                    CategoriaId = catConstruccion.Id,
                    MarcaId = marcas[1].Id, // Aceros Arequipa
                    UnidadMedidaId = undKilo.Id,
                    UbicacionId = ubicaciones[1].Id,
                    PrecioCompra = 5.20m,
                    PrecioVenta = 7.00m,
                    StockActual = 150,
                    StockMinimo = 50,
                    StockMaximo = 400,
                    CostoPromedio = 5.2000m
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
                    StockActual = 4, // Stock crítico para alertas
                    StockMinimo = 25,
                    StockMaximo = 200,
                    CostoPromedio = 8.5000m
                },
                new()
                {
                    Codigo = "PROD-005",
                    CodigoBarras = "775123400105",
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
                    CostoPromedio = 1.3500m
                },
                new()
                {
                    Codigo = "PROD-006",
                    CodigoBarras = "775123400106",
                    Descripcion = "Martillo de Uña Curva 16 oz Mango Fibra",
                    CategoriaId = catHerramientas.Id,
                    MarcaId = marcas[4].Id, // Truper
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[5].Id,
                    PrecioCompra = 22.00m,
                    PrecioVenta = 32.00m,
                    StockActual = 14,
                    StockMinimo = 8,
                    StockMaximo = 50,
                    CostoPromedio = 22.0000m
                },
                new()
                {
                    Codigo = "PROD-007",
                    CodigoBarras = "775123400107",
                    Descripcion = "Pintura Látex Pato Blanco Galón",
                    CategoriaId = catPinturas.Id,
                    MarcaId = marcas[6].Id, // CPP
                    UnidadMedidaId = undGalon.Id,
                    UbicacionId = ubicaciones[6].Id,
                    PrecioCompra = 36.00m,
                    PrecioVenta = 48.00m,
                    StockActual = 18,
                    StockMinimo = 10,
                    StockMaximo = 80,
                    CostoPromedio = 36.0000m
                },
                new()
                {
                    Codigo = "PROD-008",
                    CodigoBarras = "775123400108",
                    Descripcion = "Casco de Seguridad Tipo Jockey Blanco 3M",
                    CategoriaId = catSeguridad.Id,
                    MarcaId = marcas[7].Id, // 3M
                    UnidadMedidaId = undUnidad.Id,
                    UbicacionId = ubicaciones[5].Id,
                    PrecioCompra = 18.50m,
                    PrecioVenta = 26.00m,
                    StockActual = 2, // Stock crítico
                    StockMinimo = 15,
                    StockMaximo = 60,
                    CostoPromedio = 18.5000m
                }
            };
            context.Productos.AddRange(productos);
            context.SaveChanges();

            // 10. Órdenes de Compra Iniciales (CU-06)
            var oc1 = new OrdenCompra
            {
                NumeroOrden = "OC-2026-0001",
                ProveedorId = prov1.Id,
                UsuarioId = userAdmin.Id,
                FechaEmision = DateTime.Now.AddDays(-5),
                FechaEsperada = DateTime.Now.AddDays(-3),
                Estado = "Recibida",
                MontoTotal = 3180.00m,
                Observaciones = "Reposición urgente de Cemento Portland Extraforte"
            };

            var oc2 = new OrdenCompra
            {
                NumeroOrden = "OC-2026-0002",
                ProveedorId = prov3.Id,
                UsuarioId = userAdmin.Id,
                FechaEmision = DateTime.Now.AddDays(-1),
                FechaEsperada = DateTime.Now.AddDays(2),
                Estado = "Aprobada",
                MontoTotal = 850.00m,
                Observaciones = "Tuberías PVC de reposición por quiebre de stock mínimo"
            };

            context.OrdenesCompra.AddRange(oc1, oc2);
            context.SaveChanges();

            context.DetallesOrdenCompra.AddRange(
                new DetalleOrdenCompra { OrdenCompraId = oc1.Id, ProductoId = productos[0].Id, Cantidad = 120, PrecioUnitario = 26.50m },
                new DetalleOrdenCompra { OrdenCompraId = oc2.Id, ProductoId = productos[3].Id, Cantidad = 100, PrecioUnitario = 8.50m }
            );
            context.SaveChanges();

            // 11. Movimiento Inicial y Kárdex (Entrada inicial trazable)
            var mov1 = new Movimiento
            {
                Tipo = TipoMovimiento.Entrada,
                Fecha = DateTime.Now.AddDays(-3),
                DocumentoReferencia = "F001-000452",
                Motivo = "Recepción de OC-2026-0001",
                UsuarioId = userAlmacen.Id,
                ProveedorId = prov1.Id,
                OrdenCompraId = oc1.Id,
                MontoTotal = 3180.00m
            };
            context.Movimientos.Add(mov1);
            context.SaveChanges();

            context.DetallesMovimiento.Add(new DetalleMovimiento
            {
                MovimientoId = mov1.Id,
                ProductoId = productos[0].Id,
                Cantidad = 120,
                CostoUnitario = 26.50m
            });
            context.SaveChanges();

            // Kárdex inicial
            var kardex1 = new Kardex
            {
                ProductoId = productos[0].Id,
                Fecha = DateTime.Now.AddDays(-3),
                Operacion = "Entrada / Compra Inicial",
                DocumentoRef = "F001-000452",
                CantidadEntrada = 120,
                CostoEntrada = 26.50m,
                TotalEntrada = 3180.00m,
                CantidadSalida = 0,
                CostoSalida = 0,
                TotalSalida = 0,
                SaldoCantidad = 120,
                CostoPromedio = 26.5000m,
                SaldoTotal = 3180.00m
            };

            var kardex2 = new Kardex
            {
                ProductoId = productos[1].Id,
                Fecha = DateTime.Now.AddDays(-3),
                Operacion = "Saldo Inicial de Inventario",
                DocumentoRef = "SALDO-INIC",
                CantidadEntrada = 85,
                CostoEntrada = 38.00m,
                TotalEntrada = 3230.00m,
                SaldoCantidad = 85,
                CostoPromedio = 38.0000m,
                SaldoTotal = 3230.00m
            };

            var kardex3 = new Kardex
            {
                ProductoId = productos[3].Id,
                Fecha = DateTime.Now.AddDays(-3),
                Operacion = "Saldo Inicial de Inventario",
                DocumentoRef = "SALDO-INIC",
                CantidadEntrada = 4,
                CostoEntrada = 8.50m,
                TotalEntrada = 34.00m,
                SaldoCantidad = 4,
                CostoPromedio = 8.5000m,
                SaldoTotal = 34.00m
            };

            context.Kardex.AddRange(kardex1, kardex2, kardex3);
            context.SaveChanges();

            // 12. Auditoría Inicial
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = userAdmin.Id,
                UsuarioEmail = userAdmin.Email,
                Accion = "INICIALIZACION",
                TablaAfectada = "Sistema",
                RegistroId = null,
                Detalles = "Inicialización exitosa del sistema FerreAlmacén con seguridad PBKDF2 y almacén en Ventanilla.",
                DireccionIP = "127.0.0.1",
                Fecha = DateTime.Now
            });
            context.SaveChanges();
        }
    }
}
