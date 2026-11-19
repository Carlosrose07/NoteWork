using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Cuenta;

namespace NoteWork.Api.Endpoints;

// Endpoints de GESTIÓN de usuarios (Bloque B). Solo los puede usar un Admin.
// Va en un archivo aparte de CuentaEndpoints (login, /yo, crear) para que
// cada archivo tenga una sola responsabilidad y no crezca sin control.
public static class UsuariosEndpoints
{
    // Reglas de longitud del nombre alineadas con ListaUsuarios.razor (entre 2 y 150 caracteres).
    private const int MinLongitudNombre = 2;
    private const int MaxLongitudNombre = 150;

    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        // Mismo prefijo que "crear usuario" (/api/cuenta/usuarios). No choca:
        // aquí GET/PUT/POST con /{id}, allá un POST sin id (crear).
        // Todo el grupo exige sesión Y rol Admin. Sin token -> 401; con token
        // pero sin ser Admin -> 403. El control real está aquí, en el servidor.
        var grupo = app.MapGroup("/api/cuenta/usuarios")
            .RequireAuthorization(politica => politica.RequireRole("Admin"));

        // GET /api/cuenta/usuarios -> lista de usuarios de la empresa del admin.
        grupo.MapGet("/", ListarAsync);

        // PUT /api/cuenta/usuarios/{id} -> cambia SOLO el nombre (correo y rol no se tocan).
        // ValidarSolicitud ejecuta las reglas del DTO antes de entrar (mismo filtro de A2).
        grupo.MapPut("/{id:guid}", EditarNombreAsync)
            .ValidarSolicitud<EditarUsuarioSolicitud>();

        // POST .../{id}/desactivar y .../reactivar -> prenden y apagan el campo Activo.
        // No borramos al usuario: sus proyectos y clientes guardan su Id (UsuarioId)
        // y no hay clave foránea, así que borrarlo dejaría datos huérfanos.
        grupo.MapPost("/{id:guid}/desactivar", DesactivarAsync);
        grupo.MapPost("/{id:guid}/reactivar", ReactivarAsync);

        // POST .../{id}/password -> el admin escribe una contraseña nueva.
        grupo.MapPost("/{id:guid}/password", RestablecerPasswordAsync)
            .ValidarSolicitud<RestablecerPasswordSolicitud>();

