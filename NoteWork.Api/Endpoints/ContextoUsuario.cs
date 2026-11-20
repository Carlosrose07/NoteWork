using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Entidades;
using NoteWork.Shared.Entidades.Universal;

namespace NoteWork.Api.Endpoints;

// Quién está llamando a la Api: su Id, su empresa y si es administrador.
public sealed record ContextoUsuario(Guid UsuarioId, Guid EmpresaId, bool EsAdmin);

public static class ContextoUsuarioExtensiones
{
    // Hace UNA sola consulta a la base (la empresa del usuario).
    // El rol sale del token, igual que RequireRole("Admin") en CuentaEndpoints.
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

    // --- ENTIDADES ORIGINALES ---

    // Misma empresa Y (es administrador O es el dueño del proyecto).
    public static IQueryable<Proyecto> VisiblesPara(
        this IQueryable<Proyecto> consulta, ContextoUsuario ctx) =>
        consulta.Where(p => p.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || p.UsuarioId == ctx.UsuarioId));

    // La misma regla para los clientes.
    public static IQueryable<Cliente> VisiblesPara(
        this IQueryable<Cliente> consulta, ContextoUsuario ctx) =>
        consulta.Where(c => c.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || c.UsuarioId == ctx.UsuarioId));

    // La misma regla para los nodos (heredan el dueño de su proyecto).
    public static IQueryable<Nodo> VisiblesPara(
        this IQueryable<Nodo> consulta, ContextoUsuario ctx) =>
        consulta.Where(n => n.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || n.Proyecto!.UsuarioId == ctx.UsuarioId));

    // La misma regla para las partidas (heredan el dueño a través de partida -> nodo -> proyecto).
    public static IQueryable<Partida> VisiblesPara(
        this IQueryable<Partida> consulta, ContextoUsuario ctx) =>
        consulta.Where(p => p.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || p.Nodo!.Proyecto!.UsuarioId == ctx.UsuarioId));

    // --- ENTIDADES DEL MOTOR UNIVERSAL ---

    // Catálogo compartido por empresa (lectura para todos; escritura controlada por Admin en endpoints).
    public static IQueryable<ProductCategory> VisiblesPara(
        this IQueryable<ProductCategory> consulta, ContextoUsuario ctx) =>
        consulta.Where(c => c.EmpresaId == ctx.EmpresaId);

    public static IQueryable<MaterialCatalogItem> VisiblesPara(
        this IQueryable<MaterialCatalogItem> consulta, ContextoUsuario ctx) =>
        consulta.Where(m => m.EmpresaId == ctx.EmpresaId);

    public static IQueryable<ProductModel> VisiblesPara(
        this IQueryable<ProductModel> consulta, ContextoUsuario ctx) =>
        consulta.Where(m => m.EmpresaId == ctx.EmpresaId);

    public static IQueryable<ProductCutRule> VisiblesPara(
        this IQueryable<ProductCutRule> consulta, ContextoUsuario ctx) =>
        consulta.Where(r => r.EmpresaId == ctx.EmpresaId);

    public static IQueryable<ProductHardwareRule> VisiblesPara(
        this IQueryable<ProductHardwareRule> consulta, ContextoUsuario ctx) =>
        consulta.Where(r => r.EmpresaId == ctx.EmpresaId);

    // Órdenes de trabajo / presupuestos: usuario normal solo ve las suyas; Admin ve toda su empresa.
    public static IQueryable<WorkOrder> VisiblesPara(
        this IQueryable<WorkOrder> consulta, ContextoUsuario ctx) =>
        consulta.Where(w => w.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || w.UsuarioId == ctx.UsuarioId));

    // Ítems de orden: heredan la propiedad desde su WorkOrder padre.
    public static IQueryable<WorkOrderItem> VisiblesPara(
        this IQueryable<WorkOrderItem> consulta, ContextoUsuario ctx) =>
        consulta.Where(i => i.EmpresaId == ctx.EmpresaId
                         && (ctx.EsAdmin || i.WorkOrder!.UsuarioId == ctx.UsuarioId));
}