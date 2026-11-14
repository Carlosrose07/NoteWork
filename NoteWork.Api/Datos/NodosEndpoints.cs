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
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // CAMBIO (B1): el proyecto debe ser visible para este usuario (suyo, o admin de la empresa).
            // Respuesta NotFound genérica: igual si no existe o si es de otro usuario.
            var existeProyecto = await db.Proyectos
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(p => p.Id == proyectoId);

            if (!existeProyecto) return Results.NotFound();

            // Traemos todos los nodos del proyecto en UNA sola consulta para máxima velocidad.
            // CAMBIO (B1): como el proyecto ya se verificó arriba, aquí basta filtrar por
            // proyecto y empresa (así la consulta es más liviana). Los nodos eliminados
            // los excluye solo el HasQueryFilter de AppDbContext.
            var nodosPlana = await db.Nodos
                .AsNoTracking()
                .Where(n => n.ProyectoId == proyectoId && n.EmpresaId == ctx.EmpresaId)
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
        // CAMBIO (B1): NodosVisibles solo deja pasar nodos de proyectos que este usuario puede ver.
        grupo.MapGet("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var nodo = await NodosVisibles(db, ctx)
                .AsNoTracking()
                .Where(n => n.Id == id)
                .Select(n => new NodoDto
                {
                    Id = n.Id,
                    ProyectoId = n.ProyectoId,
                    PadreId = n.PadreId,
                    Tipo = n.Tipo,
                    Nombre = n.Nombre,
                    Orden = n.Orden,
                    TotalPartidas = n.Partidas.Count(p => !p.Eliminado),
                    Subtotal = n.Partidas.Where(p => !p.Eliminado).Sum(p => (decimal?)p.TotalLinea) ?? 0m
                })
                .FirstOrDefaultAsync();

            return nodo is not null ? Results.Ok(nodo) : Results.NotFound();
        });

        // 3. Crear un nuevo nodo
        // (A2) El filtro .ValidarSolicitud<...>() del final ejecuta las reglas del DTO
        // (proyecto y nombre obligatorios, tipo válido, límites) ANTES de entrar aquí.
        grupo.MapPost("/", async (NodoGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // CAMBIO (B1): el proyecto debe ser visible para este usuario. El mensaje es el
            // mismo si el proyecto no existe o si es de otra persona (no revela cuál de los dos).
            var proyectoValido = await db.Proyectos
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(p => p.Id == dto.ProyectoId);

            if (!proyectoValido) return Results.BadRequest("El proyecto especificado no es válido.");

            // Si tiene PadreId, validar que el nodo padre pertenezca al mismo proyecto y empresa
            if (dto.PadreId.HasValue)
            {
                var padreValido = await db.Nodos
                    .AsNoTracking()
                    .AnyAsync(n => n.Id == dto.PadreId.Value
                                && n.ProyectoId == dto.ProyectoId
                                && n.EmpresaId == ctx.EmpresaId);

                if (!padreValido) return Results.BadRequest("El nodo padre especificado no es válido.");
            }

            var nuevoNodo = new Nodo
            {
                ProyectoId = dto.ProyectoId,
                PadreId = dto.PadreId,
                Tipo = dto.Tipo,
                Nombre = dto.Nombre.Trim(),
                Orden = dto.Orden,
                EmpresaId = ctx.EmpresaId
            };

            db.Nodos.Add(nuevoNodo);
            await db.SaveChangesAsync();

            return Results.Created($"/api/nodos/{nuevoNodo.Id}", nuevoNodo.Id);
        })
        .ValidarSolicitud<NodoGuardarDto>();

        // 4. Editar un nodo existente
        // (A2) Mismo filtro de validación que en CREAR.
        // La detección de ciclos (A es padre de B y B de A) queda para el paso A5.
        grupo.MapPut("/{id:guid}", async (Guid id, NodoGuardarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // CAMBIO (B1): solo se puede editar un nodo de un proyecto visible para este usuario.
            var nodo = await NodosVisibles(db, ctx).FirstOrDefaultAsync(n => n.Id == id);
            if (nodo is null) return Results.NotFound();

            // Evitar que un nodo se asigne a sí mismo como padre
            if (dto.PadreId == id)
            {
                return Results.BadRequest("Un nodo no puede ser padre de sí mismo.");
            }

            // CAMBIO (B1): si se indica un padre, debe existir y ser del MISMO proyecto del nodo
            // (nodo.ProyectoId, no el que venga en el DTO: un nodo nunca cambia de proyecto aquí).
            if (dto.PadreId.HasValue)
            {
                var padreValido = await db.Nodos
                    .AsNoTracking()
                    .AnyAsync(n => n.Id == dto.PadreId.Value
                                && n.ProyectoId == nodo.ProyectoId
                                && n.EmpresaId == ctx.EmpresaId);

                if (!padreValido) return Results.BadRequest("El nodo padre especificado no es válido.");
            }

            nodo.PadreId = dto.PadreId;
            nodo.Tipo = dto.Tipo;
            nodo.Nombre = dto.Nombre.Trim();
            nodo.Orden = dto.Orden;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .ValidarSolicitud<NodoGuardarDto>();

        // 5. Eliminar un nodo (Borrado lógico: AppDbContext convierte el Remove en Eliminado = true)
        grupo.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // CAMBIO (B1): solo se puede borrar un nodo de un proyecto visible para este usuario.
            var nodo = await NodosVisibles(db, ctx).FirstOrDefaultAsync(n => n.Id == id);
            if (nodo is null) return Results.NotFound();

            // Validar que no tenga subnodos activos antes de eliminar
            // (los eliminados no cuentan: los excluye el HasQueryFilter)
            var tieneHijos = await db.Nodos.AnyAsync(n => n.PadreId == id && n.EmpresaId == ctx.EmpresaId);
            if (tieneHijos)
            {
                return Results.BadRequest("No se puede eliminar un nodo que contiene subnodos. Elimine primero su contenido.");
            }

            db.Nodos.Remove(nodo);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    // CAMBIO (B1): nodos que este usuario puede ver o tocar.
    //   - Deben ser de su empresa.
    //   - Y además: el usuario es admin, o el PROYECTO del nodo es suyo (n.Proyecto.UsuarioId).
    // Los nodos no tienen dueño propio: heredan el dueño de su proyecto.
    private static IQueryable<Nodo> NodosVisibles(AppDbContext db, ContextoUsuario ctx) =>
        db.Nodos.Where(n => n.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || n.Proyecto!.UsuarioId == ctx.UsuarioId));
    // El "!" solo calma la advertencia CS8602 del compilador. EF Core lo traduce a un JOIN
    // en SQL, así que no hay nulos en la práctica y el "!" no cambia la consulta.

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

    // CAMBIO (B1): se eliminó ObtenerEmpresaIdAsync de este archivo.
    // Ahora todos los endpoints usan db.ObtenerContextoAsync(user) (ContextoUsuario.cs).
}