using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Entidades;
using System.Security.Claims;

namespace NoteWork.Api.Endpoints;

public static class PartidasEndpoints
{
    public static void MapPartidasEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/partidas").RequireAuthorization();

        // 1. Obtener todas las partidas de un nodo (Área)
        grupo.MapGet("/nodo/{nodoId:guid}", async (Guid nodoId, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var existeNodo = await db.Nodos
                .AsNoTracking()
                .AnyAsync(n => n.Id == nodoId && n.EmpresaId == empresaId);

            if (!existeNodo) return Results.NotFound("El área especificada no existe.");

            var partidas = await db.Partidas
                .AsNoTracking()
                .Where(p => p.NodoId == nodoId && p.EmpresaId == empresaId)
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.FechaCreacion)
                .Select(p => new PartidaDto
                {
                    Id = p.Id,
                    NodoId = p.NodoId,
                    Descripcion = p.Descripcion,
                    Especificacion = p.Especificacion,
                    Material = p.Material,
                    Marca = p.Marca,
                    Cantidad = p.Cantidad,
                    TotalLinea = p.TotalLinea,
                    AnchoMm = p.AnchoMm,
                    AltoMm = p.AltoMm,
                    ProfundidadMm = p.ProfundidadMm,
                    Descuento = p.Descuento,
                    CostoInterno = p.CostoInterno,
                    NotaInterna = p.NotaInterna,
                    Orden = p.Orden
                })
                .ToListAsync();

            return Results.Ok(partidas);
        });

        // 2. Obtener una partida individual por su Id
        grupo.MapGet("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var partida = await db.Partidas
                .AsNoTracking()
                .Where(p => p.Id == id && p.EmpresaId == empresaId)
                .Select(p => new PartidaDto
                {
                    Id = p.Id,
                    NodoId = p.NodoId,
                    Descripcion = p.Descripcion,
                    Especificacion = p.Especificacion,
                    Material = p.Material,
                    Marca = p.Marca,
                    Cantidad = p.Cantidad,
                    TotalLinea = p.TotalLinea,
                    AnchoMm = p.AnchoMm,
                    AltoMm = p.AltoMm,
                    ProfundidadMm = p.ProfundidadMm,
                    Descuento = p.Descuento,
                    CostoInterno = p.CostoInterno,
                    NotaInterna = p.NotaInterna,
                    Orden = p.Orden
                })
                .FirstOrDefaultAsync();

            return partida is not null ? Results.Ok(partida) : Results.NotFound();
        });

        // 3. Crear una nueva partida
        grupo.MapPost("/", async (PartidaGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            // Regla de negocio: el nodo debe existir, pertenecer a la empresa y ser de tipo Area
            var nodo = await db.Nodos
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == dto.NodoId && n.EmpresaId == empresaId);

            if (nodo is null) return Results.BadRequest("El área especificada no es válida.");

            if (nodo.Tipo != TipoNodo.Area)
            {
                return Results.BadRequest("Solo se pueden agregar partidas dentro de un nodo de tipo Área.");
            }

            var nuevaPartida = new Partida
            {
                NodoId = dto.NodoId,
                Descripcion = dto.Descripcion.Trim(),
                Especificacion = string.IsNullOrWhiteSpace(dto.Especificacion) ? null : dto.Especificacion.Trim(),
                Material = string.IsNullOrWhiteSpace(dto.Material) ? null : dto.Material.Trim(),
                Marca = string.IsNullOrWhiteSpace(dto.Marca) ? null : dto.Marca.Trim(),
                Cantidad = dto.Cantidad,
                TotalLinea = dto.TotalLinea,
                AnchoMm = dto.AnchoMm,
                AltoMm = dto.AltoMm,
                ProfundidadMm = dto.ProfundidadMm,
                Descuento = dto.Descuento,
                CostoInterno = dto.CostoInterno,
                NotaInterna = string.IsNullOrWhiteSpace(dto.NotaInterna) ? null : dto.NotaInterna.Trim(),
                Orden = dto.Orden,
                EmpresaId = empresaId
            };

            db.Partidas.Add(nuevaPartida);
            await db.SaveChangesAsync();

            return Results.Created($"/api/partidas/{nuevaPartida.Id}", nuevaPartida.Id);
        });

        // 4. Editar una partida existente
        grupo.MapPut("/{id:guid}", async (Guid id, PartidaGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var partida = await db.Partidas.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (partida is null) return Results.NotFound();

            partida.Descripcion = dto.Descripcion.Trim();
            partida.Especificacion = string.IsNullOrWhiteSpace(dto.Especificacion) ? null : dto.Especificacion.Trim();
            partida.Material = string.IsNullOrWhiteSpace(dto.Material) ? null : dto.Material.Trim();
            partida.Marca = string.IsNullOrWhiteSpace(dto.Marca) ? null : dto.Marca.Trim();
            partida.Cantidad = dto.Cantidad;
            partida.TotalLinea = dto.TotalLinea;
            partida.AnchoMm = dto.AnchoMm;
            partida.AltoMm = dto.AltoMm;
            partida.ProfundidadMm = dto.ProfundidadMm;
            partida.Descuento = dto.Descuento;
            partida.CostoInterno = dto.CostoInterno;
            partida.NotaInterna = string.IsNullOrWhiteSpace(dto.NotaInterna) ? null : dto.NotaInterna.Trim();
            partida.Orden = dto.Orden;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // 5. Eliminar una partida (Borrado lógico)
        grupo.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var empresaId = await ObtenerEmpresaIdAsync(user, db);
            if (empresaId == Guid.Empty) return Results.Unauthorized();

            var partida = await db.Partidas.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
            if (partida is null) return Results.NotFound();

            db.Partidas.Remove(partida);
            await db.SaveChangesAsync();

            return Results.NoContent();
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