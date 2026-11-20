using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Dtos;
using NoteWork.Shared.Entidades.Universal;

namespace NoteWork.Api.Endpoints;

public static class WorkOrdersEndpoints
{
    public static void MapWorkOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/ordenes")
            .RequireAuthorization();

        // =====================================================================
        // 1. LISTADO PAGINADO DE ÓRDENES (ligero para iPhone 11)
        // =====================================================================
        grupo.MapGet("/", async (
            int? pagina,
            int? tamanoPagina,
            string? buscar,
            WorkOrderStatus? estado,
            WorkOrderType? tipo,
            Guid? proyectoId,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var pag = Math.Max(1, pagina ?? 1);
            var tam = Math.Clamp(tamanoPagina ?? 20, 1, 50);

            var consulta = db.WorkOrders
                .AsNoTracking()
                .VisiblesPara(ctx);

            if (estado.HasValue)
                consulta = consulta.Where(w => w.Status == estado.Value);

            if (tipo.HasValue)
                consulta = consulta.Where(w => w.OrderType == tipo.Value);

            if (proyectoId.HasValue)
                consulta = consulta.Where(w => w.ProjectId == proyectoId.Value);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var termino = buscar.Trim().ToLower();
                consulta = consulta.Where(w =>
                    w.OrderNumber.ToLower().Contains(termino) ||
                    w.CustomerName.ToLower().Contains(termino));
            }

            var total = await consulta.CountAsync();

            var items = await consulta
                .OrderByDescending(w => w.FechaCreacion)
                .Skip((pag - 1) * tam)
                .Take(tam)
                .Select(w => new WorkOrderResumenDto(
                    w.Id,
                    w.UsuarioId,
                    w.OrderNumber,
                    w.OrderType,
                    w.CustomerId,
                    w.ProjectId,
                    w.CustomerName,
                    w.Status,
                    w.TotalQuoteAmount,
                    db.WorkOrderItems.Count(i => i.WorkOrderId == w.Id),
                    w.FechaCreacion))
                .ToListAsync();

            return Results.Ok(new WorkOrdersPaginaDto(items, total, pag, tam));
        });

        // =====================================================================
        // 2. DETALLE DE UNA ORDEN CON TODOS SUS ÍTEMS
        // =====================================================================
        grupo.MapGet("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var orden = await db.WorkOrders
                .AsNoTracking()
                .VisiblesPara(ctx)
                .Where(w => w.Id == id)
                .Select(w => new
                {
                    w.Id,
                    w.UsuarioId,
                    w.OrderNumber,
                    w.OrderType,
                    w.CustomerId,
                    w.ProjectId,
                    w.CustomerName,
                    w.CustomerPhone,
                    w.CustomerAddress,
                    w.Status,
                    w.TaxRatePercent,
                    w.DiscountPercent,
                    w.SubtotalMaterials,
                    w.SubtotalLabor,
                    w.TotalQuoteAmount,
                    w.Notes,
                    w.PricingSnapshotJson,
                    w.FechaCreacion
                })
                .FirstOrDefaultAsync();

            if (orden is null) return Results.NotFound();

            var items = await db.WorkOrderItems
                .AsNoTracking()
                .VisiblesPara(ctx)
                .Where(i => i.WorkOrderId == id)
                .OrderBy(i => i.LineOrder)
                .ThenBy(i => i.FechaCreacion)
                .Select(i => new WorkOrderItemDto(
                    i.Id,
                    i.WorkOrderId,
                    i.AreaNodeId,
                    i.ProductModelId,
                    i.LineOrder,
                    i.Quantity,
                    i.WidthSixteenths,
                    i.HeightSixteenths,
                    i.DepthSixteenths,
                    i.LeafCount,
                    i.DoorCount,
                    i.DrawerCount,
                    i.ShelfCount,
                    i.IsLowTrack,
                    i.BoardThicknessOverrideSixteenths,
                    i.MaterialColor,
                    i.CustomMarginPercent,
                    i.CalculatedUnitPrice,
                    i.CalculatedLineTotal,
                    i.Notes))
                .ToListAsync();

            return Results.Ok(new WorkOrderDetalleDto(
                orden.Id,
                orden.UsuarioId,
                orden.OrderNumber,
                orden.OrderType,
                orden.CustomerId,
                orden.ProjectId,
                orden.CustomerName,
                orden.CustomerPhone,
                orden.CustomerAddress,
                orden.Status,
                orden.TaxRatePercent,
                orden.DiscountPercent,
                orden.SubtotalMaterials,
                orden.SubtotalLabor,
                orden.TotalQuoteAmount,
                orden.Notes,
                orden.PricingSnapshotJson,
                orden.FechaCreacion,
                items));
        });

        // =====================================================================
        // 3. CREAR ORDEN / PRESUPUESTO
        // =====================================================================
        grupo.MapPost("/", async (
            CrearActualizarWorkOrderDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var customerName = dto.CustomerName.Trim();
            if (string.IsNullOrWhiteSpace(customerName))
                return Results.BadRequest("El nombre del cliente es obligatorio.");

            if (dto.CustomerId.HasValue)
            {
                var clienteValido = await db.Clientes
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .AnyAsync(c => c.Id == dto.CustomerId.Value);
                if (!clienteValido) return Results.BadRequest("El cliente indicado no existe o no tienes acceso.");
            }

            if (dto.ProjectId.HasValue)
            {
                var proyectoValido = await db.Proyectos
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .AnyAsync(p => p.Id == dto.ProjectId.Value);
                if (!proyectoValido) return Results.BadRequest("El proyecto indicado no existe o no tienes acceso.");
            }

            var anio = DateTime.UtcNow.Year;
            var prefijo = $"NW-{anio}-";
            var existentesEsteAnio = await db.WorkOrders
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(w => w.EmpresaId == ctx.EmpresaId && w.OrderNumber.StartsWith(prefijo));

            var numeroOrden = $"{prefijo}{(existentesEsteAnio + 1):D4}";

            var entidad = new WorkOrder
            {
                EmpresaId = ctx.EmpresaId,
                UsuarioId = ctx.UsuarioId,
                OrderNumber = numeroOrden,
                OrderType = dto.OrderType,
                CustomerId = dto.CustomerId,
                ProjectId = dto.ProjectId,
                CustomerName = customerName,
                CustomerPhone = string.IsNullOrWhiteSpace(dto.CustomerPhone) ? null : dto.CustomerPhone.Trim(),
                CustomerAddress = string.IsNullOrWhiteSpace(dto.CustomerAddress) ? null : dto.CustomerAddress.Trim(),
                Status = dto.Status,
                TaxRatePercent = Math.Max(0, dto.TaxRatePercent),
                DiscountPercent = Math.Clamp(dto.DiscountPercent, 0, 100),
                SubtotalMaterials = Math.Max(0, dto.SubtotalMaterials),
                SubtotalLabor = Math.Max(0, dto.SubtotalLabor),
                TotalQuoteAmount = Math.Max(0, dto.TotalQuoteAmount),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                PricingSnapshotJson = dto.PricingSnapshotJson
            };

            db.WorkOrders.Add(entidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/ordenes/{entidad.Id}", new WorkOrderDetalleDto(
                entidad.Id,
                entidad.UsuarioId,
                entidad.OrderNumber,
                entidad.OrderType,
                entidad.CustomerId,
                entidad.ProjectId,
                entidad.CustomerName,
                entidad.CustomerPhone,
                entidad.CustomerAddress,
                entidad.Status,
                entidad.TaxRatePercent,
                entidad.DiscountPercent,
                entidad.SubtotalMaterials,
                entidad.SubtotalLabor,
                entidad.TotalQuoteAmount,
                entidad.Notes,
                entidad.PricingSnapshotJson,
                entidad.FechaCreacion,
                []));
        });

        // =====================================================================
        // 4. ACTUALIZAR CABECERA Y TOTALES DE LA ORDEN
        // =====================================================================
        grupo.MapPut("/{id:guid}", async (
            Guid id,
            CrearActualizarWorkOrderDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var entidad = await db.WorkOrders
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(w => w.Id == id);
            if (entidad is null) return Results.NotFound();

            var customerName = dto.CustomerName.Trim();
            if (string.IsNullOrWhiteSpace(customerName))
                return Results.BadRequest("El nombre del cliente es obligatorio.");

            if (dto.CustomerId.HasValue)
            {
                var clienteValido = await db.Clientes
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .AnyAsync(c => c.Id == dto.CustomerId.Value);
                if (!clienteValido) return Results.BadRequest("El cliente indicado no existe o no tienes acceso.");
            }

            if (dto.ProjectId.HasValue)
            {
                var proyectoValido = await db.Proyectos
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .AnyAsync(p => p.Id == dto.ProjectId.Value);
                if (!proyectoValido) return Results.BadRequest("El proyecto indicado no existe o no tienes acceso.");
            }

            entidad.OrderType = dto.OrderType;
            entidad.CustomerId = dto.CustomerId;
            entidad.ProjectId = dto.ProjectId;
            entidad.CustomerName = customerName;
            entidad.CustomerPhone = string.IsNullOrWhiteSpace(dto.CustomerPhone) ? null : dto.CustomerPhone.Trim();
            entidad.CustomerAddress = string.IsNullOrWhiteSpace(dto.CustomerAddress) ? null : dto.CustomerAddress.Trim();
            entidad.Status = dto.Status;
            entidad.TaxRatePercent = Math.Max(0, dto.TaxRatePercent);
            entidad.DiscountPercent = Math.Clamp(dto.DiscountPercent, 0, 100);
            entidad.SubtotalMaterials = Math.Max(0, dto.SubtotalMaterials);
            entidad.SubtotalLabor = Math.Max(0, dto.SubtotalLabor);
            entidad.TotalQuoteAmount = Math.Max(0, dto.TotalQuoteAmount);
            entidad.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            entidad.PricingSnapshotJson = dto.PricingSnapshotJson;
            entidad.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 5. CAMBIAR ESTADO DE LA ORDEN (Borrador -> Cotizado -> En Producción -> Completado)
        // =====================================================================
        grupo.MapPut("/{id:guid}/estado", async (
            Guid id,
            ActualizarEstadoWorkOrderDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var entidad = await db.WorkOrders
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(w => w.Id == id);
            if (entidad is null) return Results.NotFound();

            entidad.Status = dto.Status;
            entidad.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 6. ELIMINAR ORDEN (Borrado lógico de la orden y sus ítems)
        // =====================================================================
        grupo.MapDelete("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var entidad = await db.WorkOrders
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(w => w.Id == id);
            if (entidad is null) return Results.NotFound();

            var ahora = DateTime.UtcNow;
            entidad.Eliminado = true;
            entidad.FechaModificacion = ahora;

            var items = await db.WorkOrderItems
                .Where(i => i.EmpresaId == ctx.EmpresaId && i.WorkOrderId == id)
                .ToListAsync();

            foreach (var item in items)
            {
                item.Eliminado = true;
                item.FechaModificacion = ahora;
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 7. AGREGAR ÍTEM A UNA ORDEN
        // =====================================================================
        grupo.MapPost("/{ordenId:guid}/items", async (
            Guid ordenId,
            CrearActualizarWorkOrderItemDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var ordenExiste = await db.WorkOrders
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(w => w.Id == ordenId);
            if (!ordenExiste) return Results.NotFound("Orden no encontrada.");

            if (dto.Quantity < 1)
                return Results.BadRequest("La cantidad debe ser al menos 1.");
            if (dto.WidthSixteenths <= 0 || dto.HeightSixteenths <= 0)
                return Results.BadRequest("El ancho y el alto deben ser mayores que cero.");

            var modeloValido = await db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == dto.ProductModelId);
            if (!modeloValido) return Results.BadRequest("El modelo seleccionado no existe.");

            if (dto.AreaNodeId.HasValue)
            {
                var nodoValido = await db.Nodos
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .AnyAsync(n => n.Id == dto.AreaNodeId.Value);
                if (!nodoValido) return Results.BadRequest("El área/nodo seleccionado no existe o no tienes acceso.");
            }

            var lineOrder = dto.LineOrder;
            if (lineOrder <= 0)
            {
                var maxOrden = await db.WorkOrderItems
                    .AsNoTracking()
                    .Where(i => i.EmpresaId == ctx.EmpresaId && i.WorkOrderId == ordenId)
                    .Select(i => (int?)i.LineOrder)
                    .MaxAsync() ?? 0;
                lineOrder = maxOrden + 1;
            }

            var item = new WorkOrderItem
            {
                EmpresaId = ctx.EmpresaId,
                WorkOrderId = ordenId,
                AreaNodeId = dto.AreaNodeId,
                ProductModelId = dto.ProductModelId,
                LineOrder = lineOrder,
                Quantity = dto.Quantity,
                WidthSixteenths = dto.WidthSixteenths,
                HeightSixteenths = dto.HeightSixteenths,
                DepthSixteenths = dto.DepthSixteenths,
                LeafCount = Math.Max(0, dto.LeafCount),
                DoorCount = Math.Max(0, dto.DoorCount),
                DrawerCount = Math.Max(0, dto.DrawerCount),
                ShelfCount = Math.Max(0, dto.ShelfCount),
                IsLowTrack = dto.IsLowTrack,
                BoardThicknessOverrideSixteenths = dto.BoardThicknessOverrideSixteenths,
                MaterialColor = string.IsNullOrWhiteSpace(dto.MaterialColor) ? null : dto.MaterialColor.Trim(),
                CustomMarginPercent = dto.CustomMarginPercent,
                CalculatedUnitPrice = Math.Max(0, dto.CalculatedUnitPrice),
                CalculatedLineTotal = Math.Max(0, dto.CalculatedLineTotal),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
            };

            db.WorkOrderItems.Add(item);
            await db.SaveChangesAsync();

            return Results.Created($"/api/ordenes/{ordenId}/items/{item.Id}", new WorkOrderItemDto(
                item.Id,
                item.WorkOrderId,
                item.AreaNodeId,
                item.ProductModelId,
                item.LineOrder,
                item.Quantity,
                item.WidthSixteenths,
                item.HeightSixteenths,
                item.DepthSixteenths,
                item.LeafCount,
                item.DoorCount,
                item.DrawerCount,
                item.ShelfCount,
                item.IsLowTrack,
                item.BoardThicknessOverrideSixteenths,
                item.MaterialColor,
                item.CustomMarginPercent,
                item.CalculatedUnitPrice,
                item.CalculatedLineTotal,
                item.Notes));
        });

        // =====================================================================
        // 8. ACTUALIZAR UN ÍTEM DE LA ORDEN
        // =====================================================================
        grupo.MapPut("/{ordenId:guid}/items/{itemId:guid}", async (
            Guid ordenId,
            Guid itemId,
            CrearActualizarWorkOrderItemDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var item = await db.WorkOrderItems
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.WorkOrderId == ordenId);
            if (item is null) return Results.NotFound();

            if (dto.Quantity < 1)
                return Results.BadRequest("La cantidad debe ser al menos 1.");
            if (dto.WidthSixteenths <= 0 || dto.HeightSixteenths <= 0)
                return Results.BadRequest("El ancho y el alto deben ser mayores que cero.");

            var modeloValido = await db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == dto.ProductModelId);
            if (!modeloValido) return Results.BadRequest("El modelo seleccionado no existe.");

            if (dto.AreaNodeId.HasValue)
            {
                var nodoValido = await db.Nodos
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .AnyAsync(n => n.Id == dto.AreaNodeId.Value);
                if (!nodoValido) return Results.BadRequest("El área/nodo seleccionado no existe o no tienes acceso.");
            }

            item.AreaNodeId = dto.AreaNodeId;
            item.ProductModelId = dto.ProductModelId;
            item.LineOrder = dto.LineOrder > 0 ? dto.LineOrder : item.LineOrder;
            item.Quantity = dto.Quantity;
            item.WidthSixteenths = dto.WidthSixteenths;
            item.HeightSixteenths = dto.HeightSixteenths;
            item.DepthSixteenths = dto.DepthSixteenths;
            item.LeafCount = Math.Max(0, dto.LeafCount);
            item.DoorCount = Math.Max(0, dto.DoorCount);
            item.DrawerCount = Math.Max(0, dto.DrawerCount);
            item.ShelfCount = Math.Max(0, dto.ShelfCount);
            item.IsLowTrack = dto.IsLowTrack;
            item.BoardThicknessOverrideSixteenths = dto.BoardThicknessOverrideSixteenths;
            item.MaterialColor = string.IsNullOrWhiteSpace(dto.MaterialColor) ? null : dto.MaterialColor.Trim();
            item.CustomMarginPercent = dto.CustomMarginPercent;
            item.CalculatedUnitPrice = Math.Max(0, dto.CalculatedUnitPrice);
            item.CalculatedLineTotal = Math.Max(0, dto.CalculatedLineTotal);
            item.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            item.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 9. ELIMINAR UN ÍTEM DE LA ORDEN (Borrado lógico)
        // =====================================================================
        grupo.MapDelete("/{ordenId:guid}/items/{itemId:guid}", async (
            Guid ordenId,
            Guid itemId,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var item = await db.WorkOrderItems
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.WorkOrderId == ordenId);
            if (item is null) return Results.NotFound();

            item.Eliminado = true;
            item.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}