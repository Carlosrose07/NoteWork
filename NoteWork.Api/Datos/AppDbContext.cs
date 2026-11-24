using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NoteWork.Shared.Entidades;
using NoteWork.Shared.Entidades.Universal;

namespace NoteWork.Api.Datos;

/// <summary>
/// AppDbContext es el puente entre las clases C# y las tablas de PostgreSQL.
/// Vive solo en NoteWork.Api: así el celular nunca descarga EF Core ni Npgsql.
/// </summary>
public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // --- TABLAS EXISTENTES (NÚCLEO NOTEWORK 1.0) ---
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Nodo> Nodos => Set<Nodo>();
    public DbSet<Partida> Partidas => Set<Partida>();

    // --- TABLAS DEL MOTOR UNIVERSAL (NOTEWORK 2.0) ---
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<MaterialCatalogItem> MaterialCatalog => Set<MaterialCatalogItem>();
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();
    public DbSet<ProductCutRule> ProductCutRules => Set<ProductCutRule>();
    public DbSet<ProductHardwareRule> ProductHardwareRules => Set<ProductHardwareRule>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderItem> WorkOrderItems => Set<WorkOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MUY IMPORTANTE: esta línea debe quedar de PRIMERA para configurar Identity.
        base.OnModelCreating(modelBuilder);

        // --- BORRADO LÓGICO ---
        modelBuilder.Entity<Empresa>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Cliente>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Proyecto>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Nodo>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Partida>().HasQueryFilter(e => !e.Eliminado);

        modelBuilder.Entity<ProductCategory>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<MaterialCatalogItem>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<ProductModel>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<ProductCutRule>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<ProductHardwareRule>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<WorkOrder>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<WorkOrderItem>().HasQueryFilter(e => !e.Eliminado);

        // --- ÍNDICES EXISTENTES ---
        modelBuilder.Entity<Cliente>().HasIndex(e => new { e.EmpresaId, e.UsuarioId });
        modelBuilder.Entity<Proyecto>().HasIndex(e => new { e.EmpresaId, e.UsuarioId });
        modelBuilder.Entity<Nodo>().HasIndex(e => e.EmpresaId);
        modelBuilder.Entity<Partida>().HasIndex(e => e.EmpresaId);
        modelBuilder.Entity<Usuario>().HasIndex(u => u.EmpresaId);
        modelBuilder.Entity<Partida>().HasIndex(e => new { e.NodoId, e.Orden });

        // --- ÍNDICES DEL MOTOR UNIVERSAL ---
        modelBuilder.Entity<ProductCategory>().HasIndex(e => new { e.EmpresaId, e.SortOrder });
        modelBuilder.Entity<ProductCategory>().HasIndex(e => new { e.EmpresaId, e.Code });

        modelBuilder.Entity<MaterialCatalogItem>().HasIndex(e => new { e.EmpresaId, e.Code });
        modelBuilder.Entity<MaterialCatalogItem>().HasIndex(e => new { e.EmpresaId, e.MaterialType });

        modelBuilder.Entity<ProductModel>().HasIndex(e => new { e.EmpresaId, e.CategoryId, e.SortOrder });
        modelBuilder.Entity<ProductModel>().HasIndex(e => new { e.EmpresaId, e.Code });

        modelBuilder.Entity<ProductCutRule>().HasIndex(e => new { e.ProductModelId, e.SortOrder });
        modelBuilder.Entity<ProductCutRule>().HasIndex(e => e.EmpresaId);

        modelBuilder.Entity<ProductHardwareRule>().HasIndex(e => new { e.ProductModelId, e.SortOrder });
        modelBuilder.Entity<ProductHardwareRule>().HasIndex(e => e.EmpresaId);

        modelBuilder.Entity<WorkOrder>().HasIndex(e => new { e.EmpresaId, e.UsuarioId, e.FechaCreacion });
        modelBuilder.Entity<WorkOrder>().HasIndex(e => new { e.EmpresaId, e.OrderNumber });
        modelBuilder.Entity<WorkOrder>().HasIndex(e => e.ProjectId);

        modelBuilder.Entity<WorkOrderItem>().HasIndex(e => new { e.WorkOrderId, e.LineOrder });
        modelBuilder.Entity<WorkOrderItem>().HasIndex(e => e.AreaNodeId);
        modelBuilder.Entity<WorkOrderItem>().HasIndex(e => e.EmpresaId);

        // --- PRECISIÓN DEL DINERO Y CANTIDADES (EXISTENTES) ---
        modelBuilder.Entity<Proyecto>().Property(p => p.DescuentoValor).HasPrecision(18, 2);
        modelBuilder.Entity<Partida>().Property(p => p.TotalLinea).HasPrecision(18, 2);
        modelBuilder.Entity<Partida>().Property(p => p.Descuento).HasPrecision(18, 2);
        modelBuilder.Entity<Partida>().Property(p => p.CostoInterno).HasPrecision(18, 2);
        modelBuilder.Entity<Partida>().Property(p => p.Cantidad).HasPrecision(18, 3);

        // --- LÍMITES DE LONGITUD (EXISTENTES) ---
        modelBuilder.Entity<Cliente>(e =>
        {
            e.Property(c => c.Nombre).HasMaxLength(150);
            e.Property(c => c.Telefono).HasMaxLength(30);
            e.Property(c => c.Direccion).HasMaxLength(300);
            e.Property(c => c.Notas).HasMaxLength(1000);
        });

        modelBuilder.Entity<Proyecto>(e =>
        {
            e.Property(p => p.Nombre).HasMaxLength(150);
            e.Property(p => p.Notas).HasMaxLength(500);
        });

        modelBuilder.Entity<Nodo>(e =>
        {
            e.Property(n => n.Nombre).HasMaxLength(150);
        });

        modelBuilder.Entity<Partida>(e =>
        {
            e.Property(p => p.Descripcion).HasMaxLength(300);
            e.Property(p => p.Especificacion).HasMaxLength(500);
            e.Property(p => p.Material).HasMaxLength(150);
            e.Property(p => p.Marca).HasMaxLength(150);
            e.Property(p => p.NotaInterna).HasMaxLength(500);
        });

        modelBuilder.Entity<Empresa>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.Property(x => x.Subtitulo).HasMaxLength(200);
            e.Property(x => x.Rnc).HasMaxLength(30);
            e.Property(x => x.Telefono).HasMaxLength(30);
            e.Property(x => x.Direccion).HasMaxLength(300);
            e.Property(x => x.Moneda).HasMaxLength(10);
        });

        // --- CONFIGURACIÓN DE TABLAS DEL MOTOR UNIVERSAL ---
        modelBuilder.Entity<ProductCategory>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(30);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.DefaultUnit).HasMaxLength(10);
        });

        modelBuilder.Entity<MaterialCatalogItem>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.ColorOptions).HasMaxLength(300);
            e.Property(x => x.CommercialLengthInches).HasPrecision(10, 2);
            e.Property(x => x.CommercialWidthInches).HasPrecision(10, 2);
            e.Property(x => x.CommercialHeightInches).HasPrecision(10, 2);
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.Property(x => x.DefaultWastePercent).HasPrecision(6, 2);
        });

        modelBuilder.Entity<ProductModel>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.DefaultLaborValue).HasPrecision(18, 2);
            e.Property(x => x.DefaultMarginPercent).HasPrecision(6, 2);

            e.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductCutRule>(e =>
        {
            e.Property(x => x.PartName).HasMaxLength(120);
            e.Property(x => x.ConditionJson).HasMaxLength(1000);
            e.Property(x => x.Dim1CustomDivisor).HasPrecision(10, 4);
            e.Property(x => x.Dim2CustomDivisor).HasPrecision(10, 4);

            e.HasOne(x => x.ProductModel)
                .WithMany()
                .HasForeignKey(x => x.ProductModelId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductHardwareRule>(e =>
        {
            e.Property(x => x.ConditionJson).HasMaxLength(1000);
            e.Property(x => x.Factor).HasPrecision(12, 4);

            e.HasOne(x => x.ProductModel)
                .WithMany()
                .HasForeignKey(x => x.ProductModelId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.Property(x => x.OrderNumber).HasMaxLength(40);
            e.Property(x => x.CustomerName).HasMaxLength(150);
            e.Property(x => x.CustomerPhone).HasMaxLength(30);
            e.Property(x => x.CustomerAddress).HasMaxLength(300);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.TaxRatePercent).HasPrecision(6, 2);
            e.Property(x => x.DiscountPercent).HasPrecision(6, 2);
            e.Property(x => x.SubtotalMaterials).HasPrecision(18, 2);
            e.Property(x => x.SubtotalLabor).HasPrecision(18, 2);
            e.Property(x => x.TotalQuoteAmount).HasPrecision(18, 2);

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkOrderItem>(e =>
        {
            e.Property(x => x.MaterialColor).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.CustomMarginPercent).HasPrecision(6, 2);
            e.Property(x => x.CalculatedUnitPrice).HasPrecision(18, 2);
            e.Property(x => x.CalculatedLineTotal).HasPrecision(18, 2);

            e.HasOne(x => x.WorkOrder)
                .WithMany()
                .HasForeignKey(x => x.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AreaNode)
                .WithMany()
                .HasForeignKey(x => x.AreaNodeId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.ProductModel)
                .WithMany()
                .HasForeignKey(x => x.ProductModelId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        AplicarReglasAutomaticas();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicarReglasAutomaticas();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AplicarReglasAutomaticas()
    {
        var ahora = DateTime.UtcNow;

        foreach (var entrada in ChangeTracker.Entries<EntidadBase>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Entity.FechaCreacion = ahora;
                    entrada.Entity.FechaModificacion = ahora;
                    break;

                case EntityState.Modified:
                    entrada.Entity.FechaModificacion = ahora;
                    break;

                case EntityState.Deleted:
                    entrada.State = EntityState.Modified;
                    entrada.Entity.Eliminado = true;
                    entrada.Entity.FechaModificacion = ahora;
                    break;
            }
        }
    }
}