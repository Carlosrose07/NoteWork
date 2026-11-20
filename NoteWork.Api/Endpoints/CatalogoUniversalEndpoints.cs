using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NoteWork.Api.Datos;
using NoteWork.Shared.Dtos;
using NoteWork.Shared.Entidades.Universal;
using NoteWork.Shared.Motor;

namespace NoteWork.Api.Endpoints;

public static class CatalogoUniversalEndpoints
{
    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void MapCatalogoUniversalEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/catalogo-universal")
            .RequireAuthorization();

        // =====================================================================
        // 1. PAQUETE COMPLETO PARA BLAZOR WASM (1 sola llamada HTTP)
        // =====================================================================
        grupo.MapGet("/paquete", async (
            bool? incluirInactivos,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();

            var soloActivos = incluirInactivos != true;

            var categorias = await db.ProductCategories
                .AsNoTracking()
                .VisiblesPara(ctx)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new ProductCategoryDto(
                    c.Id, c.Code, c.Name,
                    c.UsesDepth, c.UsesLeaves, c.UsesDoors,
                    c.UsesDrawers, c.UsesShelves, c.UsesLowTrack,
                    c.DefaultUnit, c.SortOrder))
                .ToListAsync();

            var consultaMateriales = db.MaterialCatalog
                .AsNoTracking()
                .VisiblesPara(ctx);

            if (soloActivos)
                consultaMateriales = consultaMateriales.Where(m => m.IsActive);

            var materiales = await consultaMateriales
                .OrderBy(m => m.MaterialType)
                .ThenBy(m => m.Name)
                .Select(m => new MaterialCatalogDto(
                    m.Id, m.Code, m.Name, m.MaterialType,
                    m.CommercialLengthInches, m.CommercialWidthInches, m.CommercialHeightInches,
                    m.UnitCost, m.DefaultWastePercent, m.ColorOptions, m.IsActive))
                .ToListAsync();

            var consultaModelos = db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx);

            if (soloActivos)
                consultaModelos = consultaModelos.Where(m => m.IsActive);

