using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NoteWork.Api.Datos;
using NoteWork.Shared.Cuenta;

namespace NoteWork.Api.Endpoints;

// Agrupa los endpoints de cuenta (login, quién soy y crear usuarios).
// Es un "método de extensión": así Program.cs se mantiene corto y ordenado.
public static class CuentaEndpoints
{
    public static IEndpointRouteBuilder MapCuentaEndpoints(this IEndpointRouteBuilder app)
    {
        // Todas las rutas de este grupo empiezan con /api/cuenta
        var grupo = app.MapGroup("/api/cuenta");

        // POST /api/cuenta/login  (AllowAnonymous: aún no hay sesión al entrar)
        grupo.MapPost("/login", LoginAsync).AllowAnonymous();

        // GET /api/cuenta/yo  (NUEVO 4.18)
        // Endpoint de PRUEBA y de uso real: devuelve quién es el usuario del token.
        // RequireAuthorization() sin rol: basta con tener sesión válida.
        // Sin token -> 401. Con token vigente -> 200 con los datos del usuario.
        // Así comprobamos que ManejadorToken sí adjunta el token a las peticiones.
        grupo.MapGet("/yo", YoAsync).RequireAuthorization();

        // POST /api/cuenta/usuarios  (4.14.4)
        // RequireAuthorization + RequireRole("Admin"): solo entra quien tenga
        // sesión iniciada Y el rol Admin. Sin token -> 401; con token pero sin
        // ser Admin -> 403. El control de acceso está AQUÍ, en el servidor,
        // no en lo que muestre u oculte la pantalla del Client.
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
        // Validamos los [Required] y [EmailAddress] también en el servidor:
        // nunca confiamos solo en lo que valida el Client.
        if (string.IsNullOrWhiteSpace(solicitud.Email) || string.IsNullOrWhiteSpace(solicitud.Password))
            return Results.BadRequest("Correo y contraseña son obligatorios.");

        var usuario = await userManager.FindByEmailAsync(solicitud.Email);

        // Si el usuario no existe O la contraseña es incorrecta, damos el MISMO
        // error (401). Así nadie puede descubrir qué correos están registrados.
        if (usuario is null)
            return Results.Unauthorized();

        // lockoutOnFailure: true -> tras varios intentos fallidos, la cuenta se
        // bloquea un rato (protege contra adivinar contraseñas).
        var resultado = await signInManager.CheckPasswordSignInAsync(
            usuario, solicitud.Password, lockoutOnFailure: true);

        if (!resultado.Succeeded)
            return Results.Unauthorized();

        // Construimos la "identidad" del usuario (sus datos y roles) y la
        // metemos en un token firmado con el mismo mecanismo de Identity.
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

    // NUEVO (4.18): responde "¿quién soy yo?" según el token de la petición.
    // Es liviano a propósito: una sola consulta a la base (el usuario) y el rol
    // se lee de los datos que ya vienen dentro del token (sin consulta extra).
    // Nunca devuelve contraseña, hash ni el token.
    private static async Task<IResult> YoAsync(
        ClaimsPrincipal actual,
        UserManager<Usuario> userManager)
    {
        // "actual" es la identidad que ASP.NET sacó del token Bearer.
        var usuario = await userManager.GetUserAsync(actual);

        // Si el token es válido pero el usuario ya no existe (lo borraron),
        // tratamos la sesión como no válida.
        if (usuario is null)
            return Results.Unauthorized();

        return Results.Ok(new
        {
            usuario.Email,
            usuario.NombreCompleto,
            usuario.EmpresaId,
            Rol = actual.FindFirstValue(ClaimTypes.Role) ?? string.Empty
        });
    }

    // (4.14.4) Un admin crea un usuario nuevo dentro de SU empresa.
    // "ClaimsPrincipal admin" lo entrega ASP.NET: es la identidad que viene en
    // el token de la petición (quién está llamando).
    private static async Task<IResult> CrearUsuarioAsync(
        CrearUsuarioSolicitud solicitud,
        ClaimsPrincipal admin,
        UserManager<Usuario> userManager)
    {
        // 1. Validar los datos en el servidor con las mismas reglas
        //    ([Required], [EmailAddress], largos) que escribimos en Shared.
        //    Validator es parte de .NET: no instala nada ni pesa en la Api.
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

        // 2. Buscar al admin que está llamando para sacar SU empresa.
        //    CLAVE DE SEGURIDAD: el EmpresaId sale de la base de datos, nunca de
        //    la solicitud. Así un admin no puede crear usuarios en otra empresa.
        var adminActual = await userManager.GetUserAsync(admin);
        if (adminActual is null)
            return Results.Unauthorized();

        // 3. Armar el usuario nuevo (sin contraseña: Identity guarda solo el hash).
        var correo = solicitud.Email.Trim();
        var nuevo = new Usuario
        {
            UserName = correo,            // inicia sesión con su correo
            Email = correo,
            EmailConfirmed = true,        // aún no enviamos correos; se revisará al hacer "recuperar contraseña"
            NombreCompleto = solicitud.NombreCompleto.Trim(),
            EmpresaId = adminActual.EmpresaId   // la empresa del ADMIN, automático
        };

        // 4. Crear. Identity revisa la complejidad de la contraseña y que el
        //    correo no esté repetido (RequireUniqueEmail), y devuelve los errores.
        //    Traducimos los códigos de Identity al español y usamos Distinct()
        //    para no repetir el mensaje cuando coinciden UserName y Email.
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

        // 5. Asignar SIEMPRE el rol "Usuario" (decisión: el admin no elige rol).
        var resultadoRol = await userManager.AddToRoleAsync(nuevo, "Usuario");
        if (!resultadoRol.Succeeded)
        {
            // Si el rol falla, borramos el usuario para no dejar una cuenta a
            // medias, sin rol. Es mejor que todo salga bien o que nada quede.
            await userManager.DeleteAsync(nuevo);
            return Results.Problem("No se pudo asignar el rol. No se creó el usuario.");
        }

        // 6. Respondemos con datos básicos. NUNCA devolvemos la contraseña.
        return Results.Ok(new
        {
            nuevo.Id,
            nuevo.Email,
            nuevo.NombreCompleto,
            Rol = "Usuario"
        });
    }

    // Traduce los códigos de error más comunes de Identity al español.
    // Es un switch directo en memoria: cero gasto extra de rendimiento.
    private static string TraducirErrorIdentity(IdentityError error) => error.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "Ya existe un usuario registrado con ese correo.",
        "InvalidEmail" or "InvalidUserName" => "El correo no tiene un formato válido.",
        "PasswordTooShort" => "La contraseña debe tener al menos 8 caracteres.",
        "PasswordRequiresDigit" => "La contraseña debe incluir al menos un número (0-9).",
        "PasswordRequiresLower" => "La contraseña debe incluir al menos una letra minúscula (a-z).",
        "PasswordRequiresUpper" => "La contraseña debe incluir al menos una letra mayúscula (A-Z).",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe incluir al menos un símbolo (por ejemplo: !, @, #, *).",
        "PasswordRequiresUniqueChars" => "La contraseña debe tener caracteres más variados.",
        _ => "No se pudo crear el usuario. Revisa los datos ingresados."
    };
}