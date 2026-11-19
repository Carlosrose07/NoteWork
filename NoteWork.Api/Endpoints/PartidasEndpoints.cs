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
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // VisiblesPara(ctx) vive centralizado en ContextoUsuario.cs.
            var existeNodo = await db.Nodos
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(n => n.Id == nodoId);

            if (!existeNodo) return Results.NotFound();

            // Como el nodo ya se verificó arriba, basta filtrar por nodo y empresa.
            // Las partidas eliminadas las excluye el HasQueryFilter de AppDbContext.
            var partidas = await db.Partidas
                .AsNoTracking()
                .Where(p => p.NodoId == nodoId && p.EmpresaId == ctx.EmpresaId)
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
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var partida = await db.Partidas
                .AsNoTracking()
                .VisiblesPara(ctx)
                .Where(p => p.Id == id)
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
        // (A2) El filtro .ValidarSolicitud<...>() del final ejecuta las reglas del DTO
        // (descripción obligatoria, montos no negativos, límites de longitud) ANTES de entrar aquí.
        grupo.MapPost("/", async (PartidaGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var nodo = await db.Nodos
                .AsNoTracking()
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(n => n.Id == dto.NodoId);

            if (nodo is null) return Results.BadRequest("El área especificada no es válida.");

            // Regla de negocio: solo se agregan partidas dentro de un nodo de tipo Area
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
                TotalLinea = RedondearMonto(dto.TotalLinea),
                AnchoMm = dto.AnchoMm,
                AltoMm = dto.AltoMm,
                ProfundidadMm = dto.ProfundidadMm,
                Descuento = RedondearMonto(dto.Descuento),
                CostoInterno = RedondearMonto(dto.CostoInterno),
                NotaInterna = string.IsNullOrWhiteSpace(dto.NotaInterna) ? null : dto.NotaInterna.Trim(),
                Orden = dto.Orden,
                EmpresaId = ctx.EmpresaId
            };

            db.Partidas.Add(nuevaPartida);
            await db.SaveChangesAsync();

            return Results.Created($"/api/partidas/{nuevaPartida.Id}", nuevaPartida.Id);
        })
        .ValidarSolicitud<PartidaGuardarDto>();

        // 4. Editar una partida existente
        // (A2) Mismo filtro de validación que en CREAR.
        grupo.MapPut("/{id:guid}", async (Guid id, PartidaGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var partida = await db.Partidas
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partida is null) return Results.NotFound();

            // La partida no cambia de nodo: partida.NodoId no se toca aquí.
            partida.Descripcion = dto.Descripcion.Trim();
            partida.Especificacion = string.IsNullOrWhiteSpace(dto.Especificacion) ? null : dto.Especificacion.Trim();
            partida.Material = string.IsNullOrWhiteSpace(dto.Material) ? null : dto.Material.Trim();
            partida.Marca = string.IsNullOrWhiteSpace(dto.Marca) ? null : dto.Marca.Trim();
            partida.Cantidad = dto.Cantidad;
            partida.TotalLinea = RedondearMonto(dto.TotalLinea);
            partida.AnchoMm = dto.AnchoMm;
            partida.AltoMm = dto.AltoMm;
            partida.ProfundidadMm = dto.ProfundidadMm;
            partida.Descuento = RedondearMonto(dto.Descuento);
            partida.CostoInterno = RedondearMonto(dto.CostoInterno);
            partida.NotaInterna = string.IsNullOrWhiteSpace(dto.NotaInterna) ? null : dto.NotaInterna.Trim();
            partida.Orden = dto.Orden;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .ValidarSolicitud<PartidaGuardarDto>();

        // 5. Eliminar una partida (Borrado lógico: AppDbContext convierte el Remove en Eliminado = true)
        grupo.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var partida = await db.Partidas
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (partida is null) return Results.NotFound();

            db.Partidas.Remove(partida);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    // Normaliza los montos a 2 decimales en el servidor para evitar diferencias de centavos en las sumas.
    private static decimal RedondearMonto(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static decimal? RedondearMonto(decimal? valor) =>
        valor.HasValue ? Math.Round(valor.Value, 2, MidpointRounding.AwayFromZero) : null;
}