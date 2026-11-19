using System.Security.Claims;                          // CAMBIO (Bloque B): para leer el Id del usuario del token
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;             // CAMBIO (Bloque B): memoria interna de .NET (IMemoryCache)
using NoteWork.Api.Datos;
using NoteWork.Shared.Entidades;
using NoteWork.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Conexión a PostgreSQL: la cadena viene de User Secrets en desarrollo
// y de la variable de entorno ConnectionStrings__NoteWorkDb en el servidor (Render).
var cadenaConexion = builder.Configuration.GetConnectionString("NoteWorkDb")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'NoteWorkDb'. Revisa User Secrets o las variables de entorno.");

builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseNpgsql(cadenaConexion));

// CAMBIO (Bloque B): memoria interna para recordar unos segundos que un usuario
// está activo (ver el filtro más abajo). Viene incluida en .NET: no instala nada.
builder.Services.AddMemoryCache();

// --- IDENTITY (usuarios, contraseñas y login) ---
builder.Services.AddIdentityApiEndpoints<Usuario>(opciones =>
{
    // Dos usuarios no pueden tener el mismo correo.
    opciones.User.RequireUniqueEmail = true;
})
    // Habilita los roles (ej. "Admin", "Usuario").
    .AddRoles<IdentityRole<Guid>>()
    // Le dice a Identity que guarde usuarios y roles en NUESTRA base.
    .AddEntityFrameworkStores<AppDbContext>();

// --- CORS ---
// Las direcciones permitidas salen de la configuración "Cors:OrigenesPermitidos".
// En el servidor se define con la variable Cors__OrigenesPermitidos__0 (y __1, etc.).
// Si no hay configuración, se usan las direcciones de tu PC.
var origenesPermitidos =
    builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>()
    ?? ["https://localhost:7045", "http://localhost:5209"];

builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy("PermitirClient", politica =>
    {
        // SOLO estas direcciones pueden llamar a la Api.
        politica.WithOrigins(origenesPermitidos)
                .AllowAnyHeader()
                .WithMethods("GET", "POST", "PUT", "DELETE");
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// --- BASE DE DATOS Y DATOS INICIALES (en desarrollo y en producción) ---
{
    using var alcance = app.Services.CreateScope();
    var servicios = alcance.ServiceProvider;
    var db = servicios.GetRequiredService<AppDbContext>();

    // --- 0. MIGRACIONES AUTOMÁTICAS ---
    // Crea o actualiza las tablas al arrancar (en el servidor nadie corre Update-Database).
    await db.Database.MigrateAsync();

    // --- 1. LA EMPRESA ---
    var empresa = await db.Empresas
        .IgnoreQueryFilters()
        .OrderBy(e => e.FechaCreacion)
        .FirstOrDefaultAsync();

    if (empresa is null)
    {
        empresa = new Empresa { Nombre = "Mi Empresa" };
        empresa.EmpresaId = empresa.Id;
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();
    }

    // --- 2. LOS ROLES ---
    var roles = servicios.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    string[] rolesNecesarios = ["Admin", "Usuario"];

    foreach (var rol in rolesNecesarios)
    {
        if (!await roles.RoleExistsAsync(rol))
        {
            await roles.CreateAsync(new IdentityRole<Guid>(rol));
        }
    }

    // --- 3. EL PRIMER USUARIO ---
    // Solo se crea si NO hay ningún usuario y están las 3 configuraciones.
    // Los valores NUNCA van escritos aquí: vienen de User Secrets o de variables de entorno.
    var email = app.Configuration["PrimerUsuario:Email"];
    var nombre = app.Configuration["PrimerUsuario:NombreCompleto"];
    var clave = app.Configuration["PrimerUsuario:Password"];

    if (!string.IsNullOrWhiteSpace(email)
        && !string.IsNullOrWhiteSpace(nombre)
        && !string.IsNullOrWhiteSpace(clave)
        && !await db.Users.AnyAsync())
    {
        var usuarios = servicios.GetRequiredService<UserManager<Usuario>>();

        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = nombre,
            EmpresaId = empresa.Id
        };

        var resultado = await usuarios.CreateAsync(usuario, clave);

        if (!resultado.Succeeded)
        {
            var errores = string.Join(" | ", resultado.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"No se pudo crear el primer usuario: {errores}");
        }

        await usuarios.AddToRoleAsync(usuario, "Admin");
    }
}
// --- FIN DE DATOS INICIALES ---

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // La redirección a HTTPS solo se usa en tu PC.
    // En Render el HTTPS lo maneja Render por fuera y la Api escucha en HTTP (puerto 8080).
    app.UseHttpsRedirection();
}

app.UseCors("PermitirClient");

app.UseAuthentication();

// --- CAMBIO (Bloque B): FILTRO "USUARIO ACTIVO" ---
// Va justo DESPUÉS de UseAuthentication (ya sabemos quién llama) y ANTES de
// UseAuthorization (antes de decidir qué puede hacer). Así una cuenta desactivada
// queda fuera de TODOS los endpoints protegidos, no solo del login.
//
// Por qué hace falta: el token de sesión sigue siendo válido hasta que vence, y el
// login solo se revisa al entrar. Sin este filtro, alguien desactivado que ya tenía
// la app abierta seguiría trabajando.
//
// Rendimiento (Neon y Render gratis): consultar la base en CADA petición sería caro.
// Por eso, si el usuario está activo, lo recordamos 15 segundos en memoria.
//   - Un usuario desactivado puede seguir hasta 15 segundos; luego recibe 401.
//   - Solo guardamos en memoria el resultado "activo". Si está desactivado NO se
//     guarda, así al reactivarlo puede entrar de inmediato.
app.Use(async (contexto, siguiente) =>
{
    // Las rutas sin sesión (como /salud o el login) pasan sin revisar nada.
    if (contexto.User.Identity?.IsAuthenticated == true)
    {
        var idTexto = contexto.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idTexto, out var usuarioId))
        {
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var memoria = contexto.RequestServices.GetRequiredService<IMemoryCache>();
        var claveMemoria = $"usuario-activo:{usuarioId}";

        if (!memoria.TryGetValue(claveMemoria, out _))
        {
            var db = contexto.RequestServices.GetRequiredService<AppDbContext>();

            // Una sola columna: (bool?) devuelve null si el usuario ya no existe.
            var activo = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => (bool?)u.Activo)
                .FirstOrDefaultAsync(contexto.RequestAborted);

            // Desactivado o inexistente -> 401, igual que una sesión no válida.
            if (activo != true)
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            memoria.Set(claveMemoria, true, TimeSpan.FromSeconds(15));
        }
    }

    await siguiente();
});
// --- FIN DEL FILTRO ---

app.UseAuthorization();

// Dirección de comprobación: devuelve "ok" si la Api está despierta.
app.MapGet("/salud", () => Results.Ok("ok"));

app.MapCuentaEndpoints();
app.MapUsuariosEndpoints();   // CAMBIO (Bloque B): gestión de usuarios (solo Admin)
app.MapClientesEndpoints();
app.MapProyectosEndpoints();
app.MapNodosEndpoints();
app.MapPartidasEndpoints();

app.MapControllers();

app.Run();