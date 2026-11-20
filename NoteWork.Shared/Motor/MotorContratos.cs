namespace NoteWork.Shared.Motor;

/// <summary>Tipo comercial del material en el catálogo.</summary>
public enum MaterialType
{
    /// <summary>Barras, perfiles, tubos, goma o tapacanto (se mide en largo).</summary>
    Linear1D = 1,
    /// <summary>Cristales, tableros de melamina, MDF o plywood (se mide en área).</summary>
    Sheet2D = 2,
    /// <summary>Herrajes y accesorios por unidad (bisagras, ruedas, correderas, tiradores).</summary>
    Unit = 3
}

/// <summary>Modo de cálculo de la mano de obra por modelo.</summary>
public enum LaborMode
{
    /// <summary>Monto fijo por unidad fabricada.</summary>
    Fixed = 1,
    /// <summary>Monto multiplicado por los pies cuadrados (Ancho × Alto) del producto.</summary>
    PerSqFt = 2,
    /// <summary>Porcentaje aplicado sobre el costo total de materiales.</summary>
    Percent = 3
}

/// <summary>Cómo se mide la pieza generada por una regla de corte.</summary>
public enum MeasureMode
{
    /// <summary>Pieza lineal 1D (ej. perfil de aluminio, tubo colgador).</summary>
    Length1D = 1,
    /// <summary>Pieza rectangular 2D (ej. cristal, costado, puerta, repisa).</summary>
    Rect2D = 2,
    /// <summary>Perímetro 1D calculado a partir de Dim1 y Dim2 (ej. tapacanto o goma).</summary>
    Perimeter1D = 3
}

/// <summary>Eje de entrada del producto usado por una dimensión de la regla.</summary>
public enum DimensionAxis
{
    None = 0,
    Width = 1,
    Height = 2,
    Depth = 3
}

/// <summary>Divisor aplicado al eje antes de restar/sumar ajustes.</summary>
public enum DivisorSource
{
    ConstantOne = 0,
    LeafCount = 1,
    DoorCount = 2,
    DrawerCount = 3,
    ShelfCount = 4,
    CustomConstant = 5
}

/// <summary>Base para calcular la cantidad de piezas de una regla de corte.</summary>
public enum QuantityMode
{
    Fixed = 0,
    PerLeaf = 1,
    PerDoor = 2,
    PerDrawer = 3,
    PerShelf = 4
}

/// <summary>Base para calcular herrajes y accesorios.</summary>
public enum HardwareCalculationBasis
{
    PerUnit = 0,
    PerLeaf = 1,
    PerDoor = 2,
    PerDrawer = 3,
    PerShelf = 4,
    PerimeterFeet = 5,
    AreaSqFt = 6
}

/// <summary>
/// Condición opcional para activar o ignorar una regla sin usar scripts pesados.
/// Se serializa directo hacia/desde ConditionJson en la base de datos.
/// </summary>
public sealed record RuleCondition(
    bool? IsLowTrack = null,
    int? ExactLeafCount = null,
    int? MinLeafCount = null,
    int? MaxLeafCount = null,
    int? MinDoorCount = null,
    int? MinDrawerCount = null,
    int? MinShelfCount = null)
{
    public bool Matches(BreakdownItemInput input)
    {
        if (IsLowTrack.HasValue && input.IsLowTrack != IsLowTrack.Value) return false;
        if (ExactLeafCount.HasValue && input.LeafCount != ExactLeafCount.Value) return false;
        if (MinLeafCount.HasValue && input.LeafCount < MinLeafCount.Value) return false;
        if (MaxLeafCount.HasValue && input.LeafCount > MaxLeafCount.Value) return false;
        if (MinDoorCount.HasValue && input.DoorCount < MinDoorCount.Value) return false;
        if (MinDrawerCount.HasValue && input.DrawerCount < MinDrawerCount.Value) return false;
        if (MinShelfCount.HasValue && input.ShelfCount < MinShelfCount.Value) return false;
        return true;
    }
}

/// <summary>Entrada de catálogo de materiales (barras, planchas o herrajes).</summary>
public sealed record MaterialCatalogEntry(
    Guid Id,
    string Code,
    string Name,
    MaterialType MaterialType,
    decimal? CommercialLengthInches,
    decimal? CommercialWidthInches,
    decimal? CommercialHeightInches,
    decimal UnitCost,
    decimal DefaultWastePercent);

