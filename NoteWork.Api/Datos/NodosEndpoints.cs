using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Entidades;
using System.Security.Claims;

namespace NoteWork.Api.Endpoints;

public static class NodosEndpoints
{
    public static void MapNodosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/nodos").RequireAuthorization();

        // 1. Obtener todos los nodos de un proyecto (armados en árbol)
        grupo.MapGet("/proyecto/{proyectoId:guid}", async (Guid proyectoId, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            // Verificamos que el proyecto exista y pertenezca a la empresa del usuario
            var existeProyecto = await db.Proyectos
                .AsNoTracking()
                .AnyAsync(p => p.Id == proyectoId && p.EmpresaId == empresaId);

            if (!existeProyecto) return Results.NotFound("El proyecto no existe.");

            // Traemos todos los nodos del proyecto en UNA sola consulta para máxima velocidad
            var nodosPlana = await db.Nodos
                .AsNoTracking()
                .Where(n => n.ProyectoId == proyectoId && n.EmpresaId == empresaId)
                .OrderBy(n => n.Orden)
                .ThenBy(n => n.FechaCreacion)
                .Select(n => new NodoDto
                {
                    Id = n.Id,
                    ProyectoId = n.ProyectoId,
                    PadreId = n.PadreId,
                    Tipo = n.Tipo,
                    Nombre = n.Nombre,
                    Orden = n.Orden,
                    TotalPartidas = n.Partidas.Count(p => !p.Eliminado),
                    // CORREGIDO: Usamos TotalLinea
                    Subtotal = n.Partidas.Where(p => !p.Eliminado).Sum(p => (decimal?)p.TotalLinea) ?? 0m
                })
                .ToListAsync();

            // Armamos el árbol en memoria conectando hijos con sus padres
            var diccionario = nodosPlana.ToDictionary(n => n.Id);
            var raices = new List<NodoDto>();

            foreach (var nodo in nodosPlana)
            {
                if (nodo.PadreId.HasValue && diccionario.TryGetValue(nodo.PadreId.Value, out var padre))
                {
                    padre.Hijos.Add(nodo);
                }
                else
                {
                    raices.Add(nodo);
                }
            }

            // Acumulamos de abajo hacia arriba (Áreas -> Apartamentos -> Pisos)
            foreach (var raiz in raices)
            {
                AcumularTotalesHaciaArriba(raiz);
            }

            return Results.Ok(raices);
        });

        // 2. Obtener un nodo individual por su Id
        grupo.MapGet("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var nodo = await db.Nodos
                .AsNoTracking()
                .Where(n => n.Id == id && n.EmpresaId == empresaId)
                .Select(n => new NodoDto
                {
                    Id = n.Id,
                    ProyectoId = n.ProyectoId,
                    PadreId = n.PadreId,
                    Tipo = n.Tipo,
                    Nombre = n.Nombre,
                    Orden = n.Orden,
                    TotalPartidas = n.Partidas.Count(p => !p.Eliminado),
                    // CORREGIDO: Usamos TotalLinea
                    Subtotal = n.Partidas.Where(p => !p.Eliminado).Sum(p => (decimal?)p.TotalLinea) ?? 0m
                })
                .FirstOrDefaultAsync();

            return nodo is not null ? Results.Ok(nodo) : Results.NotFound();
        });

        // 3. Crear un nuevo nodo
        grupo.MapPost("/", async (NodoGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            // Validar que el proyecto pertenezca a la empresa
            var existeProyecto = await db.Proyectos
                .AsNoTracking()
                .AnyAsync(p => p.Id == dto.ProyectoId && p.EmpresaId == empresaId);

            if (!existeProyecto) return Results.BadRequest("El proyecto especificado no es válido.");

            // Si tiene PadreId, validar que el nodo padre pertenezca al mismo proyecto y empresa
            if (dto.PadreId.HasValue)
            {
                var padreValido = await db.Nodos
                    .AsNoTracking()
                    .AnyAsync(n => n.Id == dto.PadreId.Value && n.ProyectoId == dto.ProyectoId && n.EmpresaId == empresaId);

                if (!padreValido) return Results.BadRequest("El nodo padre especificado no es válido.");
            }

            var nuevoNodo = new Nodo
            {
                ProyectoId = dto.ProyectoId,
                PadreId = dto.PadreId,
                Tipo = dto.Tipo,
                Nombre = dto.Nombre.Trim(),
                Orden = dto.Orden,
                EmpresaId = empresaId
            };

            db.Nodos.Add(nuevoNodo);
            await db.SaveChangesAsync();

            return Results.Created($"/api/nodos/{nuevoNodo.Id}", nuevoNodo.Id);
        });

        // 4. Editar un nodo existente
        grupo.MapPut("/{id:guid}", async (Guid id, NodoGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var nodo = await db.Nodos.FirstOrDefaultAsync(n => n.Id == id && n.EmpresaId == empresaId);
            if (nodo is null) return Results.NotFound();

            // Evitar que un nodo se asigne a sí mismo como padre
            if (dto.PadreId == id)
            {
                return Results.BadRequest("Un nodo no puede ser padre de sí mismo.");
            }

            nodo.PadreId = dto.PadreId;
            nodo.Tipo = dto.Tipo;
            nodo.Nombre = dto.Nombre.Trim();
            nodo.Orden = dto.Orden;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // 5. Eliminar un nodo (Borrado lógico)
        grupo.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var nodo = await db.Nodos.FirstOrDefaultAsync(n => n.Id == id && n.EmpresaId == empresaId);
            if (nodo is null) return Results.NotFound();

            // Validar que no tenga subnodos activos antes de eliminar
            var tieneHijos = await db.Nodos.AnyAsync(n => n.PadreId == id && n.EmpresaId == empresaId);
            if (tieneHijos)
            {
                return Results.BadRequest("No se puede eliminar un nodo que contiene subnodos. Elimine primero su contenido.");
            }

            db.Nodos.Remove(nodo);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    // Método recursivo para sumar los totales hacia arriba (Pisos <- Apartamentos <- Áreas)
    private static void AcumularTotalesHaciaArriba(NodoDto nodo)
    {
        foreach (var hijo in nodo.Hijos)
        {
            AcumularTotalesHaciaArriba(hijo);
            nodo.Subtotal += hijo.Subtotal;
            nodo.TotalPartidas += hijo.TotalPartidas;
        }
    }

    private static async Task<Guid> ObtenerEmpresaIdAsync(ClaimsPrincipal user, AppDbContext db)
    {
        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdString, out Guid userId)) return Guid.Empty;

        var usuario = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return usuario?.EmpresaId ?? Guid.Empty;
    }
}