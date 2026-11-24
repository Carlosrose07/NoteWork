using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Dtos;

namespace NoteWork.Api.Endpoints;

public static class EmpresaEndpoints
{
    public static void MapEmpresaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/empresa")
            .RequireAuthorization();

        // GET /api/empresa: devuelve los datos de la empresa del usuario logueado
        grupo.MapGet("/", async (AppDbContext db, ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var empresa = await db.Empresas
                .AsNoTracking()
                .Where(e => e.Id == ctx.EmpresaId)
                .Select(e => new EmpresaDto(
                    e.Id,
                    e.Nombre,
                    e.Subtitulo,
                    e.Rnc,
                    e.LogoRuta,
                    e.Telefono,
                    e.Direccion,
                    e.Moneda,
                    e.UnidadPorDefecto))
                .FirstOrDefaultAsync();

            return empresa is null
                ? Results.NotFound("No se encontró la empresa.")
                : Results.Ok(empresa);
        });

        // PUT /api/empresa: actualiza los datos de la empresa (solo Admin)
        grupo.MapPut("/", async (
            GuardarEmpresaDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return Results.BadRequest("El nombre de la empresa es obligatorio.");

            var empresa = await db.Empresas
                .FirstOrDefaultAsync(e => e.Id == ctx.EmpresaId);

            if (empresa is null)
                return Results.NotFound("No se encontró la empresa.");

            empresa.Nombre = dto.Nombre.Trim();
            empresa.Subtitulo = string.IsNullOrWhiteSpace(dto.Subtitulo) ? null : dto.Subtitulo.Trim();
            empresa.Rnc = string.IsNullOrWhiteSpace(dto.Rnc) ? null : dto.Rnc.Trim();
            empresa.LogoRuta = string.IsNullOrWhiteSpace(dto.LogoRuta) ? null : dto.LogoRuta.Trim();
            empresa.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            empresa.Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim();
            empresa.Moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "DOP" : dto.Moneda.Trim().ToUpperInvariant();
            empresa.UnidadPorDefecto = dto.UnidadPorDefecto;

            await db.SaveChangesAsync();

            return Results.Ok(new EmpresaDto(
                empresa.Id,
                empresa.Nombre,
                empresa.Subtitulo,
                empresa.Rnc,
                empresa.LogoRuta,
                empresa.Telefono,
                empresa.Direccion,
                empresa.Moneda,
                empresa.UnidadPorDefecto));
        });
    }
}