        return app;
    }

    // ------------------------------------------------------------------
    // LISTAR (sin cambios respecto a la versión anterior)
    // ------------------------------------------------------------------
    private static async Task<IResult> ListarAsync(
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager,
        AppDbContext db)
    {
        // 1. Quién llama. CLAVE DE SEGURIDAD: la empresa sale de la base de
        //    datos, nunca de la petición, así un admin no puede ver usuarios
        //    de otra empresa.
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        // 2. Usuarios de ESA empresa. AsNoTracking: solo leemos, no editamos,
        //    y así EF gasta menos memoria. Traemos solo las columnas necesarias
        //    (nunca PasswordHash ni otros datos internos de Identity).
        var usuarios = await db.Users
            .AsNoTracking()
            .Where(u => u.EmpresaId == adminActual.EmpresaId)
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.NombreCompleto,
                u.Activo,
                u.FechaCreacion
            })
            .ToListAsync();

        // 3. Roles de esos usuarios, en UNA sola consulta (en vez de una por
        //    usuario). Después los unimos en memoria con un diccionario.
        var ids = usuarios.Select(u => u.Id).ToList();

        var roles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, Rol = r.Name }
        ).ToListAsync();

        var rolPorUsuario = roles
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.First().Rol ?? string.Empty);

        // 4. Armamos la respuesta con el modelo de Shared.
        var resultado = usuarios.Select(u => new UsuarioResumen
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            NombreCompleto = u.NombreCompleto,
            Rol = rolPorUsuario.GetValueOrDefault(u.Id, string.Empty),
            Activo = u.Activo,
            FechaCreacion = u.FechaCreacion
        }).ToList();

        return Results.Ok(resultado);
    }

    // ------------------------------------------------------------------
    // EDITAR NOMBRE
    // ------------------------------------------------------------------
    private static async Task<IResult> EditarNombreAsync(
        Guid id,
        EditarUsuarioSolicitud solicitud,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager)
    {
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        // Revisión en servidor: evita nombres vacíos, de 1 sola letra o demasiado largos.
        var nombre = solicitud.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return Error("El nombre es obligatorio.");
        if (nombre.Length < MinLongitudNombre)
            return Error($"El nombre debe tener al menos {MinLongitudNombre} caracteres.");
        if (nombre.Length > MaxLongitudNombre)
            return Error($"El nombre no puede pasar de {MaxLongitudNombre} caracteres.");

        // Buscamos por Id Y por la empresa del admin. Si el usuario es de otra
        // empresa, el resultado es el mismo que si no existiera: "no encontrado".
        var usuario = await BuscarEnMiEmpresaAsync(id, adminActual, userManager);
        if (usuario is null)
            return Results.NotFound();

        usuario.NombreCompleto = nombre;

        var resultado = await userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            return ErrorIdentity(resultado);

        return Results.NoContent();
    }

    // ------------------------------------------------------------------
    // DESACTIVAR / REACTIVAR
    // ------------------------------------------------------------------
    private static Task<IResult> DesactivarAsync(
        Guid id,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager)
        => CambiarActivoAsync(id, activo: false, admin, userManager);

    private static Task<IResult> ReactivarAsync(
        Guid id,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager)
        => CambiarActivoAsync(id, activo: true, admin, userManager);

    // Las dos acciones comparten la misma lógica, así que viven en un solo método.
    private static async Task<IResult> CambiarActivoAsync(
        Guid id,
        bool activo,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager)
    {
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        // REGLA: el admin no puede desactivarse a sí mismo. Evita que se quede
        // fuera de la app por un clic equivocado. Se revisa antes de buscar.
        if (!activo && id == adminActual.Id)
            return Error("No puedes desactivarte a ti mismo.");

        var usuario = await BuscarEnMiEmpresaAsync(id, adminActual, userManager);
        if (usuario is null)
            return Results.NotFound();

        // Si ya está en ese estado no hay nada que guardar; respondemos igual de bien.
        if (usuario.Activo != activo)
        {
            usuario.Activo = activo;

            var resultado = await userManager.UpdateAsync(usuario);
            if (!resultado.Succeeded)
                return ErrorIdentity(resultado);
        }

        return Results.NoContent();
    }

    // ------------------------------------------------------------------
    // RESTABLECER CONTRASEÑA
    // ------------------------------------------------------------------
    private static async Task<IResult> RestablecerPasswordAsync(
        Guid id,
        RestablecerPasswordSolicitud solicitud,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager)
    {
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(solicitud.NuevaPassword))
            return Error("La contraseña nueva es obligatoria.");

        var usuario = await BuscarEnMiEmpresaAsync(id, adminActual, userManager);
        if (usuario is null)
            return Results.NotFound();

        // Identity no deja "asignar" una contraseña directamente: pide un token
        // de restablecimiento (válido solo en este momento) y luego la cambia.
        // Así se aplican TODAS las reglas de complejidad de Identity (largo,
        // mayúscula, número, símbolo) y se renueva el sello de seguridad.
        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await userManager.ResetPasswordAsync(usuario, token, solicitud.NuevaPassword);

        if (!resultado.Succeeded)
            return ErrorIdentity(resultado);

        return Results.NoContent();
    }

    // ------------------------------------------------------------------
    // AYUDANTES
    // ------------------------------------------------------------------

    // Busca un usuario por Id SOLO dentro de la empresa del admin que llama.
    // Es la regla de seguridad central: la empresa sale de la base de datos
    // (adminActual), nunca de lo que mande el cliente en la solicitud.
    // Sin AsNoTracking a propósito: después vamos a modificar y guardar.
    private static Task<Usuario?> BuscarEnMiEmpresaAsync(
        Guid id,
        Usuario adminActual,
        UserManager<Usuario> userManager)
        => userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == adminActual.EmpresaId);

    // Error 400 con un mensaje legible, en el formato estándar de errores de validación.
    private static IResult Error(string mensaje)
        => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["error"] = [mensaje]
        });

    // Convierte los errores de Identity en el mismo formato de error 400.
    // CAMBIO (Bloque B, Prueba 5): antes se enviaba e.Description, que Identity
    // escribe SIEMPRE en inglés ("Passwords must have at least one digit...").
    // Ahora cada error pasa por CuentaEndpoints.TraducirErrorIdentity, la misma
    // traducción que usa "crear usuario", para que la app hable un solo idioma
    // y no haya dos listas de mensajes que mantener.
    // Distinct(): si dos errores traducen al mismo texto, se muestra una sola vez.
    // La clave se queda en "error" para que el Client lo siga mostrando igual
    // (no hay que tocar ServicioUsuarios ni ListaUsuarios).
    private static IResult ErrorIdentity(IdentityResult resultado)
        => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["error"] = resultado.Errors
                .Select(CuentaEndpoints.TraducirErrorIdentity)
                .Distinct()
                .ToArray()
        });
}