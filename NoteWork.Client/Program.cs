using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NoteWork.Client;
using NoteWork.Client.Servicios;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- DIRECCIÓN DE LA API (4.15.5) ---
// Se lee de wwwroot/appsettings.json (clave "ApiUrl"). Así, al publicar, solo
// se cambia ese archivo y no hay que recompilar. Este archivo viaja al
// navegador: NUNCA lleva contraseñas ni claves.
var urlApi = builder.Configuration["ApiUrl"]
    ?? throw new InvalidOperationException(
        "Falta 'ApiUrl' en wwwroot/appsettings.json.");

// El HttpClient apunta a la Api y pasa por ManejadorToken (4.17): cada petición
// sale con el token si hay sesión vigente.
// Sigue siendo UN solo HttpClient compartido (AddScoped), liviano para celulares.
builder.Services.AddScoped(sp =>
{
    var manejador = new ManejadorToken(sp.GetRequiredService<IJSRuntime>(), sp)
    {
        InnerHandler = new HttpClientHandler()
    };

    return new HttpClient(manejador) { BaseAddress = new Uri(urlApi) };
});

// --- SERVICIO DE SESIÓN (4.15.8) ---
// AddScoped = se crea UNA vez y se comparte mientras la app esté abierta.
builder.Services.AddScoped<ServicioSesion>();

// --- SERVICIOS DE DATOS ---
// Mismo patrón: una instancia compartida, con el HttpClient que ya trae el token.
builder.Services.AddScoped<ServicioUsuarios>();
builder.Services.AddScoped<ServicioClientes>();
builder.Services.AddScoped<ServicioProyectos>();
builder.Services.AddScoped<ServicioNodos>();
builder.Services.AddScoped<ServicioPartidas>();
builder.Services.AddScoped<ServicioCatalogoUniversal>();
builder.Services.AddScoped<ServicioOrdenes>();

// --- AUTENTICACIÓN DE PÁGINAS (4.16) ---
// AddAuthorizationCore activa [Authorize] en su versión liviana para WASM.
builder.Services.AddScoped<ProveedorAutenticacion>();
builder.Services.AddAuthorizationCore();

// Una sola instancia de ProveedorAutenticacion, publicada también como
// AuthenticationStateProvider (el nombre que Blazor busca).
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<ProveedorAutenticacion>());

await builder.Build().RunAsync();