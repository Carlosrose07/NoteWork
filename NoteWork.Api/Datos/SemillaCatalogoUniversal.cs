using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NoteWork.Shared.Entidades.Universal;
using NoteWork.Shared.Motor;

namespace NoteWork.Api.Datos;

/// <summary>
/// Siembra el catálogo inicial del Motor Universal para una empresa
/// (Ventanas, Puertas, Gabinetes y Closets) si aún no tiene categorías creadas.
/// </summary>
public static class SemillaCatalogoUniversal
{
    private static readonly JsonSerializerOptions JsonOpciones = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task SembrarSiEstaVacioAsync(AppDbContext db, Guid empresaId)
    {
        // IgnoreQueryFilters garantiza que solo se siembre una vez por empresa,
        // incluso si luego el administrador desactiva o elimina categorías iniciales.
        var yaTieneCatalogo = await db.ProductCategories
            .IgnoreQueryFilters()
            .AnyAsync(c => c.EmpresaId == empresaId);

        if (yaTieneCatalogo) return;

        // --- 1. CATEGORÍAS (controlan los campos visibles en el formulario móvil) ---
        var catVentanas = new ProductCategory
        {
            EmpresaId = empresaId,
            Code = "WINDOW",
            Name = "Ventanas",
            UsesLeaves = true,
            UsesLowTrack = true,
            DefaultUnit = "in",
            SortOrder = 1
        };

        var catPuertas = new ProductCategory
        {
            EmpresaId = empresaId,
            Code = "DOOR",
            Name = "Puertas",
            UsesLeaves = true,
            UsesLowTrack = true,
            DefaultUnit = "in",
            SortOrder = 2
        };

        var catGabinetes = new ProductCategory
        {
            EmpresaId = empresaId,
            Code = "CABINET",
            Name = "Gabinetes",
            UsesDepth = true,
            UsesDoors = true,
            UsesDrawers = true,
            UsesShelves = true,
            DefaultUnit = "in",
            SortOrder = 3
        };

        var catClosets = new ProductCategory
        {
            EmpresaId = empresaId,
            Code = "CLOSET",
            Name = "Closets",
            UsesDepth = true,
            UsesDoors = true,
            UsesDrawers = true,
            UsesShelves = true,
            DefaultUnit = "in",
            SortOrder = 4
        };

        db.ProductCategories.AddRange(catVentanas, catPuertas, catGabinetes, catClosets);

        // --- 2. CATÁLOGO BASE DE MATERIALES (costos editables por cada empresa) ---
        // Aluminio y perfiles 1D (barras de 21 pies = 252 pulgadas)
        var aluP65Marco = NuevoMaterial(empresaId, "ALU-P65-MARCO", "Aluminio P-65 Marco / Cabezal (21')", MaterialType.Linear1D, 252m, null, null, 1850m, 10m, "Blanco,Bronce,Negro,Natural");
        var aluP65RielBajo = NuevoMaterial(empresaId, "ALU-P65-RIELB", "Aluminio P-65 Riel Bajo (21')", MaterialType.Linear1D, 252m, null, null, 2100m, 10m, "Blanco,Bronce,Negro,Natural");
        var aluP65Hoja = NuevoMaterial(empresaId, "ALU-P65-HOJA", "Aluminio P-65 Hoja / Llavín (21')", MaterialType.Linear1D, 252m, null, null, 1650m, 10m, "Blanco,Bronce,Negro,Natural");
        var aluPtaMarco = NuevoMaterial(empresaId, "ALU-PTA-MARCO", "Aluminio Puerta Comercial Marco (21')", MaterialType.Linear1D, 252m, null, null, 2950m, 10m, "Blanco,Bronce,Negro,Natural");
        var aluPtaHoja = NuevoMaterial(empresaId, "ALU-PTA-HOJA", "Aluminio Puerta Comercial Hoja (21')", MaterialType.Linear1D, 252m, null, null, 2750m, 10m, "Blanco,Bronce,Negro,Natural");
        var tuboColgador = NuevoMaterial(empresaId, "TUBO-OVAL", "Tubo Ovalado Colgador Cromado (12')", MaterialType.Linear1D, 144m, null, null, 450m, 5m, "Cromado,Negro");
        var tapacantoPvc = NuevoMaterial(empresaId, "TAPA-PVC", "Tapacanto PVC 1mm (por pie)", MaterialType.Linear1D, null, null, null, 15m, 10m, "Blanco,Roble,Nogal,Gris");
        var gomaCristal = NuevoMaterial(empresaId, "GOMA-CRI", "Goma / Felpa para Cristal (por pie)", MaterialType.Linear1D, null, null, null, 12m, 5m, "Negro,Gris");

        // Planchas 2D (cristal y tableros)
        var cristal14 = NuevoMaterial(empresaId, "CRI-14", "Cristal Claro 1/4\" (130\" x 84\")", MaterialType.Sheet2D, null, 130m, 84m, 6800m, 12m, "Claro,Bronce,Gris,Esmerilado");
        var melamina34 = NuevoMaterial(empresaId, "MEL-34", "Plancha Melamina 3/4\" (48\" x 96\")", MaterialType.Sheet2D, null, 48m, 96m, 3400m, 12m, "Blanco,Roble,Nogal,Gris,Antracita");
        var fondoMdf14 = NuevoMaterial(empresaId, "MDF-14", "Plancha Fondo MDF 1/4\" (48\" x 96\")", MaterialType.Sheet2D, null, 48m, 96m, 1250m, 10m, "Blanco,Crudo");

        // Herrajes por unidad
        var ruedaVentana = NuevoMaterial(empresaId, "HER-RUEDA-V", "Rueda de Ventana Corrediza", MaterialType.Unit, null, null, null, 95m, 0m, null);
        var ruedaPuerta = NuevoMaterial(empresaId, "HER-RUEDA-P", "Rueda Pesada Puerta Corrediza", MaterialType.Unit, null, null, null, 240m, 0m, null);
        var cerraduraVentana = NuevoMaterial(empresaId, "HER-CERR-V", "Pestillo / Cerradura de Ventana", MaterialType.Unit, null, null, null, 320m, 0m, "Blanco,Negro,Bronce");
        var cerraduraPuerta = NuevoMaterial(empresaId, "HER-CERR-P", "Cerradura de Puerta Corrediza", MaterialType.Unit, null, null, null, 1150m, 0m, "Blanco,Negro,Plata");
        var bisagraCazoleta = NuevoMaterial(empresaId, "HER-BIS-CAZ", "Bisagra Cazoleta Cierre Suave", MaterialType.Unit, null, null, null, 110m, 0m, null);
        var correderaPar = NuevoMaterial(empresaId, "HER-CORR-PAR", "Corredera Telescópica (Par)", MaterialType.Unit, null, null, null, 420m, 0m, null);
        var tirador = NuevoMaterial(empresaId, "HER-TIRADOR", "Tirador / Jaladera Estándar", MaterialType.Unit, null, null, null, 160m, 0m, "Negro,Plata,Dorado");
        var soporteTubo = NuevoMaterial(empresaId, "HER-SOP-TUBO", "Soporte Lateral para Tubo Ovalado", MaterialType.Unit, null, null, null, 65m, 0m, "Cromado,Negro");

        db.MaterialCatalog.AddRange(
            aluP65Marco, aluP65RielBajo, aluP65Hoja,
            aluPtaMarco, aluPtaHoja, tuboColgador, tapacantoPvc, gomaCristal,
            cristal14, melamina34, fondoMdf14,
            ruedaVentana, ruedaPuerta, cerraduraVentana, cerraduraPuerta,
            bisagraCazoleta, correderaPar, tirador, soporteTubo);

        // --- 3. MODELOS FABRICABLES (1 por categoría listos para cotizar y desglosar) ---
        var modVentanaP65 = new ProductModel
        {
            EmpresaId = empresaId,
            CategoryId = catVentanas.Id,
            Code = "VENT-P65",
            Name = "Ventana Corrediza P-65",
            DefaultBoardThicknessSixteenths = 0,
            DefaultLaborMode = LaborMode.Percent,
            DefaultLaborValue = 25m,
            DefaultMarginPercent = 30m,
            SortOrder = 1
        };

        var modPuertaCorr = new ProductModel
        {
            EmpresaId = empresaId,
            CategoryId = catPuertas.Id,
            Code = "PTA-CORR",
            Name = "Puerta Corrediza Comercial",
            DefaultBoardThicknessSixteenths = 0,
            DefaultLaborMode = LaborMode.PerSqFt,
            DefaultLaborValue = 180m,
            DefaultMarginPercent = 30m,
            SortOrder = 1
        };

        var modGabineteBase = new ProductModel
        {
            EmpresaId = empresaId,
            CategoryId = catGabinetes.Id,
            Code = "GAB-BASE",
            Name = "Gabinete Base Cocina (3/4\")",
            DefaultBoardThicknessSixteenths = 12, // 3/4" = 12/16"
            DefaultLaborMode = LaborMode.Percent,
            DefaultLaborValue = 35m,
            DefaultMarginPercent = 35m,
            SortOrder = 1
        };

        var modClosetModular = new ProductModel
        {
            EmpresaId = empresaId,
            CategoryId = catClosets.Id,
            Code = "CLOSET-MOD",
            Name = "Closet Modular Melamina (3/4\")",
            DefaultBoardThicknessSixteenths = 12, // 3/4" = 12/16"
            DefaultLaborMode = LaborMode.Percent,
            DefaultLaborValue = 35m,
            DefaultMarginPercent = 35m,
            SortOrder = 1
        };

        db.ProductModels.AddRange(modVentanaP65, modPuertaCorr, modGabineteBase, modClosetModular);

        // Condiciones JSON reutilizables
        var condRielNormal = CondJson(new RuleCondition(IsLowTrack: false));
        var condRielBajo = CondJson(new RuleCondition(IsLowTrack: true));
        var condConPuertas = CondJson(new RuleCondition(MinDoorCount: 1));
        var condConGavetas = CondJson(new RuleCondition(MinDrawerCount: 1));
        var condConRepisas = CondJson(new RuleCondition(MinShelfCount: 1));

        // --- 4. REGLAS DE CORTE ---
        db.ProductCutRules.AddRange(
            // === VENTANA P-65 ===
            Regla1D(empresaId, modVentanaP65.Id, "Cabezal Superior", aluP65Marco.Id, DimensionAxis.Width, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 1, null, 1),
            Regla1D(empresaId, modVentanaP65.Id, "Riel Inferior Estándar", aluP65Marco.Id, DimensionAxis.Width, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 1, condRielNormal, 2),
            Regla1D(empresaId, modVentanaP65.Id, "Riel Bajo", aluP65RielBajo.Id, DimensionAxis.Width, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 1, condRielBajo, 3),
            Regla1D(empresaId, modVentanaP65.Id, "Jambas Laterales", aluP65Marco.Id, DimensionAxis.Height, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 2, null, 4),
            Regla1D(empresaId, modVentanaP65.Id, "Horizontales de Hoja", aluP65Hoja.Id, DimensionAxis.Width, DivisorSource.LeafCount, -16, 0, QuantityMode.PerLeaf, 2, null, 5),
            Regla1D(empresaId, modVentanaP65.Id, "Verticales de Hoja (Llavín/Traslape)", aluP65Hoja.Id, DimensionAxis.Height, DivisorSource.ConstantOne, -24, 0, QuantityMode.PerLeaf, 2, null, 6),
            Regla2D(empresaId, modVentanaP65.Id, "Cristal 1/4\"", cristal14.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.LeafCount, -32, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -48, 0,
                QuantityMode.PerLeaf, 1, null, 7),

            // === PUERTA CORREDIZA COMERCIAL ===
            Regla1D(empresaId, modPuertaCorr.Id, "Cabezal Marco Puerta", aluPtaMarco.Id, DimensionAxis.Width, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 1, null, 1),
            Regla1D(empresaId, modPuertaCorr.Id, "Riel Inferior Puerta", aluPtaMarco.Id, DimensionAxis.Width, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 1, condRielNormal, 2),
            Regla1D(empresaId, modPuertaCorr.Id, "Riel Bajo Puerta", aluP65RielBajo.Id, DimensionAxis.Width, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 1, condRielBajo, 3),
            Regla1D(empresaId, modPuertaCorr.Id, "Jambas Laterales Puerta", aluPtaMarco.Id, DimensionAxis.Height, DivisorSource.ConstantOne, 0, 0, QuantityMode.Fixed, 2, null, 4),
            Regla1D(empresaId, modPuertaCorr.Id, "Horizontales Hoja Puerta", aluPtaHoja.Id, DimensionAxis.Width, DivisorSource.LeafCount, -16, 0, QuantityMode.PerLeaf, 2, null, 5),
            Regla1D(empresaId, modPuertaCorr.Id, "Verticales Hoja Puerta", aluPtaHoja.Id, DimensionAxis.Height, DivisorSource.ConstantOne, -28, 0, QuantityMode.PerLeaf, 2, null, 6),
            Regla2D(empresaId, modPuertaCorr.Id, "Cristal Puerta 1/4\"", cristal14.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.LeafCount, -48, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -64, 0,
                QuantityMode.PerLeaf, 1, null, 7),

            // === GABINETE BASE COCINA (3D) ===
            Regla2D(empresaId, modGabineteBase.Id, "Costados Laterales", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Depth, DivisorSource.ConstantOne, 0, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, 0, 0,
                QuantityMode.Fixed, 2, null, 1),
            Regla2D(empresaId, modGabineteBase.Id, "Piso / Base Interior", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.ConstantOne, 0, -2,
                DimensionAxis.Depth, DivisorSource.ConstantOne, 0, 0,
                QuantityMode.Fixed, 1, null, 2),
            Regla2D(empresaId, modGabineteBase.Id, "Amarres Superiores (4\")", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.ConstantOne, 0, -2,
                DimensionAxis.None, DivisorSource.ConstantOne, 64, 0,
                QuantityMode.Fixed, 2, null, 3),
            Regla2D(empresaId, modGabineteBase.Id, "Tramos / Repisas Interiores", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.ConstantOne, 0, -2,
                DimensionAxis.Depth, DivisorSource.ConstantOne, -16, 0,
                QuantityMode.PerShelf, 1, condConRepisas, 4),
            Regla2D(empresaId, modGabineteBase.Id, "Puertas Frontales", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.DoorCount, -2, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -4, 0,
                QuantityMode.PerDoor, 1, condConPuertas, 5),
            Regla2D(empresaId, modGabineteBase.Id, "Tapacanto de Puertas", tapacantoPvc.Id, MeasureMode.Perimeter1D,
                DimensionAxis.Width, DivisorSource.DoorCount, -2, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -4, 0,
                QuantityMode.PerDoor, 1, condConPuertas, 6),
            Regla2D(empresaId, modGabineteBase.Id, "Fondo MDF 1/4\"", fondoMdf14.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.ConstantOne, -8, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -8, 0,
                QuantityMode.Fixed, 1, null, 7),

            // === CLOSET MODULAR (3D) ===
            Regla2D(empresaId, modClosetModular.Id, "Costados Laterales", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Depth, DivisorSource.ConstantOne, 0, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, 0, 0,
                QuantityMode.Fixed, 2, null, 1),
            Regla2D(empresaId, modClosetModular.Id, "Techo y Base Interior", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.ConstantOne, 0, -2,
                DimensionAxis.Depth, DivisorSource.ConstantOne, 0, 0,
                QuantityMode.Fixed, 2, null, 2),
            Regla2D(empresaId, modClosetModular.Id, "Tramos / Repisas", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.ConstantOne, 0, -2,
                DimensionAxis.Depth, DivisorSource.ConstantOne, -16, 0,
                QuantityMode.PerShelf, 1, condConRepisas, 3),
            Regla2D(empresaId, modClosetModular.Id, "Puertas de Closet", melamina34.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.DoorCount, -2, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -4, 0,
                QuantityMode.PerDoor, 1, condConPuertas, 4),
            Regla2D(empresaId, modClosetModular.Id, "Tapacanto Puertas Closet", tapacantoPvc.Id, MeasureMode.Perimeter1D,
                DimensionAxis.Width, DivisorSource.DoorCount, -2, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -4, 0,
                QuantityMode.PerDoor, 1, condConPuertas, 5),
            Regla1D(empresaId, modClosetModular.Id, "Tubo Colgador", tuboColgador.Id, DimensionAxis.Width, DivisorSource.ConstantOne, 0, -2, QuantityMode.Fixed, 1, null, 6),
            Regla2D(empresaId, modClosetModular.Id, "Fondo MDF 1/4\"", fondoMdf14.Id, MeasureMode.Rect2D,
                DimensionAxis.Width, DivisorSource.ConstantOne, -8, 0,
                DimensionAxis.Height, DivisorSource.ConstantOne, -8, 0,
                QuantityMode.Fixed, 1, null, 7)
        );

        // --- 5. REGLAS DE HERRAJES ---
        db.ProductHardwareRules.AddRange(
            // Ventana P-65
            ReglaHerraje(empresaId, modVentanaP65.Id, ruedaVentana.Id, HardwareCalculationBasis.PerLeaf, 2m, null, 1),
            ReglaHerraje(empresaId, modVentanaP65.Id, cerraduraVentana.Id, HardwareCalculationBasis.PerUnit, 1m, null, 2),
            ReglaHerraje(empresaId, modVentanaP65.Id, gomaCristal.Id, HardwareCalculationBasis.PerimeterFeet, 1m, null, 3),

            // Puerta Corrediza Comercial
            ReglaHerraje(empresaId, modPuertaCorr.Id, ruedaPuerta.Id, HardwareCalculationBasis.PerLeaf, 2m, null, 1),
            ReglaHerraje(empresaId, modPuertaCorr.Id, cerraduraPuerta.Id, HardwareCalculationBasis.PerUnit, 1m, null, 2),
            ReglaHerraje(empresaId, modPuertaCorr.Id, gomaCristal.Id, HardwareCalculationBasis.PerimeterFeet, 1m, null, 3),

            // Gabinete Base Cocina
            ReglaHerraje(empresaId, modGabineteBase.Id, bisagraCazoleta.Id, HardwareCalculationBasis.PerDoor, 2m, condConPuertas, 1),
            ReglaHerraje(empresaId, modGabineteBase.Id, correderaPar.Id, HardwareCalculationBasis.PerDrawer, 1m, condConGavetas, 2),
            ReglaHerraje(empresaId, modGabineteBase.Id, tirador.Id, HardwareCalculationBasis.PerDoor, 1m, condConPuertas, 3),
            ReglaHerraje(empresaId, modGabineteBase.Id, tirador.Id, HardwareCalculationBasis.PerDrawer, 1m, condConGavetas, 4),

            // Closet Modular
            ReglaHerraje(empresaId, modClosetModular.Id, bisagraCazoleta.Id, HardwareCalculationBasis.PerDoor, 3m, condConPuertas, 1),
            ReglaHerraje(empresaId, modClosetModular.Id, correderaPar.Id, HardwareCalculationBasis.PerDrawer, 1m, condConGavetas, 2),
            ReglaHerraje(empresaId, modClosetModular.Id, tirador.Id, HardwareCalculationBasis.PerDoor, 1m, condConPuertas, 3),
            ReglaHerraje(empresaId, modClosetModular.Id, tirador.Id, HardwareCalculationBasis.PerDrawer, 1m, condConGavetas, 4),
            ReglaHerraje(empresaId, modClosetModular.Id, soporteTubo.Id, HardwareCalculationBasis.PerUnit, 2m, null, 5)
        );

        await db.SaveChangesAsync();
    }

