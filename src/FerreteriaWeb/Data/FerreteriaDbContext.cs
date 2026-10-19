using Microsoft.EntityFrameworkCore;
using FerreteriaWeb.Models;

namespace FerreteriaWeb.Data
{
    public class FerreteriaDbContext : DbContext
    {
        public FerreteriaDbContext(DbContextOptions<FerreteriaDbContext> options) : base(options)
        {
        }

        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Marca> Marcas => Set<Marca>();
        public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Almacen> Almacenes => Set<Almacen>();
        public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<Movimiento> Movimientos => Set<Movimiento>();
        public DbSet<DetalleMovimiento> DetallesMovimiento => Set<DetalleMovimiento>();
        public DbSet<Kardex> Kardex => Set<Kardex>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índices únicos
            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.Codigo)
                .IsUnique();

            modelBuilder.Entity<Proveedor>()
                .HasIndex(p => p.Ruc)
                .IsUnique();

            // Relaciones con eliminación restringida
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
        }
    }
}
