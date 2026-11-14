using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Entidades;

namespace NoteWork.Api.Endpoints;

// NUEVO (B1): quién está llamando a la Api.
// Guarda su Id, su empresa y si es administrador.
public sealed record ContextoUsuario(Guid UsuarioId, Guid EmpresaId, bool EsAdmin);

public static class ContextoUsuarioExtensiones
{
    // Reemplaza a ObtenerEmpresaIdAsync, que estaba repetido en cada archivo.
    // Hace UNA sola consulta a la base (la empresa del usuario).
    // El rol sale del token, igual que ya hace RequireRole("Admin") en CuentaEndpoints.
    // Devuelve null si el token no es válido o el usuario no existe.
    public static async Task<ContextoUsuario?> ObtenerContextoAsync(
        this AppDbContext db, ClaimsPrincipal user)
    {
        var idTexto = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idTexto, out var usuarioId)) return null;

        var empresaId = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => u.EmpresaId)
            .FirstOrDefaultAsync();

        if (empresaId == Guid.Empty) return null;

        return new ContextoUsuario(usuarioId, empresaId, user.IsInRole("Admin"));
    }

    // LA regla de propiedad vive aquí, en un solo lugar:
    // misma empresa Y (es administrador O es el dueño del proyecto).
    public static IQueryable<Proyecto> VisiblesPara(
        this IQueryable<Proyecto> consulta, ContextoUsuario ctx) =>
        consulta.Where(p => p.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || p.UsuarioId == ctx.UsuarioId));

    // La misma regla para los clientes.
    public static IQueryable<Cliente> VisiblesPara(
        this IQueryable<Cliente> consulta, ContextoUsuario ctx) =>
        consulta.Where(c => c.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || c.UsuarioId == ctx.UsuarioId));
}