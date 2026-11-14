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

            // CAMBIO (B1): el nodo debe ser de un proyecto visible para este usuario.
            // NotFound genérico: igual si no existe o si es de otra persona.
            var existeNodo = await NodosVisibles(db, ctx)
                .AsNoTracking()
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
        // CAMBIO (B1): PartidasVisibles solo deja pasar partidas de proyectos que este usuario puede ver.
        grupo.MapGet("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var partida = await PartidasVisibles(db, ctx)
                .AsNoTracking()
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

            // CAMBIO (B1): el nodo debe ser de un proyecto visible para este usuario.
            // El mensaje es el mismo si el área no existe o es de otra persona (no revela cuál de los dos).
            var nodo = await NodosVisibles(db, ctx)
                .AsNoTracking()
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
                TotalLinea = dto.TotalLinea,
                AnchoMm = dto.AnchoMm,
                AltoMm = dto.AltoMm,
                ProfundidadMm = dto.ProfundidadMm,
                Descuento = dto.Descuento,
                CostoInterno = dto.CostoInterno,
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

            // CAMBIO (B1): solo se puede editar una partida de un proyecto visible para este usuario.
            var partida = await PartidasVisibles(db, ctx).FirstOrDefaultAsync(p => p.Id == id);
            if (partida is null) return Results.NotFound();

            // La partida no cambia de nodo: partida.NodoId no se toca aquí.
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
        })
        .ValidarSolicitud<PartidaGuardarDto>();

        // 5. Eliminar una partida (Borrado lógico: AppDbContext convierte el Remove en Eliminado = true)
        grupo.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // CAMBIO (B1): solo se puede borrar una partida de un proyecto visible para este usuario.
            var partida = await PartidasVisibles(db, ctx).FirstOrDefaultAsync(p => p.Id == id);
            if (partida is null) return Results.NotFound();

            db.Partidas.Remove(partida);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    // CAMBIO (B1): partidas que este usuario puede ver o tocar.
    //   - Deben ser de su empresa.
    //   - Y además: el usuario es admin, o el PROYECTO de la partida es suyo.
    // Las partidas no tienen dueño propio: heredan el de su proyecto, a través de partida -> nodo -> proyecto.
    // (El "!" solo calma la advertencia CS8602; EF Core lo traduce a JOIN en SQL.)
    private static IQueryable<Partida> PartidasVisibles(AppDbContext db, ContextoUsuario ctx) =>
        db.Partidas.Where(p => p.EmpresaId == ctx.EmpresaId
                            && (ctx.EsAdmin || p.Nodo!.Proyecto!.UsuarioId == ctx.UsuarioId));

    // CAMBIO (B1): misma regla que NodosVisibles de NodosEndpoints.cs (copiada aquí a propósito).
    // Mejora futura: moverla a ContextoUsuario.cs para tener una sola copia de esta regla de seguridad.
    private static IQueryable<Nodo> NodosVisibles(AppDbContext db, ContextoUsuario ctx) =>
        db.Nodos.Where(n => n.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || n.Proyecto!.UsuarioId == ctx.UsuarioId));

    // CAMBIO (B1): se eliminó ObtenerEmpresaIdAsync de este archivo.
    // Ahora todos los endpoints usan db.ObtenerContextoAsync(user) (ContextoUsuario.cs).
}