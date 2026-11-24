using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NoteWork.Api.Datos;
using NoteWork.Shared.Cuenta;

namespace NoteWork.Api.Endpoints;

// Agrupa los endpoints de cuenta (login, quién soy y crear usuarios).
public static class CuentaEndpoints
{
    public static IEndpointRouteBuilder MapCuentaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/cuenta");

        grupo.MapPost("/login", LoginAsync).AllowAnonymous();
        grupo.MapGet("/yo", YoAsync).RequireAuthorization();

        grupo.MapPost("/usuarios", CrearUsuarioAsync)
            .RequireAuthorization(politica => politica.RequireRole("Admin"));

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginSolicitud solicitud,
        UserManager<Usuario> userManager,
        SignInManager<Usuario> signInManager,
        IOptionsMonitor<BearerTokenOptions> opcionesToken)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Email) || string.IsNullOrWhiteSpace(solicitud.Password))
            return Results.BadRequest("Correo y contraseña son obligatorios.");

        var usuario = await userManager.FindByEmailAsync(solicitud.Email);

        if (usuario is null)
            return Results.Unauthorized();

        if (!usuario.Activo)
            return Results.Unauthorized();

        var resultado = await signInManager.CheckPasswordSignInAsync(
            usuario, solicitud.Password, lockoutOnFailure: true);

        if (!resultado.Succeeded)
            return Results.Unauthorized();

        var principal = await signInManager.CreateUserPrincipalAsync(usuario);
        var opciones = opcionesToken.Get(IdentityConstants.BearerScheme);
        var expira = DateTimeOffset.UtcNow + opciones.BearerTokenExpiration;

        var propiedades = new AuthenticationProperties { ExpiresUtc = expira };
        var ticket = new AuthenticationTicket(principal, propiedades, IdentityConstants.BearerScheme);
        var token = opciones.BearerTokenProtector.Protect(ticket);

        var roles = await userManager.GetRolesAsync(usuario);

        return Results.Ok(new LoginRespuesta
        {
            AccessToken = token,
            ExpiraEn = expira.UtcDateTime,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email ?? string.Empty,
            Rol = roles.FirstOrDefault() ?? string.Empty
        });
    }

    private static async Task<IResult> YoAsync(
        ClaimsPrincipal actual,
        UserManager<Usuario> userManager)
    {
        var usuario = await userManager.GetUserAsync(actual);

        if (usuario is null)
            return Results.Unauthorized();

        return Results.Ok(new
        {
            usuario.Email,
            usuario.NombreCompleto,
            usuario.EmpresaId,
            Rol = actual.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            EsAdminPrincipal = usuario.CreadoPorId is null
        });
    }

    private static async Task<IResult> CrearUsuarioAsync(
        CrearUsuarioSolicitud solicitud,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager,
        AppDbContext db)
    {
        var erroresValidacion = new List<ValidationResult>();
        var contexto = new ValidationContext(solicitud);
        if (!Validator.TryValidateObject(solicitud, contexto, erroresValidacion, validateAllProperties: true))
        {
            var mensajes = erroresValidacion
                .Select(e => e.ErrorMessage ?? "Dato no válido.")
                .ToArray();
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Solicitud"] = mensajes
            });
        }

        var rol = solicitud.Rol?.Trim() switch
        {
            var r when string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) => "Admin",
            var r when string.IsNullOrWhiteSpace(r) || string.Equals(r, "Usuario", StringComparison.OrdinalIgnoreCase) => "Usuario",
            _ => null
        };
        if (rol is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Solicitud"] = ["El rol debe ser Admin o Usuario."]
            });
        }

        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        // Solo el Admin Principal (CreadoPorId == null) puede crear otros Admin.
        if (adminActual.CreadoPorId is not null && rol == "Admin")
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Solicitud"] = ["Solo el administrador principal puede crear otros administradores."]
            });
        }

        var maxUsuarios = await db.Set<NoteWork.Shared.Entidades.Empresa>()
            .AsNoTracking()
            .Where(e => e.Id == adminActual.EmpresaId)
            .Select(e => e.MaxUsuarios)
            .FirstOrDefaultAsync();

        if (maxUsuarios <= 0) maxUsuarios = 3;

        var activosActuales = await db.Users
            .CountAsync(u => u.EmpresaId == adminActual.EmpresaId && u.Activo);

        if (activosActuales >= maxUsuarios)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Solicitud"] = [$"Has alcanzado el límite de {maxUsuarios} usuarios activos de tu plan."]
            });
        }

        var correo = solicitud.Email.Trim();
        var nuevo = new Usuario
        {
            UserName = correo,
            Email = correo,
            EmailConfirmed = true,
            NombreCompleto = solicitud.NombreCompleto.Trim(),
            EmpresaId = adminActual.EmpresaId,
            CreadoPorId = adminActual.Id
        };

        var resultado = await userManager.CreateAsync(nuevo, solicitud.Password);
        if (!resultado.Succeeded)
        {
            var mensajesEnEspanol = resultado.Errors
                .Select(TraducirErrorIdentity)
                .Distinct()
                .ToArray();

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Identity"] = mensajesEnEspanol
            });
        }

        var resultadoRol = await userManager.AddToRoleAsync(nuevo, rol);
        if (!resultadoRol.Succeeded)
        {
            await userManager.DeleteAsync(nuevo);
            return Results.Problem("No se pudo asignar el rol. No se creó el usuario.");
        }

        return Results.Ok(new
        {
            nuevo.Id,
            nuevo.Email,
            nuevo.NombreCompleto,
            Rol = rol
        });
    }

    internal static string TraducirErrorIdentity(IdentityError error) => error.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "Ya existe un usuario registrado con ese correo.",
        "InvalidEmail" or "InvalidUserName" => "El correo no tiene un formato válido.",
        "PasswordTooShort" => "La contraseña debe tener al menos 8 caracteres.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número (0-9).",
        "PasswordRequiresLower" => "La contraseña debe incluir al menos una letra minúscula (a-z).",
        "PasswordRequiresUpper" => "La contraseña debe incluir al menos una letra mayúscula (A-Z).",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe incluir al menos un símbolo (por ejemplo: !, @, #, *).",
        "PasswordRequiresUniqueChars" => "La contraseña debe tener caracteres más variados.",
        _ => "No se pudo completar la operación. Revisa los datos ingresados."
    };
}