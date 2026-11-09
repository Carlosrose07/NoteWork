using Microsoft.AspNetCore.Identity;                       // CAMBIO (4.3): para IdentityRole<Guid>
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;   // CAMBIO (4.3): para IdentityDbContext
using Microsoft.EntityFrameworkCore;
using NoteWork.Shared.Entidades;

namespace NoteWork.Api.Datos;

/// <summary>
/// AppDbContext es el "puente" entre nuestras clases C# y las tablas de PostgreSQL.
/// Vive solo en NoteWork.Api: así el celular nunca descarga EF Core ni Npgsql.
///
/// CAMBIO (4.3): ahora hereda de IdentityDbContext en vez de DbContext.
/// IdentityDbContext ya sabe crear las tablas de usuarios, roles y contraseñas
/// (AspNetUsers, AspNetRoles, etc.), así que no las escribimos a mano.
/// Los tres tipos entre < > significan:
///   Usuario            -> nuestra clase de usuario (la que creamos en 4.2)
///   IdentityRole<Guid> -> los roles (ej. "Admin", "Vendedor"), también con Id tipo Guid
///   Guid               -> el tipo del Id, igual que en todas nuestras entidades
/// </summary>
public class AppDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>
{
    // El constructor recibe la configuración (cadena de conexión, etc.)
    // desde Program.cs. No cambia.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Cada DbSet = una tabla en la base de datos.
    // (No agregamos DbSet de Usuario: IdentityDbContext ya trae "Users" y "Roles".)
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Nodo> Nodos => Set<Nodo>();
    public DbSet<Partida> Partidas => Set<Partida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MUY IMPORTANTE: esta línea debe quedar de PRIMERA. Es la que configura
        // las tablas de Identity. Si la quitas o la mueves al final, falla la migración.
        base.OnModelCreating(modelBuilder);

        // --- BORRADO LÓGICO ---
        // Este filtro hace que TODAS las consultas ignoren automáticamente
        // los registros con Eliminado = true. Así no hay que acordarse
        // de filtrarlos a mano en cada consulta.
        modelBuilder.Entity<Empresa>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Cliente>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Proyecto>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Nodo>().HasQueryFilter(e => !e.Eliminado);
        modelBuilder.Entity<Partida>().HasQueryFilter(e => !e.Eliminado);

        // --- ÍNDICES ---
        // Casi todas las consultas filtran por EmpresaId (cada empresa solo ve
        // lo suyo). El índice hace que esa búsqueda sea rápida aunque crezcan los datos.
        modelBuilder.Entity<Cliente>().HasIndex(e => e.EmpresaId);
        modelBuilder.Entity<Proyecto>().HasIndex(e => e.EmpresaId);
        modelBuilder.Entity<Nodo>().HasIndex(e => e.EmpresaId);
        modelBuilder.Entity<Partida>().HasIndex(e => e.EmpresaId);

        // CAMBIO (4.3): mismo criterio para los usuarios, porque más adelante
        // listaremos "los usuarios de mi empresa".
        modelBuilder.Entity<Usuario>().HasIndex(u => u.EmpresaId);

        // Para listar las partidas de un nodo en su orden.
        modelBuilder.Entity<Partida>().HasIndex(e => new { e.NodoId, e.Orden });

        // --- PRECISIÓN DEL DINERO ---
        // numeric(18,2) en PostgreSQL: hasta 16 dígitos enteros y 2 decimales.
        // Nunca usamos double/float para dinero porque pierden exactitud.
        modelBuilder.Entity<Partida>().Property(p => p.TotalLinea).HasPrecision(18, 2);
        modelBuilder.Entity<Partida>().Property(p => p.Descuento).HasPrecision(18, 2);
        modelBuilder.Entity<Partida>().Property(p => p.CostoInterno).HasPrecision(18, 2);

        // La cantidad no es dinero, pero es decimal (ej. 2.5 láminas).
        modelBuilder.Entity<Partida>().Property(p => p.Cantidad).HasPrecision(18, 3);

        // --- LÍMITES DE LONGITUD (CAMBIO A2, hallazgo 4.3) ---
        // Antes todas las columnas de texto eran "text" sin límite: alguien podía guardar
        // un texto de varios MB. Estos números DEBEN coincidir con los DTO de NoteWork.Shared
        // (si cambias uno, cambia el otro). HasMaxLength convierte "text" en "character varying(N)".
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
    }

    // --- GUARDADO: fechas automáticas y borrado lógico ---
    // Estos dos métodos se ejecutan cada vez que guardamos cambios.
    // No cambian. (Solo afectan a las entidades que heredan de EntidadBase;
    // los usuarios de Identity no se tocan.)
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
        // Siempre UTC: así no hay confusión de zonas horarias entre servidor y celular.
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
                    // Convertimos el DELETE real en un UPDATE que solo marca
                    // Eliminado = true. Así nunca se pierde información.
                    entrada.State = EntityState.Modified;
                    entrada.Entity.Eliminado = true;
                    entrada.Entity.FechaModificacion = ahora;
                    break;
            }
        }
    }
}