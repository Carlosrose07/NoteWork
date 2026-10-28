using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Entidades;
using NoteWork.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Conexión a PostgreSQL: la cadena viene de User Secrets en desarrollo.
var cadenaConexion = builder.Configuration.GetConnectionString("NoteWorkDb")
    ?? throw new InvalidOperationException(
        "No se encontró la cadena de conexión 'NoteWorkDb'. Revisa User Secrets.");

builder.Services.AddDbContext<AppDbContext>(opciones =>
    opciones.UseNpgsql(cadenaConexion));

// --- IDENTITY (usuarios, contraseñas y login) ---
// AddIdentityApiEndpoints prepara todo lo necesario para que la Api pueda
// registrar usuarios e iniciar sesión con "tokens".
builder.Services.AddIdentityApiEndpoints<Usuario>(opciones =>
{
    // Dos usuarios no pueden tener el mismo correo.
    opciones.User.RequireUniqueEmail = true;
})
    // Habilita los roles (ej. "Admin", "Usuario").
    .AddRoles<IdentityRole<Guid>>()
    // Le dice a Identity que guarde usuarios y roles en NUESTRA base.
    .AddEntityFrameworkStores<AppDbContext>();

// --- CORS (4.15.4) ---
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
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// --- DATOS INICIALES (solo en desarrollo) ---
if (app.Environment.IsDevelopment())
{
    using var alcance = app.Services.CreateScope();
    var servicios = alcance.ServiceProvider;
    var db = servicios.GetRequiredService<AppDbContext>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("PermitirClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapCuentaEndpoints();
app.MapClientesEndpoints();
// LÍNEA AGREGADA: Registra los endpoints de Proyectos
app.MapProyectosEndpoints();
// LÍNEA AGREGADA: Registra los endpoints de Nodos
app.MapNodosEndpoints();
// LÍNEA AGREGADA: Registra los endpoints de Partidas
app.MapPartidasEndpoints();

app.MapControllers();

app.Run();