            var modelosCrudos = await consultaModelos
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.Name)
                .Select(m => new
                {
                    m.Id,
                    m.CategoryId,
                    m.Code,
                    m.Name,
                    m.DefaultBoardThicknessSixteenths,
                    m.DefaultLaborMode,
                    m.DefaultLaborValue,
                    m.DefaultMarginPercent,
                    m.SortOrder,
                    m.IsActive
                })
                .ToListAsync();

            var modeloIds = modelosCrudos.Select(m => m.Id).ToList();

            var reglasCorteCrudas = modeloIds.Count == 0
                ? []
                : await db.ProductCutRules
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .Where(r => modeloIds.Contains(r.ProductModelId))
                    .OrderBy(r => r.SortOrder)
                    .ThenBy(r => r.PartName)
                    .Select(r => new
                    {
                        r.Id,
                        r.ProductModelId,
                        r.PartName,
                        r.MaterialId,
                        r.MeasureMode,
                        r.Dim1Axis,
                        r.Dim1DivisorSource,
                        r.Dim1CustomDivisor,
                        r.Dim1OffsetSixteenths,
                        r.Dim1ThicknessMultiplier,
                        r.Dim2Axis,
                        r.Dim2DivisorSource,
                        r.Dim2CustomDivisor,
                        r.Dim2OffsetSixteenths,
                        r.Dim2ThicknessMultiplier,
                        r.QuantityMode,
                        r.QuantityFactor,
                        r.ConditionJson,
                        r.SortOrder
                    })
                    .ToListAsync();

            var reglasHerrajesCrudas = modeloIds.Count == 0
                ? []
                : await db.ProductHardwareRules
                    .AsNoTracking()
                    .VisiblesPara(ctx)
                    .Where(r => modeloIds.Contains(r.ProductModelId))
                    .OrderBy(r => r.SortOrder)
                    .Select(r => new
                    {
                        r.Id,
                        r.ProductModelId,
                        r.MaterialId,
                        r.CalculationBasis,
                        r.Factor,
                        r.ConditionJson,
                        r.SortOrder
                    })
                    .ToListAsync();

            var cortesPorModelo = reglasCorteCrudas
                .GroupBy(r => r.ProductModelId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<CutRuleDto>)g.Select(r => new CutRuleDto(
                        r.Id, r.ProductModelId, r.PartName, r.MaterialId, r.MeasureMode,
                        r.Dim1Axis, r.Dim1DivisorSource, r.Dim1CustomDivisor,
                        r.Dim1OffsetSixteenths, r.Dim1ThicknessMultiplier,
                        r.Dim2Axis, r.Dim2DivisorSource, r.Dim2CustomDivisor,
                        r.Dim2OffsetSixteenths, r.Dim2ThicknessMultiplier,
                        r.QuantityMode, r.QuantityFactor,
                        DeserializarCondicion(r.ConditionJson),
                        r.SortOrder)).ToList());

            var herrajesPorModelo = reglasHerrajesCrudas
                .GroupBy(r => r.ProductModelId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<HardwareRuleDto>)g.Select(r => new HardwareRuleDto(
                        r.Id, r.ProductModelId, r.MaterialId,
                        r.CalculationBasis, r.Factor,
                        DeserializarCondicion(r.ConditionJson),
                        r.SortOrder)).ToList());

            var modelos = modelosCrudos.Select(m => new ProductModelDto(
                m.Id, m.CategoryId, m.Code, m.Name,
                m.DefaultBoardThicknessSixteenths,
                m.DefaultLaborMode, m.DefaultLaborValue, m.DefaultMarginPercent,
                m.SortOrder, m.IsActive,
                cortesPorModelo.TryGetValue(m.Id, out var c) ? c : [],
                herrajesPorModelo.TryGetValue(m.Id, out var h) ? h : []))
                .ToList();

            return Results.Ok(new CatalogoUniversalPaqueteDto(categorias, materiales, modelos));
        });

        // =====================================================================
        // 2. CATEGORÍAS (Solo Admin crea/edita/elimina)
        // =====================================================================
        grupo.MapPost("/categorias", async (
            GuardarProductCategoryDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var code = dto.Code.Trim().ToUpperInvariant();
            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("Código y nombre de categoría son obligatorios.");

            var existe = await db.ProductCategories
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(c => c.Code == code);
            if (existe) return Results.Conflict($"Ya existe una categoría con código '{code}'.");

            var entidad = new ProductCategory
            {
                EmpresaId = ctx.EmpresaId,
                Code = code,
                Name = name,
                UsesDepth = dto.UsesDepth,
                UsesLeaves = dto.UsesLeaves,
                UsesDoors = dto.UsesDoors,
                UsesDrawers = dto.UsesDrawers,
                UsesShelves = dto.UsesShelves,
                UsesLowTrack = dto.UsesLowTrack,
                DefaultUnit = string.IsNullOrWhiteSpace(dto.DefaultUnit) ? "in" : dto.DefaultUnit.Trim(),
                SortOrder = dto.SortOrder
            };

            db.ProductCategories.Add(entidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogo-universal/categorias/{entidad.Id}",
                new ProductCategoryDto(
                    entidad.Id, entidad.Code, entidad.Name,
                    entidad.UsesDepth, entidad.UsesLeaves, entidad.UsesDoors,
                    entidad.UsesDrawers, entidad.UsesShelves, entidad.UsesLowTrack,
                    entidad.DefaultUnit, entidad.SortOrder));
        });

        grupo.MapPut("/categorias/{id:guid}", async (
            Guid id,
            GuardarProductCategoryDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductCategories
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (entidad is null) return Results.NotFound();

            var code = dto.Code.Trim().ToUpperInvariant();
            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("Código y nombre de categoría son obligatorios.");

            var duplicado = await db.ProductCategories
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(c => c.Id != id && c.Code == code);
            if (duplicado) return Results.Conflict($"Ya existe otra categoría con código '{code}'.");

            entidad.Code = code;
            entidad.Name = name;
            entidad.UsesDepth = dto.UsesDepth;
            entidad.UsesLeaves = dto.UsesLeaves;
            entidad.UsesDoors = dto.UsesDoors;
            entidad.UsesDrawers = dto.UsesDrawers;
            entidad.UsesShelves = dto.UsesShelves;
            entidad.UsesLowTrack = dto.UsesLowTrack;
            entidad.DefaultUnit = string.IsNullOrWhiteSpace(dto.DefaultUnit) ? "in" : dto.DefaultUnit.Trim();
            entidad.SortOrder = dto.SortOrder;
            entidad.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        grupo.MapDelete("/categorias/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductCategories
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (entidad is null) return Results.NotFound();

            var tieneModelos = await db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.CategoryId == id);
            if (tieneModelos)
                return Results.Conflict("No se puede eliminar la categoría porque tiene modelos asociados.");

            entidad.Eliminado = true;
            entidad.FechaModificacion = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 3. MATERIALES DEL CATÁLOGO (Solo Admin crea/edita/elimina)
        // =====================================================================
        grupo.MapPost("/materiales", async (
            GuardarMaterialCatalogDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var code = dto.Code.Trim().ToUpperInvariant();
            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("Código y nombre del material son obligatorios.");
            if (dto.UnitCost < 0 || dto.DefaultWastePercent < 0)
                return Results.BadRequest("Costo y desperdicio no pueden ser negativos.");

            var existe = await db.MaterialCatalog
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Code == code);
            if (existe) return Results.Conflict($"Ya existe un material con código '{code}'.");

            var entidad = new MaterialCatalogItem
            {
                EmpresaId = ctx.EmpresaId,
                Code = code,
                Name = name,
                MaterialType = dto.MaterialType,
                CommercialLengthInches = dto.CommercialLengthInches,
                CommercialWidthInches = dto.CommercialWidthInches,
                CommercialHeightInches = dto.CommercialHeightInches,
                UnitCost = dto.UnitCost,
                DefaultWastePercent = dto.DefaultWastePercent,
                ColorOptions = string.IsNullOrWhiteSpace(dto.ColorOptions) ? null : dto.ColorOptions.Trim(),
                IsActive = dto.IsActive
            };

            db.MaterialCatalog.Add(entidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogo-universal/materiales/{entidad.Id}",
                new MaterialCatalogDto(
                    entidad.Id, entidad.Code, entidad.Name, entidad.MaterialType,
                    entidad.CommercialLengthInches, entidad.CommercialWidthInches,
                    entidad.CommercialHeightInches, entidad.UnitCost,
                    entidad.DefaultWastePercent, entidad.ColorOptions, entidad.IsActive));
        });

        grupo.MapPut("/materiales/{id:guid}", async (
            Guid id,
            GuardarMaterialCatalogDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.MaterialCatalog
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entidad is null) return Results.NotFound();

            var code = dto.Code.Trim().ToUpperInvariant();
            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("Código y nombre del material son obligatorios.");
            if (dto.UnitCost < 0 || dto.DefaultWastePercent < 0)
                return Results.BadRequest("Costo y desperdicio no pueden ser negativos.");

            var duplicado = await db.MaterialCatalog
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id != id && m.Code == code);
            if (duplicado) return Results.Conflict($"Ya existe otro material con código '{code}'.");

            entidad.Code = code;
            entidad.Name = name;
            entidad.MaterialType = dto.MaterialType;
            entidad.CommercialLengthInches = dto.CommercialLengthInches;
            entidad.CommercialWidthInches = dto.CommercialWidthInches;
            entidad.CommercialHeightInches = dto.CommercialHeightInches;
            entidad.UnitCost = dto.UnitCost;
            entidad.DefaultWastePercent = dto.DefaultWastePercent;
            entidad.ColorOptions = string.IsNullOrWhiteSpace(dto.ColorOptions) ? null : dto.ColorOptions.Trim();
            entidad.IsActive = dto.IsActive;
            entidad.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        grupo.MapDelete("/materiales/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.MaterialCatalog
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entidad is null) return Results.NotFound();

            var usadoEnCorte = await db.ProductCutRules
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(r => r.MaterialId == id);
            var usadoEnHerraje = await db.ProductHardwareRules
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(r => r.MaterialId == id);

            if (usadoEnCorte || usadoEnHerraje)
                return Results.Conflict("El material está en uso en reglas de modelos. Desactívalo en lugar de eliminarlo.");

            entidad.Eliminado = true;
            entidad.FechaModificacion = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 4. MODELOS FABRICABLES (Solo Admin crea/edita/elimina)
        // =====================================================================
        grupo.MapPost("/modelos", async (
            GuardarProductModelDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var code = dto.Code.Trim().ToUpperInvariant();
            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("Código y nombre del modelo son obligatorios.");

            var categoriaExiste = await db.ProductCategories
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoriaExiste) return Results.BadRequest("La categoría indicada no existe.");

            var duplicado = await db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Code == code);
            if (duplicado) return Results.Conflict($"Ya existe un modelo con código '{code}'.");

            var entidad = new ProductModel
            {
                EmpresaId = ctx.EmpresaId,
                CategoryId = dto.CategoryId,
                Code = code,
                Name = name,
                DefaultBoardThicknessSixteenths = Math.Max(0, dto.DefaultBoardThicknessSixteenths),
                DefaultLaborMode = dto.DefaultLaborMode,
                DefaultLaborValue = Math.Max(0, dto.DefaultLaborValue),
                DefaultMarginPercent = Math.Max(0, dto.DefaultMarginPercent),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };

            db.ProductModels.Add(entidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogo-universal/modelos/{entidad.Id}",
                new ProductModelDto(
                    entidad.Id, entidad.CategoryId, entidad.Code, entidad.Name,
                    entidad.DefaultBoardThicknessSixteenths, entidad.DefaultLaborMode,
                    entidad.DefaultLaborValue, entidad.DefaultMarginPercent,
                    entidad.SortOrder, entidad.IsActive, [], []));
        });

        grupo.MapPut("/modelos/{id:guid}", async (
            Guid id,
            GuardarProductModelDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductModels
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entidad is null) return Results.NotFound();

            var code = dto.Code.Trim().ToUpperInvariant();
            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("Código y nombre del modelo son obligatorios.");

            var categoriaExiste = await db.ProductCategories
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoriaExiste) return Results.BadRequest("La categoría indicada no existe.");

            var duplicado = await db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id != id && m.Code == code);
            if (duplicado) return Results.Conflict($"Ya existe otro modelo con código '{code}'.");

            entidad.CategoryId = dto.CategoryId;
            entidad.Code = code;
            entidad.Name = name;
            entidad.DefaultBoardThicknessSixteenths = Math.Max(0, dto.DefaultBoardThicknessSixteenths);
            entidad.DefaultLaborMode = dto.DefaultLaborMode;
            entidad.DefaultLaborValue = Math.Max(0, dto.DefaultLaborValue);
            entidad.DefaultMarginPercent = Math.Max(0, dto.DefaultMarginPercent);
            entidad.SortOrder = dto.SortOrder;
            entidad.IsActive = dto.IsActive;
            entidad.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        grupo.MapDelete("/modelos/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductModels
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entidad is null) return Results.NotFound();

            var usadoEnOrdenes = await db.WorkOrderItems
                .AsNoTracking()
                .AnyAsync(i => i.EmpresaId == ctx.EmpresaId && i.ProductModelId == id);
            if (usadoEnOrdenes)
                return Results.Conflict("El modelo ya se usó en órdenes. Desactívalo en lugar de eliminarlo.");

            entidad.Eliminado = true;
            entidad.FechaModificacion = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 5. REGLAS DE CORTE DE UN MODELO (Solo Admin)
        // =====================================================================
        grupo.MapPost("/modelos/{modeloId:guid}/reglas-corte", async (
            Guid modeloId,
            GuardarCutRuleDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(dto.PartName))
                return Results.BadRequest("El nombre de la pieza es obligatorio.");

            var modeloExiste = await db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == modeloId);
            if (!modeloExiste) return Results.NotFound("Modelo no encontrado.");

            var materialExiste = await db.MaterialCatalog
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == dto.MaterialId);
            if (!materialExiste) return Results.BadRequest("El material indicado no existe.");

            var entidad = new ProductCutRule
            {
                EmpresaId = ctx.EmpresaId,
                ProductModelId = modeloId,
                PartName = dto.PartName.Trim(),
                MaterialId = dto.MaterialId,
                MeasureMode = dto.MeasureMode,
                Dim1Axis = dto.Dim1Axis,
                Dim1DivisorSource = dto.Dim1DivisorSource,
                Dim1CustomDivisor = dto.Dim1CustomDivisor <= 0 ? 1m : dto.Dim1CustomDivisor,
                Dim1OffsetSixteenths = dto.Dim1OffsetSixteenths,
                Dim1ThicknessMultiplier = dto.Dim1ThicknessMultiplier,
                Dim2Axis = dto.Dim2Axis,
                Dim2DivisorSource = dto.Dim2DivisorSource,
                Dim2CustomDivisor = dto.Dim2CustomDivisor <= 0 ? 1m : dto.Dim2CustomDivisor,
                Dim2OffsetSixteenths = dto.Dim2OffsetSixteenths,
                Dim2ThicknessMultiplier = dto.Dim2ThicknessMultiplier,
                QuantityMode = dto.QuantityMode,
                QuantityFactor = Math.Max(1, dto.QuantityFactor),
                ConditionJson = SerializarCondicion(dto.Condition),
                SortOrder = dto.SortOrder
            };

            db.ProductCutRules.Add(entidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogo-universal/reglas-corte/{entidad.Id}",
                new CutRuleDto(
                    entidad.Id, entidad.ProductModelId, entidad.PartName, entidad.MaterialId,
                    entidad.MeasureMode,
                    entidad.Dim1Axis, entidad.Dim1DivisorSource, entidad.Dim1CustomDivisor,
                    entidad.Dim1OffsetSixteenths, entidad.Dim1ThicknessMultiplier,
                    entidad.Dim2Axis, entidad.Dim2DivisorSource, entidad.Dim2CustomDivisor,
                    entidad.Dim2OffsetSixteenths, entidad.Dim2ThicknessMultiplier,
                    entidad.QuantityMode, entidad.QuantityFactor,
                    dto.Condition, entidad.SortOrder));
        });

        grupo.MapPut("/reglas-corte/{id:guid}", async (
            Guid id,
            GuardarCutRuleDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductCutRules
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (entidad is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(dto.PartName))
                return Results.BadRequest("El nombre de la pieza es obligatorio.");

            var materialExiste = await db.MaterialCatalog
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == dto.MaterialId);
            if (!materialExiste) return Results.BadRequest("El material indicado no existe.");

            entidad.PartName = dto.PartName.Trim();
            entidad.MaterialId = dto.MaterialId;
            entidad.MeasureMode = dto.MeasureMode;
            entidad.Dim1Axis = dto.Dim1Axis;
            entidad.Dim1DivisorSource = dto.Dim1DivisorSource;
            entidad.Dim1CustomDivisor = dto.Dim1CustomDivisor <= 0 ? 1m : dto.Dim1CustomDivisor;
            entidad.Dim1OffsetSixteenths = dto.Dim1OffsetSixteenths;
            entidad.Dim1ThicknessMultiplier = dto.Dim1ThicknessMultiplier;
            entidad.Dim2Axis = dto.Dim2Axis;
            entidad.Dim2DivisorSource = dto.Dim2DivisorSource;
            entidad.Dim2CustomDivisor = dto.Dim2CustomDivisor <= 0 ? 1m : dto.Dim2CustomDivisor;
            entidad.Dim2OffsetSixteenths = dto.Dim2OffsetSixteenths;
            entidad.Dim2ThicknessMultiplier = dto.Dim2ThicknessMultiplier;
            entidad.QuantityMode = dto.QuantityMode;
            entidad.QuantityFactor = Math.Max(1, dto.QuantityFactor);
            entidad.ConditionJson = SerializarCondicion(dto.Condition);
            entidad.SortOrder = dto.SortOrder;
            entidad.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        grupo.MapDelete("/reglas-corte/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductCutRules
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (entidad is null) return Results.NotFound();

            entidad.Eliminado = true;
            entidad.FechaModificacion = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // =====================================================================
        // 6. REGLAS DE HERRAJES DE UN MODELO (Solo Admin)
        // =====================================================================
        grupo.MapPost("/modelos/{modeloId:guid}/reglas-herrajes", async (
            Guid modeloId,
            GuardarHardwareRuleDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            if (dto.Factor <= 0)
                return Results.BadRequest("El factor del herraje debe ser mayor que cero.");

            var modeloExiste = await db.ProductModels
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == modeloId);
            if (!modeloExiste) return Results.NotFound("Modelo no encontrado.");

            var materialExiste = await db.MaterialCatalog
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == dto.MaterialId);
            if (!materialExiste) return Results.BadRequest("El material/herraje indicado no existe.");

            var entidad = new ProductHardwareRule
            {
                EmpresaId = ctx.EmpresaId,
                ProductModelId = modeloId,
                MaterialId = dto.MaterialId,
                CalculationBasis = dto.CalculationBasis,
                Factor = dto.Factor,
                ConditionJson = SerializarCondicion(dto.Condition),
                SortOrder = dto.SortOrder
            };

            db.ProductHardwareRules.Add(entidad);
            await db.SaveChangesAsync();

            return Results.Created($"/api/catalogo-universal/reglas-herrajes/{entidad.Id}",
                new HardwareRuleDto(
                    entidad.Id, entidad.ProductModelId, entidad.MaterialId,
                    entidad.CalculationBasis, entidad.Factor,
                    dto.Condition, entidad.SortOrder));
        });

        grupo.MapPut("/reglas-herrajes/{id:guid}", async (
            Guid id,
            GuardarHardwareRuleDto dto,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductHardwareRules
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (entidad is null) return Results.NotFound();

            if (dto.Factor <= 0)
                return Results.BadRequest("El factor del herraje debe ser mayor que cero.");

            var materialExiste = await db.MaterialCatalog
                .AsNoTracking()
                .VisiblesPara(ctx)
                .AnyAsync(m => m.Id == dto.MaterialId);
            if (!materialExiste) return Results.BadRequest("El material/herraje indicado no existe.");

            entidad.MaterialId = dto.MaterialId;
            entidad.CalculationBasis = dto.CalculationBasis;
            entidad.Factor = dto.Factor;
            entidad.ConditionJson = SerializarCondicion(dto.Condition);
            entidad.SortOrder = dto.SortOrder;
            entidad.FechaModificacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        grupo.MapDelete("/reglas-herrajes/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var ctx = await db.ObtenerContextoAsync(user);
            if (ctx is null) return Results.Unauthorized();
            if (!ctx.EsAdmin) return Results.Forbid();

            var entidad = await db.ProductHardwareRules
                .VisiblesPara(ctx)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (entidad is null) return Results.NotFound();

            entidad.Eliminado = true;
            entidad.FechaModificacion = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static string? SerializarCondicion(RuleCondition? condicion) =>
        condicion is null ? null : JsonSerializer.Serialize(condicion, JsonOpciones);

    private static RuleCondition? DeserializarCondicion(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<RuleCondition>(json, JsonOpciones);
        }
        catch
        {
            return null;
        }
    }
}