    // --- HELPERS INTERNOS PARA MANTENER LA SEMILLA LEGIBLE ---

    private static string CondJson(RuleCondition condition) =>
        JsonSerializer.Serialize(condition, JsonOpciones);

    private static MaterialCatalogItem NuevoMaterial(
        Guid empresaId, string code, string name, MaterialType tipo,
        decimal? lengthIn, decimal? widthIn, decimal? heightIn,
        decimal unitCost, decimal wastePercent, string? colors) => new()
        {
            EmpresaId = empresaId,
            Code = code,
            Name = name,
            MaterialType = tipo,
            CommercialLengthInches = lengthIn,
            CommercialWidthInches = widthIn,
            CommercialHeightInches = heightIn,
            UnitCost = unitCost,
            DefaultWastePercent = wastePercent,
            ColorOptions = colors,
            IsActive = true
        };

    private static ProductCutRule Regla1D(
        Guid empresaId, Guid modelId, string partName, Guid materialId,
        DimensionAxis axis, DivisorSource divisor, int offsetSixteenths, int thicknessMult,
        QuantityMode qtyMode, int qtyFactor, string? conditionJson, int sortOrder) => new()
        {
            EmpresaId = empresaId,
            ProductModelId = modelId,
            PartName = partName,
            MaterialId = materialId,
            MeasureMode = MeasureMode.Length1D,
            Dim1Axis = axis,
            Dim1DivisorSource = divisor,
            Dim1CustomDivisor = 1m,
            Dim1OffsetSixteenths = offsetSixteenths,
            Dim1ThicknessMultiplier = thicknessMult,
            Dim2Axis = DimensionAxis.None,
            Dim2DivisorSource = DivisorSource.ConstantOne,
            Dim2CustomDivisor = 1m,
            QuantityMode = qtyMode,
            QuantityFactor = qtyFactor,
            ConditionJson = conditionJson,
            SortOrder = sortOrder
        };