/// <summary>Regla declarativa de corte para un producto 2D o 3D.</summary>
public sealed record CutRuleDefinition(
    Guid Id,
    string PartName,
    Guid MaterialId,
    MeasureMode MeasureMode,
    DimensionAxis Dim1Axis,
    DivisorSource Dim1DivisorSource,
    decimal Dim1CustomDivisor,
    int Dim1OffsetSixteenths,
    int Dim1ThicknessMultiplier,
    DimensionAxis Dim2Axis,
    DivisorSource Dim2DivisorSource,
    decimal Dim2CustomDivisor,
    int Dim2OffsetSixteenths,
    int Dim2ThicknessMultiplier,
    QuantityMode QuantityMode,
    int QuantityFactor,
    RuleCondition? Condition = null);

/// <summary>Regla declarativa de herrajes o accesorios.</summary>
public sealed record HardwareRuleDefinition(
    Guid Id,
    Guid MaterialId,
    HardwareCalculationBasis CalculationBasis,
    decimal Factor,
    RuleCondition? Condition = null);

/// <summary>Definición completa de un modelo fabricable (ej. Ventana P-65, Gabinete Base).</summary>
public sealed record ProductModelDefinition(
    Guid Id,
    string Code,
    string Name,
    int DefaultBoardThicknessSixteenths,
    LaborMode DefaultLaborMode,
    decimal DefaultLaborValue,
    decimal DefaultMarginPercent,
    IReadOnlyList<CutRuleDefinition> CutRules,
    IReadOnlyList<HardwareRuleDefinition> HardwareRules);

/// <summary>Datos capturados de un ítem (ventana, puerta, gabinete o closet).</summary>
public sealed record BreakdownItemInput(
    Guid ItemId,
    int LineOrder,
    Guid ModelId,
    int Quantity,
    MeasureFraction Width,
    MeasureFraction Height,
    MeasureFraction? Depth = null,
    int LeafCount = 2,
    int DoorCount = 0,
    int DrawerCount = 0,
    int ShelfCount = 0,
    bool IsLowTrack = false,
    string? MaterialColor = null,
    decimal? CustomMarginPercent = null,
    int? BoardThicknessOverrideSixteenths = null);

/// <summary>Pieza exacta a cortar generada por el motor.</summary>
public sealed record CutPieceResult(
    Guid ItemId,
    int LineOrder,
    string ModelName,
    string PartName,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    MeasureMode MeasureMode,
    MeasureFraction Dim1,
    MeasureFraction Dim2,
    int Quantity,
    decimal TotalLinearFeet,
    decimal TotalSquareFeet);

/// <summary>Requerimiento de herraje generado para un ítem.</summary>
public sealed record HardwareRequirementResult(
    Guid ItemId,
    int LineOrder,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    decimal Quantity);

/// <summary>Desglose completo de un solo ítem de la orden.</summary>
public sealed record ItemBreakdownResult(
    BreakdownItemInput Input,
    ProductModelDefinition Model,
    IReadOnlyList<CutPieceResult> CutPieces,
    IReadOnlyList<HardwareRequirementResult> HardwareItems);

/// <summary>Material consolidado a comprar para toda la orden o proyecto.</summary>
public sealed record ConsolidatedMaterialResult(
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    MaterialType MaterialType,
    decimal RawQuantity,
    decimal WastePercent,
    decimal QuantityWithWaste,
    decimal CommercialUnitsExact,
    int CommercialUnitsWhole,
    string CommercialUnitLabel,
    decimal UnitCost,
    decimal TotalCost);

/// <summary>Resultado financiero individual de cada ítem.</summary>
public sealed record ItemPriceResult(
    Guid ItemId,
    int LineOrder,
    int Quantity,
    decimal MaterialCost,
    decimal LaborCost,
    decimal MarginPercent,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>Resultado consolidado del desglose + presupuesto automático.</summary>
public sealed record OrderPricingResult(
    IReadOnlyList<ItemBreakdownResult> ItemBreakdowns,
    IReadOnlyList<ConsolidatedMaterialResult> ConsolidatedMaterials,
    IReadOnlyList<ItemPriceResult> ItemPrices,
    decimal SubtotalMaterials,
    decimal SubtotalLabor,
    decimal SubtotalBeforeDiscount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal SubtotalAfterDiscount,
    decimal TaxRatePercent,
    decimal TaxAmount,
    decimal TotalQuoteAmount);