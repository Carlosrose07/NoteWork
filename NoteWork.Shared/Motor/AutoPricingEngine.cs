namespace NoteWork.Shared.Motor;

/// <summary>
/// Motor universal de consolidación de materiales y presupuesto automático.
/// Calcula en memoria (C# puro) el consolidado de compras para el taller y el
/// precio de venta al cliente (materiales + desperdicio + mano de obra + margen - descuento + ITBIS).
/// </summary>
public static class AutoPricingEngine
{
    /// <summary>
    /// Ejecuta el flujo completo en un solo paso:
    /// Desglose de cortes/herrajes -> Consolidado de materiales -> Presupuesto automático.
    /// </summary>
    public static OrderPricingResult CalculateOrder(
        IEnumerable<BreakdownItemInput> items,
        IReadOnlyDictionary<Guid, ProductModelDefinition> models,
        IReadOnlyDictionary<Guid, MaterialCatalogEntry> materials,
        decimal discountPercent = 0m,
        decimal taxRatePercent = 0m,
        bool chargeWholeCommercialUnits = false)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(models);
        ArgumentNullException.ThrowIfNull(materials);

        var breakdowns = UniversalBreakdownEngine.CalculateOrder(items, models, materials);

        return CalculateFromBreakdowns(
            breakdowns,
            materials,
            discountPercent,
            taxRatePercent,
            chargeWholeCommercialUnits);
    }

    /// <summary>
    /// Calcula el consolidado de materiales y el presupuesto a partir de un desglose ya ejecutado.
    /// </summary>
    public static OrderPricingResult CalculateFromBreakdowns(
        IReadOnlyList<ItemBreakdownResult> breakdowns,
        IReadOnlyDictionary<Guid, MaterialCatalogEntry> materials,
        decimal discountPercent = 0m,
        decimal taxRatePercent = 0m,
        bool chargeWholeCommercialUnits = false)
    {
        ArgumentNullException.ThrowIfNull(breakdowns);
        ArgumentNullException.ThrowIfNull(materials);

        // 1. Consolidar materiales a comprar para toda la orden/proyecto
        var consolidatedMaterials = ConsolidateMaterials(
            breakdowns,
            materials,
            chargeWholeCommercialUnits);

        // 2. Calcular el precio individual de cada ítem (ventana, puerta, gabinete, closet)
        var itemPrices = new List<ItemPriceResult>(breakdowns.Count);

        foreach (var breakdown in breakdowns)
        {
            itemPrices.Add(CalculateItemPrice(
                breakdown,
                materials,
                chargeWholeCommercialUnits));
        }

        // 3. Totales generales del presupuesto
        decimal subtotalMaterials = RoundMoney(itemPrices.Sum(p => p.MaterialCost));
        decimal subtotalLabor = RoundMoney(itemPrices.Sum(p => p.LaborCost));
        decimal subtotalBeforeDiscount = RoundMoney(itemPrices.Sum(p => p.LineTotal));

        decimal safeDiscountPercent = Math.Clamp(discountPercent, 0m, 100m);
        decimal discountAmount = RoundMoney(subtotalBeforeDiscount * (safeDiscountPercent / 100m));
        if (discountAmount > subtotalBeforeDiscount)
            discountAmount = subtotalBeforeDiscount;

        decimal subtotalAfterDiscount = RoundMoney(subtotalBeforeDiscount - discountAmount);

        decimal safeTaxRate = Math.Max(0m, taxRatePercent);
        decimal taxAmount = RoundMoney(subtotalAfterDiscount * (safeTaxRate / 100m));
        decimal totalQuoteAmount = RoundMoney(subtotalAfterDiscount + taxAmount);

        return new OrderPricingResult(
            ItemBreakdowns: breakdowns,
            ConsolidatedMaterials: consolidatedMaterials,
            ItemPrices: itemPrices,
            SubtotalMaterials: subtotalMaterials,
            SubtotalLabor: subtotalLabor,
            SubtotalBeforeDiscount: subtotalBeforeDiscount,
            DiscountPercent: safeDiscountPercent,
            DiscountAmount: discountAmount,
            SubtotalAfterDiscount: subtotalAfterDiscount,
            TaxRatePercent: safeTaxRate,
            TaxAmount: taxAmount,
            TotalQuoteAmount: totalQuoteAmount);
    }

    /// <summary>
    /// Agrupa todas las piezas y herrajes por MaterialId para obtener la lista real de compras.
    /// </summary>
    public static IReadOnlyList<ConsolidatedMaterialResult> ConsolidateMaterials(
        IReadOnlyList<ItemBreakdownResult> breakdowns,
        IReadOnlyDictionary<Guid, MaterialCatalogEntry> materials,
        bool chargeWholeCommercialUnits = false)
    {
        var rawTotalsByMaterial = new Dictionary<Guid, decimal>();

        foreach (var breakdown in breakdowns)
        {
            AccumulateItemRawMaterials(breakdown, rawTotalsByMaterial);
        }

        var result = new List<ConsolidatedMaterialResult>(rawTotalsByMaterial.Count);

        foreach (var (materialId, rawQty) in rawTotalsByMaterial)
        {
            if (rawQty <= 0m)
                continue;

            materials.TryGetValue(materialId, out var material);

            string code = material?.Code ?? "SIN-COD";
            string name = material?.Name ?? "Material sin catálogo";
            MaterialType type = material?.MaterialType ?? MaterialType.Unit;
            decimal wastePercent = Math.Max(0m, material?.DefaultWastePercent ?? 0m);
            decimal unitCost = Math.Max(0m, material?.UnitCost ?? 0m);

            decimal qtyWithWaste = rawQty * (1m + (wastePercent / 100m));
            var (exactUnits, wholeUnits, unitLabel) = ConvertToCommercialUnits(qtyWithWaste, material, type);

            decimal billableUnits = chargeWholeCommercialUnits ? wholeUnits : exactUnits;
            decimal totalCost = RoundMoney(billableUnits * unitCost);

            result.Add(new ConsolidatedMaterialResult(
                MaterialId: materialId,
                MaterialCode: code,
                MaterialName: name,
                MaterialType: type,
                RawQuantity: RoundQuantity(rawQty),
                WastePercent: wastePercent,
                QuantityWithWaste: RoundQuantity(qtyWithWaste),
                CommercialUnitsExact: RoundQuantity(exactUnits),
                CommercialUnitsWhole: wholeUnits,
                CommercialUnitLabel: unitLabel,
                UnitCost: unitCost,
                TotalCost: totalCost));
        }

        // Orden limpio para el taller: primero perfiles 1D, luego planchas 2D, luego herrajes
        return result
            .OrderBy(m => m.MaterialType)
            .ThenBy(m => m.MaterialName)
            .ToList();
    }

    private static ItemPriceResult CalculateItemPrice(
        ItemBreakdownResult breakdown,
        IReadOnlyDictionary<Guid, MaterialCatalogEntry> materials,
        bool chargeWholeCommercialUnits)
    {
        var input = breakdown.Input;
        var model = breakdown.Model;

        if (input.Quantity <= 0)
        {
            return new ItemPriceResult(
                ItemId: input.ItemId,
                LineOrder: input.LineOrder,
                Quantity: 0,
                MaterialCost: 0m,
                LaborCost: 0m,
                MarginPercent: 0m,
                UnitPrice: 0m,
                LineTotal: 0m);
        }

        // 1. Costo de materiales propios de este ítem
        var itemRawMaterials = new Dictionary<Guid, decimal>();
        AccumulateItemRawMaterials(breakdown, itemRawMaterials);

        decimal materialCost = 0m;
        foreach (var (materialId, rawQty) in itemRawMaterials)
        {
            if (rawQty <= 0m || !materials.TryGetValue(materialId, out var material))
                continue;

            decimal wastePercent = Math.Max(0m, material.DefaultWastePercent);
            decimal qtyWithWaste = rawQty * (1m + (wastePercent / 100m));
            var (exactUnits, wholeUnits, _) = ConvertToCommercialUnits(qtyWithWaste, material, material.MaterialType);

            decimal billableUnits = chargeWholeCommercialUnits ? wholeUnits : exactUnits;
            materialCost += billableUnits * Math.Max(0m, material.UnitCost);
        }

        materialCost = RoundMoney(materialCost);

        // 2. Mano de obra según el modo configurado en el modelo
        decimal laborCost = CalculateLaborCost(input, model, materialCost);

        // 3. Margen de ganancia (acepta sobrescritura personalizada por ítem)
        decimal marginPercent = Math.Max(0m, input.CustomMarginPercent ?? model.DefaultMarginPercent);

        decimal baseCost = materialCost + laborCost;
        decimal rawLineTotal = baseCost * (1m + (marginPercent / 100m));

        // Redondeamos el precio unitario primero para que Cantidad × Precio Unitario = Total Línea exacto en el PDF
        decimal unitPrice = RoundMoney(rawLineTotal / input.Quantity);
        decimal lineTotal = RoundMoney(unitPrice * input.Quantity);

        return new ItemPriceResult(
            ItemId: input.ItemId,
            LineOrder: input.LineOrder,
            Quantity: input.Quantity,
            MaterialCost: materialCost,
            LaborCost: laborCost,
            MarginPercent: marginPercent,
            UnitPrice: unitPrice,
            LineTotal: lineTotal);
    }

    private static void AccumulateItemRawMaterials(
        ItemBreakdownResult breakdown,
        Dictionary<Guid, decimal> accumulator)
    {
        foreach (var piece in breakdown.CutPieces)
        {
            decimal qty = piece.MeasureMode == MeasureMode.Rect2D
                ? piece.TotalSquareFeet
                : piece.TotalLinearFeet;

            if (qty <= 0m)
                continue;

            accumulator[piece.MaterialId] = accumulator.TryGetValue(piece.MaterialId, out var current)
                ? current + qty
                : qty;
        }

        foreach (var hw in breakdown.HardwareItems)
        {
            if (hw.Quantity <= 0m)
                continue;

            accumulator[hw.MaterialId] = accumulator.TryGetValue(hw.MaterialId, out var current)
                ? current + hw.Quantity
                : hw.Quantity;
        }
    }

    private static (decimal ExactUnits, int WholeUnits, string Label) ConvertToCommercialUnits(
        decimal quantityWithWaste,
        MaterialCatalogEntry? material,
        MaterialType type)
    {
        switch (type)
        {
            case MaterialType.Linear1D:
                // Si el material tiene largo comercial (ej. barra de 252" = 21 pies), convertimos a barras.
                // Si no tiene largo comercial (ej. goma o tapacanto), se mantiene en pies lineales.
                if (material?.CommercialLengthInches is > 0m)
                {
                    decimal barFeet = material.CommercialLengthInches.Value / 12m;
                    decimal exactBars = quantityWithWaste / barFeet;
                    return (exactBars, (int)Math.Ceiling(exactBars), "barras");
                }
                return (quantityWithWaste, (int)Math.Ceiling(quantityWithWaste), "pies");

            case MaterialType.Sheet2D:
                // Si tiene formato de plancha (ej. 48"×96" o 130"×84"), convertimos pies² a planchas.
                if (material?.CommercialWidthInches is > 0m && material?.CommercialHeightInches is > 0m)
                {
                    decimal sheetSqFt = (material.CommercialWidthInches.Value * material.CommercialHeightInches.Value) / 144m;
                    decimal exactSheets = quantityWithWaste / sheetSqFt;
                    return (exactSheets, (int)Math.Ceiling(exactSheets), "planchas");
                }
                return (quantityWithWaste, (int)Math.Ceiling(quantityWithWaste), "pies²");

            default:
                return (quantityWithWaste, (int)Math.Ceiling(quantityWithWaste), "und");
        }
    }

    private static decimal CalculateLaborCost(
        BreakdownItemInput input,
        ProductModelDefinition model,
        decimal materialCost)
    {
        decimal laborValue = Math.Max(0m, model.DefaultLaborValue);

        decimal rawLabor = model.DefaultLaborMode switch
        {
            LaborMode.Fixed => laborValue * input.Quantity,
            LaborMode.PerSqFt => MeasureFraction.CalculateSquareFeet(input.Width, input.Height) * laborValue * input.Quantity,
            LaborMode.Percent => materialCost * (laborValue / 100m),
            _ => 0m
        };

        return RoundMoney(rawLabor);
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal RoundQuantity(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}