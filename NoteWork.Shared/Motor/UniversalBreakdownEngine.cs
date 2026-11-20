namespace NoteWork.Shared.Motor;

/// <summary>
/// Motor universal de desglose (2D y 3D) para Ventanas, Puertas, Gabinetes, Closets y más.
/// Evalúa reglas declarativas en memoria (C# puro) con precisión exacta de 1/16".
/// </summary>
public static class UniversalBreakdownEngine
{
    /// <summary>
    /// Calcula el despiece completo (cortes + herrajes) para un solo ítem de la orden.
    /// </summary>
    public static ItemBreakdownResult CalculateItem(
        BreakdownItemInput input,
        ProductModelDefinition model,
        IReadOnlyDictionary<Guid, MaterialCatalogEntry> materials)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(materials);

        if (input.Quantity <= 0)
        {
            return new ItemBreakdownResult(input, model, [], []);
        }

        int boardThicknessSixteenths = input.BoardThicknessOverrideSixteenths
            ?? model.DefaultBoardThicknessSixteenths;

        var cutPieces = new List<CutPieceResult>(model.CutRules.Count);
        var hardwareItems = new List<HardwareRequirementResult>(model.HardwareRules.Count);

        // --- 1. EVALUAR REGLAS DE CORTE (1D, 2D Y PERÍMETRO) ---
        foreach (var rule in model.CutRules)
        {
            if (rule.Condition is not null && !rule.Condition.Matches(input))
                continue;

            int totalPieces = ResolveCutQuantity(rule.QuantityMode, rule.QuantityFactor, input);
            if (totalPieces <= 0)
                continue;

            var dim1 = EvaluateDimension(
                rule.Dim1Axis,
                rule.Dim1DivisorSource,
                rule.Dim1CustomDivisor,
                rule.Dim1OffsetSixteenths,
                rule.Dim1ThicknessMultiplier,
                boardThicknessSixteenths,
                input);

            var dim2 = rule.MeasureMode == MeasureMode.Length1D
                ? MeasureFraction.Zero
                : EvaluateDimension(
                    rule.Dim2Axis,
                    rule.Dim2DivisorSource,
                    rule.Dim2CustomDivisor,
                    rule.Dim2OffsetSixteenths,
                    rule.Dim2ThicknessMultiplier,
                    boardThicknessSixteenths,
                    input);

            // Descarta piezas inválidas (medida cero o negativa por medidas demasiado pequeñas).
            if (dim1.Sixteenths <= 0)
                continue;

            if (rule.MeasureMode != MeasureMode.Length1D && dim2.Sixteenths <= 0)
                continue;

            materials.TryGetValue(rule.MaterialId, out var material);
            string materialCode = material?.Code ?? "SIN-COD";
            string materialName = material?.Name ?? "Material sin catálogo";

            decimal totalLinearFeet = 0m;
            decimal totalSquareFeet = 0m;

            switch (rule.MeasureMode)
            {
                case MeasureMode.Length1D:
                    totalLinearFeet = dim1.LinearFeet * totalPieces;
                    break;

                case MeasureMode.Rect2D:
                    totalSquareFeet = MeasureFraction.CalculateSquareFeet(dim1, dim2) * totalPieces;
                    break;

                case MeasureMode.Perimeter1D:
                    totalLinearFeet = MeasureFraction.CalculatePerimeterFeet(dim1, dim2) * totalPieces;
                    break;
            }

            cutPieces.Add(new CutPieceResult(
                ItemId: input.ItemId,
                LineOrder: input.LineOrder,
                ModelName: model.Name,
                PartName: rule.PartName,
                MaterialId: rule.MaterialId,
                MaterialCode: materialCode,
                MaterialName: materialName,
                MeasureMode: rule.MeasureMode,
                Dim1: dim1,
                Dim2: dim2,
                Quantity: totalPieces,
                TotalLinearFeet: Math.Round(totalLinearFeet, 4, MidpointRounding.AwayFromZero),
                TotalSquareFeet: Math.Round(totalSquareFeet, 4, MidpointRounding.AwayFromZero)));
        }

        // --- 2. EVALUAR REGLAS DE HERRAJES Y ACCESORIOS ---
        foreach (var hwRule in model.HardwareRules)
        {
            if (hwRule.Condition is not null && !hwRule.Condition.Matches(input))
                continue;

            decimal baseValue = ResolveHardwareBasis(hwRule.CalculationBasis, input);
            decimal totalQuantity = baseValue * hwRule.Factor * input.Quantity;

            if (totalQuantity <= 0m)
                continue;

            materials.TryGetValue(hwRule.MaterialId, out var material);
            string materialCode = material?.Code ?? "SIN-COD";
            string materialName = material?.Name ?? "Herraje sin catálogo";

            hardwareItems.Add(new HardwareRequirementResult(
                ItemId: input.ItemId,
                LineOrder: input.LineOrder,
                MaterialId: hwRule.MaterialId,
                MaterialCode: materialCode,
                MaterialName: materialName,
                Quantity: Math.Round(totalQuantity, 4, MidpointRounding.AwayFromZero)));
        }

