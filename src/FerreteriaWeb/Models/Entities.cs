using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FerreteriaWeb.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [StringLength(80)]
        [Display(Name = "Categoría")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    public class Marca
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
        [StringLength(80)]
        [Display(Name = "Marca")]
        public string Nombre { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    public class UnidadMedida
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la unidad es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Unidad")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La abreviatura es obligatoria.")]
        [StringLength(10)]
        [Display(Name = "Abrev.")]
        public string Abreviatura { get; set; } = string.Empty;

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    public class Proveedor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El RUC es obligatorio.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener exactamente 11 dígitos.")]
        [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "El RUC solo debe contener dígitos.")]
        [Display(Name = "RUC")]
        public string Ruc { get; set; } = string.Empty;

        [Required(ErrorMessage = "La razón social es obligatoria.")]
        [StringLength(150)]
        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Formato de correo no válido.")]
        [StringLength(100)]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }

        [StringLength(200)]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    public class Almacen
    {
        public int Id { get; set; }

        [Required]
        [StringLength(60)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Direccion { get; set; }

        public virtual ICollection<Ubicacion> Ubicaciones { get; set; } = new List<Ubicacion>();
    }

    public class Ubicacion
    {
        public int Id { get; set; }

        public int AlmacenId { get; set; }
        public virtual Almacen? Almacen { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Pasillo")]
        public string Pasillo { get; set; } = "A";

        [Required]
        [StringLength(20)]
        [Display(Name = "Estante")]
        public string Estante { get; set; } = "1";

        [Required]
        [StringLength(20)]
        [Display(Name = "Nivel")]
        public string Nivel { get; set; } = "1";

        [Display(Name = "Código Ubicación")]
        public string CodigoCompleto => $"{Pasillo}-{Estante}-{Nivel}";

        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }

    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(20)]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Código de Barras")]
        public string? CodigoBarras { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(150)]
        [Display(Name = "Descripción del Producto")]
        public string Descripcion { get; set; } = string.Empty;

        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }
        public virtual Categoria? Categoria { get; set; }

        [Display(Name = "Marca")]
        public int MarcaId { get; set; }
        public virtual Marca? Marca { get; set; }

        [Display(Name = "Unidad de Medida")]
        public int UnidadMedidaId { get; set; }
        public virtual UnidadMedida? UnidadMedida { get; set; }

        [Display(Name = "Ubicación")]
        public int? UbicacionId { get; set; }
        public virtual Ubicacion? Ubicacion { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 9999999.99, ErrorMessage = "El precio de compra debe ser positivo.")]
        [Display(Name = "Precio Compra (S/)")]
        public decimal PrecioCompra { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 9999999.99, ErrorMessage = "El precio de venta debe ser positivo.")]
        [Display(Name = "Precio Venta (S/)")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        [Display(Name = "Stock Actual")]
        public decimal StockActual { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 9999999.99)]
        [Display(Name = "Stock Mínimo")]
        public decimal StockMinimo { get; set; } = 5;

        [Column(TypeName = "decimal(12, 2)")]
        [Range(0, 9999999.99)]
        [Display(Name = "Stock Máximo")]
        public decimal StockMaximo { get; set; } = 1000;

        [Column(TypeName = "decimal(14, 4)")]
        [Display(Name = "Costo Promedio (S/)")]
        public decimal CostoPromedio { get; set; }

        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha Registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal ValorTotalInventario => StockActual * (CostoPromedio > 0 ? CostoPromedio : PrecioCompra);

        [NotMapped]
        public string EstadoStock
        {
            get
            {
                if (StockActual <= 0) return "Agotado";
                if (StockActual <= StockMinimo) return "Bajo";
                return "Normal";
            }
        }
    }

    public enum TipoMovimiento
    {
        Entrada = 1,
        Salida = 2,
        Ajuste = 3
    }

    public class Movimiento
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tipo de Movimiento")]
        public TipoMovimiento Tipo { get; set; }

        [Display(Name = "Fecha y Hora")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(50)]
        [Display(Name = "N.° Documento / Ref.")]
        public string DocumentoReferencia { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Motivo u Observación")]
        public string? Motivo { get; set; }

        [StringLength(60)]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; } = "Administrador";

        [Display(Name = "Proveedor")]
        public int? ProveedorId { get; set; }
        public virtual Proveedor? Proveedor { get; set; }

        [Column(TypeName = "decimal(14, 2)")]
        [Display(Name = "Monto Total (S/)")]
        public decimal MontoTotal { get; set; }

        public virtual ICollection<DetalleMovimiento> Detalles { get; set; } = new List<DetalleMovimiento>();
    }

    public class DetalleMovimiento
    {
        public int Id { get; set; }

        public int MovimientoId { get; set; }
        public virtual Movimiento? Movimiento { get; set; }

        public int ProductoId { get; set; }
        public virtual Producto? Producto { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        [Range(0.01, 9999999.99, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        [Display(Name = "Cantidad")]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        [Range(0, 9999999.99)]
        [Display(Name = "Costo Unitario (S/)")]
        public decimal CostoUnitario { get; set; }

        [Column(TypeName = "decimal(14, 2)")]
        [Display(Name = "Subtotal (S/)")]
        public decimal Subtotal => Cantidad * CostoUnitario;
    }

    public class Kardex
    {
        public int Id { get; set; }

        public int ProductoId { get; set; }
        public virtual Producto? Producto { get; set; }

        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(30)]
        [Display(Name = "Operación")]
        public string Operacion { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Comprobante / Ref.")]
        public string DocumentoRef { get; set; } = string.Empty;

        // Entradas
        [Column(TypeName = "decimal(12, 2)")]
        public decimal CantidadEntrada { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal CostoEntrada { get; set; }

        [Column(TypeName = "decimal(14, 2)")]
        public decimal TotalEntrada { get; set; }

        // Salidas
        [Column(TypeName = "decimal(12, 2)")]
        public decimal CantidadSalida { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        public decimal CostoSalida { get; set; }

        [Column(TypeName = "decimal(14, 2)")]
        public decimal TotalSalida { get; set; }

        // Saldos
        [Column(TypeName = "decimal(12, 2)")]
        [Display(Name = "Saldo Cantidad")]
        public decimal SaldoCantidad { get; set; }

        [Column(TypeName = "decimal(12, 4)")]
        [Display(Name = "Costo Promedio")]
        public decimal CostoPromedio { get; set; }

        [Column(TypeName = "decimal(14, 2)")]
        [Display(Name = "Saldo Total (S/)")]
        public decimal SaldoTotal { get; set; }
    }
}
