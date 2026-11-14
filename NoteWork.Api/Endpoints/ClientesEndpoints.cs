using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Clientes;
using NoteWork.Shared.Comun;
using NoteWork.Shared.Entidades;

namespace NoteWork.Api.Endpoints;

/// <summary>
/// Endpoints para la gestión de clientes (CRUD completo).
/// Todos requieren sesión activa.
/// CAMBIO (B1): cada usuario ve y edita solo los clientes que él creó.
/// El administrador ve y controla todos los de su empresa.
/// La regla vive en un solo lugar: VisiblesPara(ctx) (archivo ContextoUsuario.cs).
/// </summary>
public static class ClientesEndpoints
{
    // CAMBIO (A2): tope para el texto de búsqueda, para que nadie mande un texto gigante.
    private const int MaxLongitudBusqueda = 100;

    public static RouteGroupBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/clientes")
            .RequireAuthorization();

        // 1. LISTAR (Paginado y búsqueda)
        grupo.MapGet("", async (
            int pagina = 1,
            int tamano = 20,
            string? buscar = null,
            AppDbContext db = null!,
            ClaimsPrincipal usuario = null!) =>
        {
            // CAMBIO (B1): una sola consulta nos da usuario, empresa y si es admin.
            var ctx = await db.ObtenerContextoAsync(usuario);
            if (ctx == null)
            {
                return Results.Unauthorized();
            }

            if (pagina < 1) pagina = 1;
            if (tamano < 1) tamano = 20;
            if (tamano > 50) tamano = 50;

            // CAMBIO (B1): VisiblesPara = misma empresa Y (admin O dueño).
            // Reemplaza al filtro anterior por EmpresaId.
            var consulta = db.Clientes
                .AsNoTracking()
                .VisiblesPara(ctx);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim();

                // CAMBIO (A2): si el texto de búsqueda es demasiado largo, lo recortamos.
                if (termino.Length > MaxLongitudBusqueda)
                {
                    termino = termino[..MaxLongitudBusqueda];
                }

                consulta = consulta.Where(c => EF.Functions.ILike(c.Nombre, $"%{termino}%"));
            }

            var total = await consulta.CountAsync();

            var items = await consulta
                .OrderBy(c => c.Nombre)
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(c => new ClienteResumen(c.Id, c.Nombre, c.Telefono))
                .ToListAsync();

            var resultado = new PaginaResultado<ClienteResumen>(items, total, pagina, tamano);
            return Results.Ok(resultado);
        });

        // 2. OBTENER UNO POR ID
        grupo.MapGet("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal usuario) =>
        {
            var ctx = await db.ObtenerContextoAsync(usuario);
            if (ctx == null)
            {
                return Results.Unauthorized();
            }

            // CAMBIO (B1): si el cliente es de otro usuario, no aparece aquí y
            // la respuesta es NotFound (igual que si no existiera).
            var cliente = await db.Clientes
                .AsNoTracking()
                .VisiblesPara(ctx)
                .Where(c => c.Id == id)
                .Select(c => new ClienteGuardarSolicitud
                {
                    Nombre = c.Nombre,
                    Telefono = c.Telefono,
                    Direccion = c.Direccion,
                    Notas = c.Notas
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(cliente);
        });

        // 3. CREAR
        // CAMBIO (A2): el filtro .ValidarSolicitud<...>() del final ejecuta las reglas del DTO
        // (nombre obligatorio y límites de longitud) ANTES de entrar aquí.
        grupo.MapPost("", async (
            ClienteGuardarSolicitud solicitud,
            AppDbContext db,
            ClaimsPrincipal usuario) =>
        {
            var ctx = await db.ObtenerContextoAsync(usuario);
            if (ctx == null)
            {
                return Results.Unauthorized();
            }

            var nuevoCliente = new Cliente
            {
                Id = Guid.NewGuid(),
                EmpresaId = ctx.EmpresaId,
                // CAMBIO (B1): el dueño es quien lo crea (si lo crea el admin, el dueño es el admin).
                UsuarioId = ctx.UsuarioId,
                Nombre = solicitud.Nombre.Trim(),
                Telefono = string.IsNullOrWhiteSpace(solicitud.Telefono) ? null : solicitud.Telefono.Trim(),
                Direccion = string.IsNullOrWhiteSpace(solicitud.Direccion) ? null : solicitud.Direccion.Trim(),
                Notas = string.IsNullOrWhiteSpace(solicitud.Notas) ? null : solicitud.Notas.Trim()
            };

            db.Clientes.Add(nuevoCliente);
            await db.SaveChangesAsync();

            var respuesta = new ClienteResumen(nuevoCliente.Id, nuevoCliente.Nombre, nuevoCliente.Telefono);
            return Results.Created($"/api/clientes/{nuevoCliente.Id}", respuesta);
        })
        .ValidarSolicitud<ClienteGuardarSolicitud>();

        // 4. EDITAR
        // CAMBIO (A2): mismo filtro de validación que en CREAR.
        grupo.MapPut("/{id:guid}", async (
            Guid id,
            ClienteGuardarSolicitud solicitud,
            AppDbContext db,
            ClaimsPrincipal usuario) =>
        {
            var ctx = await db.ObtenerContextoAsync(usuario);
            if (ctx == null)
            {
                return Results.Unauthorized();
            }

            // CAMBIO (B1): solo encuentra el cliente si es visible para este usuario.
            var cliente = await db.Clientes
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return Results.NotFound();
            }

            // IMPORTANTE (B1): aquí NO se toca cliente.UsuarioId.
            // Si el admin edita un cliente de usuario1, sigue siendo de usuario1.
            cliente.Nombre = solicitud.Nombre.Trim();
            cliente.Telefono = string.IsNullOrWhiteSpace(solicitud.Telefono) ? null : solicitud.Telefono.Trim();
            cliente.Direccion = string.IsNullOrWhiteSpace(solicitud.Direccion) ? null : solicitud.Direccion.Trim();
            cliente.Notas = string.IsNullOrWhiteSpace(solicitud.Notas) ? null : solicitud.Notas.Trim();

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .ValidarSolicitud<ClienteGuardarSolicitud>();

        // 5. BORRAR (Borrado lógico: AppDbContext convierte el Remove en Eliminado = true)
        grupo.MapDelete("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal usuario) =>
        {
            var ctx = await db.ObtenerContextoAsync(usuario);
            if (ctx == null)
            {
                return Results.Unauthorized();
            }

            // CAMBIO (B1): un usuario solo puede borrar lo suyo; el admin, todo lo de su empresa.
            var cliente = await db.Clientes
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return Results.NotFound();
            }

            db.Clientes.Remove(cliente);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return grupo;
    }

    // CAMBIO (B1): se eliminó ObtenerEmpresaIdAsync de este archivo.
    // Ahora se usa db.ObtenerContextoAsync(usuario), que vive en ContextoUsuario.cs.
}