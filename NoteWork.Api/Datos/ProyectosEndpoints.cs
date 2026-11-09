using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Entidades;
using NoteWork.Shared.Proyecto;
using System.Security.Claims;

namespace NoteWork.Api.Endpoints;

public static class ProyectosEndpoints
{
    public static void MapProyectosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/proyectos").RequireAuthorization();

        // 1. Obtener lista paginada
        grupo.MapGet("/", async (AppDbContext db, ClaimsPrincipal user, int pagina = 1, int tamano = 20) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var query = db.Proyectos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Where(p => p.EmpresaId == empresaId)
                .OrderByDescending(p => p.FechaCreacion);

            var total = await query.CountAsync();

            var proyectos = await query
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(p => new ProyectoDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    ClienteId = p.ClienteId,
                    ClienteNombre = p.Cliente != null ? p.Cliente.Nombre : null,
                    Tipo = p.Tipo,
                    Estado = p.Estado,
                    UnidadMedida = p.UnidadMedida,
                    FechaCreacion = p.FechaCreacion
                })
                .ToListAsync();

            return Results.Ok(new { Items = proyectos, Total = total });
        });

        // 2. Obtener un proyecto por Id
        grupo.MapGet("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var proyecto = await db.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);

            if (proyecto is null) return Results.NotFound();

            var dto = new ProyectoEditarDto
            {
                Id = proyecto.Id,
                Nombre = proyecto.Nombre,
                ClienteId = proyecto.ClienteId,
                Tipo = proyecto.Tipo,
                Estado = proyecto.Estado,
                UnidadMedida = proyecto.UnidadMedida,
                Notas = proyecto.Notas
            };

            return Results.Ok(dto);
        });

        // 3. Crear proyecto
        // CAMBIO (A2): el filtro .ValidarSolicitud<...>() del final ejecuta las reglas del DTO
        // (nombre obligatorio, límites de longitud y enums válidos) ANTES de entrar aquí.
        // Si llegamos a este código, los datos ya son válidos y .Trim() no puede fallar.
        grupo.MapPost("/", async (ProyectoCrearDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var proyecto = new Proyecto
            {
                // CAMBIO (A2): se limpian los espacios sobrantes; Notas vacías se guardan como null.
                Nombre = dto.Nombre.Trim(),
                ClienteId = dto.ClienteId,
                Tipo = dto.Tipo,
                Estado = dto.Estado,
                UnidadMedida = dto.UnidadMedida,
                Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
                EmpresaId = empresaId
            };

            db.Proyectos.Add(proyecto);
            await db.SaveChangesAsync();

            return Results.Created($"/api/proyectos/{proyecto.Id}", proyecto.Id);
        })
        .ValidarSolicitud<ProyectoCrearDto>();

        // 4. Editar proyecto
        // CAMBIO (A2): mismo filtro de validación que en CREAR.
        grupo.MapPut("/{id:guid}", async (Guid id, ProyectoEditarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (proyecto is null) return Results.NotFound();

            proyecto.Nombre = dto.Nombre.Trim();
            proyecto.ClienteId = dto.ClienteId;
            proyecto.Tipo = dto.Tipo;
            proyecto.Estado = dto.Estado;
            proyecto.UnidadMedida = dto.UnidadMedida;
            proyecto.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .ValidarSolicitud<ProyectoEditarDto>();

        // 5. Eliminar proyecto (Borrado lógico: AppDbContext convierte el Remove en Eliminado = true)
        grupo.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var proyecto = await db.Proyectos.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (proyecto is null) return Results.NotFound();

            db.Proyectos.Remove(proyecto);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // 6. Obtener resumen de cotización del proyecto
        grupo.MapGet("/{id:guid}/resumen", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var proyectoExiste = await db.Proyectos.AnyAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (!proyectoExiste) return Results.NotFound();

            var totalGeneral = await db.Nodos
                .Where(n => n.ProyectoId == id)
                .SelectMany(n => n.Partidas)
                .Where(p => !p.Eliminado)
                .SumAsync(p => (decimal?)p.TotalLinea) ?? 0m;

            return Results.Ok(new { TotalGeneral = totalGeneral });
        });
    }

    private static async Task<Guid> ObtenerEmpresaIdAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Guid.Empty;

        var usuario = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return usuario?.EmpresaId ?? Guid.Empty;
    }
}