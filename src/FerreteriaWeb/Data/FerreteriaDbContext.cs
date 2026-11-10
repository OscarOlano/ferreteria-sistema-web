using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Models;

namespace FerreteriaWeb.Data
{
    public class FerreteriaDbContext : DbContext
    {
        public FerreteriaDbContext(DbContextOptions<FerreteriaDbContext> options) : base(options)
        {
        }

        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Marca> Marcas => Set<Marca>();
        public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Almacen> Almacenes => Set<Almacen>();
        public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
        public DbSet<DetalleOrdenCompra> DetallesOrdenCompra => Set<DetalleOrdenCompra>();
        public DbSet<Movimiento> Movimientos => Set<Movimiento>();
        public DbSet<DetalleMovimiento> DetallesMovimiento => Set<DetalleMovimiento>();
        public DbSet<Kardex> Kardex => Set<Kardex>();
        public DbSet<Auditoria> Auditorias => Set<Auditoria>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índices únicos
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Codigo)
                .IsUnique();

            modelBuilder.Entity<Proveedor>()
                .HasIndex(p => p.Ruc)
                .IsUnique();

            modelBuilder.Entity<OrdenCompra>()
                .HasIndex(o => o.NumeroOrden)
                .IsUnique();

            // Relaciones de Usuario y Rol
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relaciones de Producto
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Marca)
                .WithMany(m => m.Productos)
                .HasForeignKey(p => p.MarcaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.UnidadMedida)
                .WithMany(u => u.Productos)
                .HasForeignKey(p => p.UnidadMedidaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relaciones de Orden de Compra
            modelBuilder.Entity<OrdenCompra>()
                .HasOne(o => o.Proveedor)
                .WithMany(p => p.OrdenesCompra)
                .HasForeignKey(o => o.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrdenCompra>()
                .HasOne(o => o.Usuario)
                .WithMany()
                .HasForeignKey(o => o.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleOrdenCompra>()
                .HasOne(d => d.OrdenCompra)
                .WithMany(o => o.Detalles)
                .HasForeignKey(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleOrdenCompra>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relaciones de Movimiento con Usuario (FK para autoría real)
            modelBuilder.Entity<Movimiento>()
                .HasOne(m => m.Usuario)
                .WithMany()
                .HasForeignKey(m => m.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Movimiento>()
                .HasOne(m => m.Proveedor)
                .WithMany()
                .HasForeignKey(m => m.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Movimiento>()
                .HasOne(m => m.OrdenCompra)
                .WithMany()
                .HasForeignKey(m => m.OrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DetalleMovimiento>()
                .HasOne(d => d.Movimiento)
                .WithMany(m => m.Detalles)
                .HasForeignKey(d => d.MovimientoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetalleMovimiento>()
                .HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Kardex>()
                .HasOne(k => k.Producto)
                .WithMany()
                .HasForeignKey(k => k.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Auditoria>()
                .HasOne(a => a.Usuario)
                .WithMany()
                .HasForeignKey(a => a.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
