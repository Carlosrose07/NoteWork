using NoteWork.Shared.Entidades.Universal;
using NoteWork.Shared.Motor;

namespace NoteWork.Shared.Dtos;

// =========================================================================
// 1. CATÁLOGO UNIVERSAL (Categorías, Materiales, Modelos y Reglas)
// =========================================================================

/// <summary>Paquete completo del catálogo para cargar en una sola llamada en Blazor WASM.</summary>
public sealed record CatalogoUniversalPaqueteDto(
    IReadOnlyList<ProductCategoryDto> Categories,
    IReadOnlyList<MaterialCatalogDto> Materials,
    IReadOnlyList<ProductModelDto> Models);

public sealed record ProductCategoryDto(
    Guid Id,
    string Code,
    string Name,
    bool UsesDepth,
    bool UsesLeaves,
    bool UsesDoors,
    bool UsesDrawers,
    bool UsesShelves,
    bool UsesLowTrack,
    string DefaultUnit,
    int SortOrder);

public sealed record GuardarProductCategoryDto(
    string Code,
    string Name,
    bool UsesDepth,
    bool UsesLeaves,
    bool UsesDoors,
    bool UsesDrawers,
    bool UsesShelves,
    bool UsesLowTrack,
    string DefaultUnit,
    int SortOrder);

public sealed record MaterialCatalogDto(
    Guid Id,
    string Code,
    string Name,
    MaterialType MaterialType,
    decimal? CommercialLengthInches,
    decimal? CommercialWidthInches,
    decimal? CommercialHeightInches,
    decimal UnitCost,
    decimal DefaultWastePercent,
    string? ColorOptions,
    bool IsActive)
{
    /// <summary>Convierte el DTO al formato que consume el Motor Universal en memoria.</summary>
    public MaterialCatalogEntry ToMotorEntry() => new(
        Id,
        Code,
        Name,
        MaterialType,
        CommercialLengthInches,
        CommercialWidthInches,
        CommercialHeightInches,
        UnitCost,
        DefaultWastePercent);
}

public sealed record GuardarMaterialCatalogDto(
    string Code,
    string Name,
    MaterialType MaterialType,
    decimal? CommercialLengthInches,
    decimal? CommercialWidthInches,
    decimal? CommercialHeightInches,
    decimal UnitCost,
    decimal DefaultWastePercent,
    string? ColorOptions,
    bool IsActive);

public sealed record CutRuleDto(
    Guid Id,
    Guid ProductModelId,
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
    RuleCondition? Condition,
    int SortOrder)
{
    public CutRuleDefinition ToMotorDefinition() => new(
        Id,
        PartName,
        MaterialId,
        MeasureMode,
        Dim1Axis,
        Dim1DivisorSource,
        Dim1CustomDivisor,
        Dim1OffsetSixteenths,
        Dim1ThicknessMultiplier,
        Dim2Axis,
        Dim2DivisorSource,
        Dim2CustomDivisor,
        Dim2OffsetSixteenths,
        Dim2ThicknessMultiplier,
        QuantityMode,
        QuantityFactor,
        Condition);
}

public sealed record GuardarCutRuleDto(
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
    RuleCondition? Condition,
    int SortOrder);

public sealed record HardwareRuleDto(
    Guid Id,
    Guid ProductModelId,
    Guid MaterialId,
    HardwareCalculationBasis CalculationBasis,
    decimal Factor,
    RuleCondition? Condition,
    int SortOrder)
{
    public HardwareRuleDefinition ToMotorDefinition() => new(
        Id,
        MaterialId,
        CalculationBasis,
        Factor,
        Condition);
}

public sealed record GuardarHardwareRuleDto(
    Guid MaterialId,
    HardwareCalculationBasis CalculationBasis,
    decimal Factor,
    RuleCondition? Condition,
    int SortOrder);

public sealed record ProductModelDto(
    Guid Id,
    Guid CategoryId,
    string Code,
    string Name,
    int DefaultBoardThicknessSixteenths,
    LaborMode DefaultLaborMode,
    decimal DefaultLaborValue,
    decimal DefaultMarginPercent,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<CutRuleDto> CutRules,
    IReadOnlyList<HardwareRuleDto> HardwareRules)
{
    /// <summary>Convierte el modelo y sus reglas al formato que consume el Motor Universal.</summary>
    public ProductModelDefinition ToMotorDefinition() => new(
        Id,
        Code,
        Name,
        DefaultBoardThicknessSixteenths,
        DefaultLaborMode,
        DefaultLaborValue,
        DefaultMarginPercent,
        CutRules.Select(r => r.ToMotorDefinition()).ToList(),
        HardwareRules.Select(r => r.ToMotorDefinition()).ToList());
}

