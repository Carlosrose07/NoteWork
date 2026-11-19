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
        // CAMBIO (B1): VisiblesPara(ctx) deja solo los proyectos que este usuario puede ver:
        //   - usuario normal: los que él creó (UsuarioId = su Id)
        //   - admin: todos los de su empresa
        // El filtro de borrado lógico (!Eliminado) lo pone AppDbContext solo (HasQueryFilter).
        grupo.MapGet("/", async (AppDbContext db, ClaimsPrincipal user, int pagina = 1, int tamano = 20) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // Regla de paginación segura (igual que ClientesEndpoints): evita OFFSET negativos
            // y pone un tope máximo de 50 registros por petición para proteger memoria y BD.
            if (pagina < 1) pagina = 1;
            if (tamano < 1) tamano = 20;
            if (tamano > 50) tamano = 50;

            // CAMBIO (B1): se quitó el .Include(p => p.Cliente): el Select de abajo ya
            // pide el nombre del cliente, así que el Include era trabajo de más.
            var query = db.Proyectos
                .AsNoTracking()
                .VisiblesPara(ctx)
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
                    TipoDescuento = p.TipoDescuento,
                    DescuentoValor = p.DescuentoValor,
                    FechaCreacion = p.FechaCreacion
                })
                .ToListAsync();

            return Results.Ok(new { Items = proyectos, Total = total });
        });

        // 2. Obtener un proyecto por Id
        // CAMBIO (B1): si el proyecto es de otro usuario, VisiblesPara no lo devuelve
        // y la respuesta es NotFound (igual que si no existiera).
        grupo.MapGet("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var proyecto = await db.Proyectos
                .AsNoTracking()
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto is null) return Results.NotFound();

            var dto = new ProyectoEditarDto
            {
                Id = proyecto.Id,
                Nombre = proyecto.Nombre,
                ClienteId = proyecto.ClienteId,
                Tipo = proyecto.Tipo,
                Estado = proyecto.Estado,
                UnidadMedida = proyecto.UnidadMedida,
                TipoDescuento = proyecto.TipoDescuento,
                DescuentoValor = proyecto.DescuentoValor,
                Notas = proyecto.Notas
            };

            return Results.Ok(dto);
        });

        // 3. Crear proyecto
        // (A2) El filtro .ValidarSolicitud<...>() del final ejecuta las reglas del DTO
        // (nombre obligatorio, límites de longitud y enums válidos) ANTES de entrar aquí.
        grupo.MapPost("/", async (ProyectoCrearDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            if (!DescuentoEsValido(dto.TipoDescuento, dto.DescuentoValor))
                return DescuentoInvalido();

            // CAMBIO (B1): el dueño del proyecto nuevo es quien lo crea (también si es admin).
            // El cliente elegido debe ser de ese mismo dueño.
            if (!await ClienteEsValidoAsync(db, ctx, dto.ClienteId, ctx.UsuarioId))
                return ClienteInvalido();

            var proyecto = new Proyecto
            {
                Nombre = dto.Nombre.Trim(),
                ClienteId = dto.ClienteId,
                Tipo = dto.Tipo,
                Estado = dto.Estado,
                UnidadMedida = dto.UnidadMedida,
                TipoDescuento = dto.TipoDescuento,
                DescuentoValor = RedondearMonto(dto.DescuentoValor),
                Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
                EmpresaId = ctx.EmpresaId,
                UsuarioId = ctx.UsuarioId // CAMBIO (B1): asigna el dueño
            };

            db.Proyectos.Add(proyecto);
            await db.SaveChangesAsync();

            return Results.Created($"/api/proyectos/{proyecto.Id}", proyecto.Id);
        })
        .ValidarSolicitud<ProyectoCrearDto>();

        // 4. Editar proyecto
        grupo.MapPut("/{id:guid}", async (Guid id, ProyectoEditarDto dto, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            if (!DescuentoEsValido(dto.TipoDescuento, dto.DescuentoValor))
                return DescuentoInvalido();

            var proyecto = await db.Proyectos
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (proyecto is null) return Results.NotFound();

            // CAMBIO (B1): el cliente se compara con el dueño DEL PROYECTO (proyecto.UsuarioId),
            // no con quien edita. Así, si el admin edita un proyecto de usuario1, solo puede
            // elegir clientes de usuario1.
            if (!await ClienteEsValidoAsync(db, ctx, dto.ClienteId, proyecto.UsuarioId))
                return ClienteInvalido();

            proyecto.Nombre = dto.Nombre.Trim();
            proyecto.ClienteId = dto.ClienteId;
            proyecto.Tipo = dto.Tipo;
            proyecto.Estado = dto.Estado;
            proyecto.UnidadMedida = dto.UnidadMedida;
            proyecto.TipoDescuento = dto.TipoDescuento;
            proyecto.DescuentoValor = RedondearMonto(dto.DescuentoValor);
            proyecto.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();
            // CAMBIO (B1): aquí NO se toca proyecto.UsuarioId. El dueño nunca cambia.

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .ValidarSolicitud<ProyectoEditarDto>();

        // 5. Eliminar proyecto (Borrado lógico: AppDbContext convierte el Remove en Eliminado = true)
        grupo.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var proyecto = await db.Proyectos
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (proyecto is null) return Results.NotFound();

            db.Proyectos.Remove(proyecto);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        // 6. Obtener resumen de cotización del proyecto
        grupo.MapGet("/{id:guid}/resumen", async (Guid id, AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            // CAMBIO (B1 + D1): comprueba visibilidad y trae el descuento configurado en una sola consulta.
            var proyecto = await db.Proyectos
                .AsNoTracking()
                .VisiblesPara(ctx)
                .Where(p => p.Id == id)
                .Select(p => new { p.TipoDescuento, p.DescuentoValor })
                .FirstOrDefaultAsync();

            if (proyecto is null) return Results.NotFound();

            // CAMBIO (B1): se quitó .Where(p => !p.Eliminado): el HasQueryFilter de
            // AppDbContext ya excluye nodos y partidas eliminados.
            var subtotal = RedondearMonto(await db.Nodos
                .Where(n => n.ProyectoId == id)
                .SelectMany(n => n.Partidas)
                .SumAsync(p => (decimal?)p.TotalLinea) ?? 0m);

            var montoDescuento = proyecto.TipoDescuento == TipoDescuento.Porcentaje
                ? RedondearMonto(subtotal * (proyecto.DescuentoValor / 100m))
                : RedondearMonto(proyecto.DescuentoValor);

            // Regla de seguridad: el descuento nunca puede superar el subtotal ni dejar total negativo.
            if (montoDescuento < 0m) montoDescuento = 0m;
            if (montoDescuento > subtotal) montoDescuento = subtotal;

            var totalGeneral = RedondearMonto(subtotal - montoDescuento);

            return Results.Ok(new
            {
                Subtotal = subtotal,
                TipoDescuento = proyecto.TipoDescuento,
                DescuentoValor = proyecto.DescuentoValor,
                MontoDescuento = montoDescuento,
                TotalGeneral = totalGeneral
            });
        });
    }

    // CAMBIO (B1): comprueba que el cliente exista, no esté eliminado (lo hace el
    // HasQueryFilter), sea de la misma empresa y tenga el mismo dueño que el proyecto.
    // Sin cliente (null) es válido: un proyecto puede no tener cliente.
    private static async Task<bool> ClienteEsValidoAsync(
        AppDbContext db, ContextoUsuario ctx, Guid? clienteId, Guid duenoId)
    {
        if (clienteId is null) return true;

        return await db.Clientes
            .AsNoTracking()
            .AnyAsync(c => c.Id == clienteId.Value
                        && c.EmpresaId == ctx.EmpresaId
                        && c.UsuarioId == duenoId);
    }

    // Respuesta genérica: igual si el cliente no existe, está eliminado o es de otro dueño.
    // Así no se revela si el Id pertenece a otra persona.
    private static IResult ClienteInvalido() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["ClienteId"] = new[] { "El cliente seleccionado no existe o no está disponible." }
        });

    private static bool DescuentoEsValido(TipoDescuento tipo, decimal valor)
    {
        if (valor < 0m) return false;
        if (tipo == TipoDescuento.Porcentaje && valor > 100m) return false;
        return true;
    }

    private static IResult DescuentoInvalido() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["DescuentoValor"] = new[] { "El descuento en porcentaje debe estar entre 0 y 100." }
        });

    private static decimal RedondearMonto(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}