    private static ProductCutRule Regla2D(
        Guid empresaId, Guid modelId, string partName, Guid materialId, MeasureMode mode,
        DimensionAxis dim1Axis, DivisorSource dim1Div, int dim1Offset, int dim1ThickMult,
        DimensionAxis dim2Axis, DivisorSource dim2Div, int dim2Offset, int dim2ThickMult,
        QuantityMode qtyMode, int qtyFactor, string? conditionJson, int sortOrder) => new()
        {
            EmpresaId = empresaId,
            ProductModelId = modelId,
            PartName = partName,
            MaterialId = materialId,
            MeasureMode = mode,
            Dim1Axis = dim1Axis,
            Dim1DivisorSource = dim1Div,
            Dim1CustomDivisor = 1m,
            Dim1OffsetSixteenths = dim1Offset,
            Dim1ThicknessMultiplier = dim1ThickMult,
            Dim2Axis = dim2Axis,
            Dim2DivisorSource = dim2Div,
            Dim2CustomDivisor = 1m,
            Dim2OffsetSixteenths = dim2Offset,
            Dim2ThicknessMultiplier = dim2ThickMult,
            QuantityMode = qtyMode,
            QuantityFactor = qtyFactor,
            ConditionJson = conditionJson,
            SortOrder = sortOrder
        };

    private static ProductHardwareRule ReglaHerraje(
        Guid empresaId, Guid modelId, Guid materialId,
        HardwareCalculationBasis basis, decimal factor, string? conditionJson, int sortOrder) => new()
        {
            EmpresaId = empresaId,
            ProductModelId = modelId,
            MaterialId = materialId,
            CalculationBasis = basis,
            Factor = factor,
            ConditionJson = conditionJson,
            SortOrder = sortOrder
        };
}