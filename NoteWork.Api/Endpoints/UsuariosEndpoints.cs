using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Cuenta;

namespace NoteWork.Api.Endpoints;

// Endpoints de GESTIÓN de usuarios (Bloque B). Solo los puede usar un Admin.
public static class UsuariosEndpoints
{
    private const int MinLongitudNombre = 2;
    private const int MaxLongitudNombre = 150;

    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/cuenta/usuarios")
            .RequireAuthorization(politica => politica.RequireRole("Admin"));

        grupo.MapGet("/", ListarAsync);

        grupo.MapPut("/{id:guid}", EditarNombreAsync)
            .ValidarSolicitud<EditarUsuarioSolicitud>();

        grupo.MapPost("/{id:guid}/desactivar", DesactivarAsync);
        grupo.MapPost("/{id:guid}/reactivar", ReactivarAsync);

        grupo.MapPost("/{id:guid}/password", RestablecerPasswordAsync)
            .ValidarSolicitud<RestablecerPasswordSolicitud>();

        return app;
    }

    // ------------------------------------------------------------------
    // LISTAR
    // ------------------------------------------------------------------
    private static async Task<IResult> ListarAsync(
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager,
        AppDbContext db)
    {
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        var esAdminPrincipal = adminActual.CreadoPorId is null;

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
                u.FechaCreacion,
                u.CreadoPorId
            })
            .ToListAsync();

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

        var resultado = usuarios.Select(u => new UsuarioResumen
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            NombreCompleto = u.NombreCompleto,
            Rol = rolPorUsuario.GetValueOrDefault(u.Id, string.Empty),
            Activo = u.Activo,
            FechaCreacion = u.FechaCreacion,
            PuedeGestionar = esAdminPrincipal || u.CreadoPorId == adminActual.Id
        }).ToList();

        return Results.Ok(resultado);
    }

    // ------------------------------------------------------------------
    // EDITAR NOMBRE Y ROL
    // ------------------------------------------------------------------
    private static async Task<IResult> EditarNombreAsync(
        Guid id,
        EditarUsuarioSolicitud solicitud,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager,
        AppDbContext db)
    {
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        var nombre = solicitud.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return Error("El nombre es obligatorio.");
        if (nombre.Length < MinLongitudNombre)
            return Error($"El nombre debe tener al menos {MinLongitudNombre} caracteres.");
        if (nombre.Length > MaxLongitudNombre)
            return Error($"El nombre no puede pasar de {MaxLongitudNombre} caracteres.");

        var rol = solicitud.Rol?.Trim() switch
        {
            var r when string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) => "Admin",
            var r when string.IsNullOrWhiteSpace(r) || string.Equals(r, "Usuario", StringComparison.OrdinalIgnoreCase) => "Usuario",
            _ => null
        };
        if (rol is null)
            return Error("El rol debe ser Admin o Usuario.");

        var usuario = await BuscarEnMiEmpresaAsync(id, adminActual, userManager);
        if (usuario is null)
            return Results.NotFound();

        if (!PuedeGestionarUsuario(adminActual, usuario))
            return Error("No tienes permiso para modificar este usuario.");

        if (adminActual.CreadoPorId is not null && rol == "Admin")
            return Error("Solo el administrador principal puede asignar el rol Admin.");

        var rolesActuales = await userManager.GetRolesAsync(usuario);
        var eraAdmin = rolesActuales.Contains("Admin");

        if (eraAdmin && rol == "Usuario" && usuario.Activo)
        {
            var adminsActivos = await ContarAdminsActivosAsync(adminActual.EmpresaId, db);
            if (adminsActivos <= 1)
                return Error("No puedes quitarle el rol Admin al único administrador activo de la empresa.");
        }

        usuario.NombreCompleto = nombre;

        var resultado = await userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            return ErrorIdentity(resultado);

        if (!rolesActuales.Contains(rol))
        {
            if (rolesActuales.Count > 0)
                await userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            await userManager.AddToRoleAsync(usuario, rol);
        }

        return Results.NoContent();
    }

    // ------------------------------------------------------------------
    // DESACTIVAR / REACTIVAR
    // ------------------------------------------------------------------
    private static Task<IResult> DesactivarAsync(
        Guid id,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager,
        AppDbContext db)
        => CambiarActivoAsync(id, activo: false, admin, userManager, db);

    private static Task<IResult> ReactivarAsync(
        Guid id,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager,
        AppDbContext db)
        => CambiarActivoAsync(id, activo: true, admin, userManager, db);

    private static async Task<IResult> CambiarActivoAsync(
        Guid id,
        bool activo,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager,
        AppDbContext db)
    {
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        if (!activo && id == adminActual.Id)
            return Error("No puedes desactivarte a ti mismo.");

        var usuario = await BuscarEnMiEmpresaAsync(id, adminActual, userManager);
        if (usuario is null)
            return Results.NotFound();

        if (!PuedeGestionarUsuario(adminActual, usuario))
            return Error("No tienes permiso para activar o desactivar este usuario.");

        if (usuario.Activo != activo)
        {
            if (!activo && await userManager.IsInRoleAsync(usuario, "Admin"))
            {
                var adminsActivos = await ContarAdminsActivosAsync(adminActual.EmpresaId, db);
                if (adminsActivos <= 1)
                    return Error("No puedes desactivar al único administrador activo de la empresa.");
            }

            if (activo)
            {
                var maxUsuarios = await db.Set<NoteWork.Shared.Entidades.Empresa>()
                    .AsNoTracking()
                    .Where(e => e.Id == adminActual.EmpresaId)
                    .Select(e => e.MaxUsuarios)
                    .FirstOrDefaultAsync();

                if (maxUsuarios <= 0) maxUsuarios = 3;

                var activosActuales = await db.Users
                    .CountAsync(u => u.EmpresaId == adminActual.EmpresaId && u.Activo);

                if (activosActuales >= maxUsuarios)
                    return Error($"Has alcanzado el límite de {maxUsuarios} usuarios activos de tu plan.");
            }

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

        if (!PuedeGestionarUsuario(adminActual, usuario))
            return Error("No tienes permiso para cambiar la contraseña de este usuario.");

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await userManager.ResetPasswordAsync(usuario, token, solicitud.NuevaPassword);

        if (!resultado.Succeeded)
            return ErrorIdentity(resultado);

        return Results.NoContent();
    }

    // ------------------------------------------------------------------
    // AYUDANTES
    // ------------------------------------------------------------------
    private static bool PuedeGestionarUsuario(Usuario adminActual, Usuario objetivo)
        => adminActual.CreadoPorId is null || objetivo.CreadoPorId == adminActual.Id;

    private static Task<int> ContarAdminsActivosAsync(Guid empresaId, AppDbContext db)
        => (from u in db.Users.AsNoTracking()
            join ur in db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where u.EmpresaId == empresaId && u.Activo && r.Name == "Admin"
            select u.Id).CountAsync();

    private static Task<Usuario?> BuscarEnMiEmpresaAsync(
        Guid id,
        Usuario adminActual,
        UserManager<Usuario> userManager)
        => userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == adminActual.EmpresaId);

    private static IResult Error(string mensaje)
        => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["error"] = [mensaje]
        });

    private static IResult ErrorIdentity(IdentityResult resultado)
        => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["error"] = resultado.Errors
                .Select(CuentaEndpoints.TraducirErrorIdentity)
                .Distinct()
                .ToArray()
        });
}