public sealed record GuardarProductModelDto(
    Guid CategoryId,
    string Code,
    string Name,
    int DefaultBoardThicknessSixteenths,
    LaborMode DefaultLaborMode,
    decimal DefaultLaborValue,
    decimal DefaultMarginPercent,
    int SortOrder,
    bool IsActive);

// =========================================================================
// 2. ÓRDENES DE TRABAJO Y PRESUPUESTOS (WorkOrders y WorkOrderItems)
// =========================================================================

/// <summary>Fila ligera para el listado paginado de órdenes/presupuestos.</summary>
public sealed record WorkOrderResumenDto(
    Guid Id,
    Guid UsuarioId,
    string OrderNumber,
    WorkOrderType OrderType,
    Guid? CustomerId,
    Guid? ProjectId,
    string CustomerName,
    WorkOrderStatus Status,
    decimal TotalQuoteAmount,
    int ItemCount,
    DateTime FechaCreacion);

public sealed record WorkOrdersPaginaDto(
    IReadOnlyList<WorkOrderResumenDto> Items,
    int TotalRegistros,
    int Pagina,
    int TamanoPagina);

public sealed record WorkOrderItemDto(
    Guid Id,
    Guid WorkOrderId,
    Guid? AreaNodeId,
    Guid ProductModelId,
    int LineOrder,
    int Quantity,
    int WidthSixteenths,
    int HeightSixteenths,
    int? DepthSixteenths,
    int LeafCount,
    int DoorCount,
    int DrawerCount,
    int ShelfCount,
    bool IsLowTrack,
    int? BoardThicknessOverrideSixteenths,
    string? MaterialColor,
    decimal? CustomMarginPercent,
    decimal CalculatedUnitPrice,
    decimal CalculatedLineTotal,
    string? Notes)
{
    /// <summary>Convierte el ítem a entrada del Motor Universal (precisión 1/16").</summary>
    public BreakdownItemInput ToMotorInput() => new(
        Id,
        LineOrder,
        ProductModelId,
        Quantity,
        new MeasureFraction(WidthSixteenths),
        new MeasureFraction(HeightSixteenths),
        DepthSixteenths.HasValue ? new MeasureFraction(DepthSixteenths.Value) : null,
        LeafCount,
        DoorCount,
        DrawerCount,
        ShelfCount,
        IsLowTrack,
        MaterialColor,
        CustomMarginPercent,
        BoardThicknessOverrideSixteenths);
}

public sealed record WorkOrderDetalleDto(
    Guid Id,
    Guid UsuarioId,
    string OrderNumber,
    WorkOrderType OrderType,
    Guid? CustomerId,
    Guid? ProjectId,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    WorkOrderStatus Status,
    decimal TaxRatePercent,
    decimal DiscountPercent,
    decimal SubtotalMaterials,
    decimal SubtotalLabor,
    decimal TotalQuoteAmount,
    string? Notes,
    string? PricingSnapshotJson,
    DateTime FechaCreacion,
    IReadOnlyList<WorkOrderItemDto> Items);

public sealed record CrearActualizarWorkOrderDto(
    WorkOrderType OrderType,
    Guid? CustomerId,
    Guid? ProjectId,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    WorkOrderStatus Status,
    decimal TaxRatePercent,
    decimal DiscountPercent,
    decimal SubtotalMaterials,
    decimal SubtotalLabor,
    decimal TotalQuoteAmount,
    string? Notes,
    string? PricingSnapshotJson);

public sealed record CrearActualizarWorkOrderItemDto(
    Guid? AreaNodeId,
    Guid ProductModelId,
    int LineOrder,
    int Quantity,
    int WidthSixteenths,
    int HeightSixteenths,
    int? DepthSixteenths,
    int LeafCount,
    int DoorCount,
    int DrawerCount,
    int ShelfCount,
    bool IsLowTrack,
    int? BoardThicknessOverrideSixteenths,
    string? MaterialColor,
    decimal? CustomMarginPercent,
    decimal CalculatedUnitPrice,
    decimal CalculatedLineTotal,
    string? Notes);

public sealed record ActualizarEstadoWorkOrderDto(WorkOrderStatus Status);