        return new ItemBreakdownResult(input, model, cutPieces, hardwareItems);
    }

    /// <summary>
    /// Calcula el despiece de una lista completa de ítems (orden rápida o proyecto mixto).
    /// </summary>
    public static IReadOnlyList<ItemBreakdownResult> CalculateOrder(
        IEnumerable<BreakdownItemInput> items,
        IReadOnlyDictionary<Guid, ProductModelDefinition> models,
        IReadOnlyDictionary<Guid, MaterialCatalogEntry> materials)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(models);
        ArgumentNullException.ThrowIfNull(materials);

        var results = new List<ItemBreakdownResult>();

        foreach (var item in items)
        {
            if (!models.TryGetValue(item.ModelId, out var model))
                continue;

            results.Add(CalculateItem(item, model, materials));
        }

        return results;
    }

    /// <summary>
    /// Fórmula declarativa de cada eje:
    ///   Resultado = ((EjeBase + (MultiplicadorGrosor * GrosorTablero)) / Divisor) + AjusteEnDieciseisavos
    ///
    /// Ejemplos:
    /// - Piso/Techo de gabinete: (Width + (-2 * 3/4")) / 1 + 0
    /// - Puertas de gabinete:    (Width + 0) / DoorCount + (-1/8")
    /// - Cabezal hoja ventana:   (Width + 0) / LeafCount + (-DescuentoPorHoja)
    /// </summary>
    private static MeasureFraction EvaluateDimension(
        DimensionAxis axis,
        DivisorSource divisorSource,
        decimal customDivisor,
        int offsetSixteenths,
        int thicknessMultiplier,
        int boardThicknessSixteenths,
        BreakdownItemInput input)
    {
        if (axis == DimensionAxis.None)
            return MeasureFraction.Zero;

        MeasureFraction baseMeasure = axis switch
        {
            DimensionAxis.Width => input.Width,
            DimensionAxis.Height => input.Height,
            DimensionAxis.Depth => input.Depth ?? MeasureFraction.Zero,
            _ => MeasureFraction.Zero
        };

        int adjustedBeforeDivide = baseMeasure.Sixteenths + (thicknessMultiplier * boardThicknessSixteenths);
        decimal divisor = ResolveDivisor(divisorSource, customDivisor, input);

        int dividedSixteenths = divisor == 1m
            ? adjustedBeforeDivide
            : (int)Math.Round(adjustedBeforeDivide / divisor, MidpointRounding.AwayFromZero);

        return new MeasureFraction(dividedSixteenths + offsetSixteenths);
    }

    private static decimal ResolveDivisor(
        DivisorSource source,
        decimal customDivisor,
        BreakdownItemInput input)
    {
        decimal value = source switch
        {
            DivisorSource.ConstantOne => 1m,
            DivisorSource.LeafCount => input.LeafCount,
            DivisorSource.DoorCount => input.DoorCount,
            DivisorSource.DrawerCount => input.DrawerCount,
            DivisorSource.ShelfCount => input.ShelfCount,
            DivisorSource.CustomConstant => customDivisor,
            _ => 1m
        };

        return value > 0m ? value : 1m;
    }

    private static int ResolveCutQuantity(
        QuantityMode mode,
        int factor,
        BreakdownItemInput input)
    {
        if (factor <= 0)
            return 0;

        int perUnitBase = mode switch
        {
            QuantityMode.Fixed => 1,
            QuantityMode.PerLeaf => input.LeafCount,
            QuantityMode.PerDoor => input.DoorCount,
            QuantityMode.PerDrawer => input.DrawerCount,
            QuantityMode.PerShelf => input.ShelfCount,
            _ => 0
        };

        return perUnitBase > 0 ? perUnitBase * factor * input.Quantity : 0;
    }

    private static decimal ResolveHardwareBasis(
        HardwareCalculationBasis basis,
        BreakdownItemInput input)
    {
        return basis switch
        {
            HardwareCalculationBasis.PerUnit => 1m,
            HardwareCalculationBasis.PerLeaf => Math.Max(0, input.LeafCount),
            HardwareCalculationBasis.PerDoor => Math.Max(0, input.DoorCount),
            HardwareCalculationBasis.PerDrawer => Math.Max(0, input.DrawerCount),
            HardwareCalculationBasis.PerShelf => Math.Max(0, input.ShelfCount),
            HardwareCalculationBasis.PerimeterFeet => MeasureFraction.CalculatePerimeterFeet(input.Width, input.Height),
            HardwareCalculationBasis.AreaSqFt => MeasureFraction.CalculateSquareFeet(input.Width, input.Height),
            _ => 0m
        };
    }
}