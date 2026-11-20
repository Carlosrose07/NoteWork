using System.Globalization;

namespace NoteWork.Shared.Motor;

/// <summary>
/// Representa una medida exacta en dieciseisavos de pulgada (1/16").
/// Es una estructura inmutable (readonly record struct) para evitar asignaciones
/// en memoria y mantener cálculos instantáneos en Blazor WASM (iPhone 11 / PC modesta).
/// </summary>
public readonly record struct MeasureFraction(int Sixteenths) : IComparable<MeasureFraction>
{
    public const int SixteenthsPerInch = 16;
    public const int SixteenthsPerFoot = 192; // 12 pulgadas * 16 dieciseisavos

    public static MeasureFraction Zero => new(0);

    /// <summary>Pulgadas enteras (con signo).</summary>
    public int WholeInches => Sixteenths / SixteenthsPerInch;

    /// <summary>Residuo en dieciseisavos (siempre entre 0 y 15).</summary>
    public int RemainderSixteenths => Math.Abs(Sixteenths % SixteenthsPerInch);

    /// <summary>Valor exacto en pulgadas decimales.</summary>
    public decimal DecimalInches => Sixteenths / (decimal)SixteenthsPerInch;

    /// <summary>Valor exacto en pies lineales (ft).</summary>
    public decimal LinearFeet => Sixteenths / (decimal)SixteenthsPerFoot;

    /// <summary>Conversión aproximada a milímetros (1" = 25.4 mm).</summary>
    public decimal Millimeters => DecimalInches * 25.4m;

    /// <summary>
    /// Crea una medida a partir de pulgadas enteras y fracción opcional (ej. 40, 1, 2 -> 40 1/2").
    /// </summary>
    public static MeasureFraction FromInches(int wholeInches, int numerator = 0, int denominator = 16)
    {
        if (denominator <= 0)
            throw new ArgumentOutOfRangeException(nameof(denominator), "El denominador debe ser mayor que cero.");

        int fractionSixteenths = (int)Math.Round(
            numerator * (decimal)SixteenthsPerInch / denominator,
            MidpointRounding.AwayFromZero);

        if (wholeInches < 0)
        {
            return new MeasureFraction(-(Math.Abs(wholeInches) * SixteenthsPerInch + Math.Abs(fractionSixteenths)));
        }

        return new MeasureFraction(wholeInches * SixteenthsPerInch + fractionSixteenths);
    }

    /// <summary>Crea una medida redondeando pulgadas decimales al 1/16" más cercano.</summary>
    public static MeasureFraction FromDecimalInches(decimal inches)
    {
        int sixteenths = (int)Math.Round(inches * SixteenthsPerInch, MidpointRounding.AwayFromZero);
        return new MeasureFraction(sixteenths);
    }

    /// <summary>Crea una medida a partir de milímetros redondeando al 1/16" más cercano.</summary>
    public static MeasureFraction FromMillimeters(decimal millimeters) =>
        FromDecimalInches(millimeters / 25.4m);

    /// <summary>Calcula el área en pies cuadrados (ft²) sin pérdida intermedia de precisión.</summary>
    public static decimal CalculateSquareFeet(MeasureFraction width, MeasureFraction height)
    {
        if (width.Sixteenths <= 0 || height.Sixteenths <= 0)
            return 0m;

        return (decimal)width.Sixteenths * height.Sixteenths / (SixteenthsPerFoot * SixteenthsPerFoot);
    }

    /// <summary>Calcula el perímetro en pies lineales (ft) de un rectángulo Ancho × Alto.</summary>
    public static decimal CalculatePerimeterFeet(MeasureFraction width, MeasureFraction height)
    {
        if (width.Sixteenths <= 0 && height.Sixteenths <= 0)
            return 0m;

        return ((width.Sixteenths + height.Sixteenths) * 2m) / SixteenthsPerFoot;
    }

    /// <summary>
    /// Convierte texto de taller a MeasureFraction.
    /// Acepta formatos como: "40 1/2", "40 1/2\"", "40-1/2", "40.5", "3/4", "36".
    /// </summary>
    public static bool TryParse(string? text, out MeasureFraction result)
    {
        result = Zero;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string cleaned = text.Trim().Replace("\"", "").Replace("''", "").Trim();
        if (cleaned.Length == 0)
            return false;

        bool isNegative = cleaned.StartsWith('-');
        if (isNegative)
            cleaned = cleaned[1..].Trim();

        // Caso 1: número entero o decimal directo (ej. "40" o "40.5")
        if (!cleaned.Contains('/'))
        {
            if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decVal))
            {
                result = FromDecimalInches(isNegative ? -decVal : decVal);
                return true;
            }
            return false;
        }

        // Caso 2: entero + fracción (ej. "40 1/2" o "40-1/2") o solo fracción ("3/4")
        string[] parts = cleaned.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        int whole = 0;
        string fractionPart;

        if (parts.Length == 1)
        {
            fractionPart = parts[0];
        }
        else if (parts.Length == 2 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out whole))
        {
            fractionPart = parts[1];
        }
        else
        {
            return false;
        }

        string[] fracTokens = fractionPart.Split('/');
        if (fracTokens.Length != 2
            || !int.TryParse(fracTokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int num)
            || !int.TryParse(fracTokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int den)
            || den <= 0)
        {
            return false;
        }

        var parsed = FromInches(whole, num, den);
        result = isNegative ? new MeasureFraction(-parsed.Sixteenths) : parsed;
        return true;
    }

    // --- Operadores aritméticos exactos ---

    public static MeasureFraction operator +(MeasureFraction a, MeasureFraction b) =>
        new(a.Sixteenths + b.Sixteenths);

    public static MeasureFraction operator -(MeasureFraction a, MeasureFraction b) =>
        new(a.Sixteenths - b.Sixteenths);

    public static MeasureFraction operator -(MeasureFraction a) =>
        new(-a.Sixteenths);

    public static MeasureFraction operator *(MeasureFraction a, int factor) =>
        new(a.Sixteenths * factor);

    public static MeasureFraction operator *(int factor, MeasureFraction a) =>
        new(a.Sixteenths * factor);

    public static MeasureFraction operator *(MeasureFraction a, decimal factor) =>
        new((int)Math.Round(a.Sixteenths * factor, MidpointRounding.AwayFromZero));

    public static MeasureFraction operator /(MeasureFraction a, int divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("No se puede dividir una medida entre cero.");

        return new((int)Math.Round(a.Sixteenths / (decimal)divisor, MidpointRounding.AwayFromZero));
    }

    public static MeasureFraction operator /(MeasureFraction a, decimal divisor)
    {
        if (divisor == 0m)
            throw new DivideByZeroException("No se puede dividir una medida entre cero.");

        return new((int)Math.Round(a.Sixteenths / divisor, MidpointRounding.AwayFromZero));
    }

    // --- Comparaciones ---

    public int CompareTo(MeasureFraction other) => Sixteenths.CompareTo(other.Sixteenths);

    public static bool operator <(MeasureFraction a, MeasureFraction b) => a.Sixteenths < b.Sixteenths;
    public static bool operator >(MeasureFraction a, MeasureFraction b) => a.Sixteenths > b.Sixteenths;
    public static bool operator <=(MeasureFraction a, MeasureFraction b) => a.Sixteenths <= b.Sixteenths;
    public static bool operator >=(MeasureFraction a, MeasureFraction b) => a.Sixteenths >= b.Sixteenths;

    /// <summary>
    /// Formatea la medida simplificada por MCD (ej. 40 8/16 -> 40 1/2", 36 0/16 -> 36", 12/16 -> 3/4").
    /// </summary>
    public override string ToString()
    {
        if (Sixteenths == 0)
            return "0\"";

        string sign = Sixteenths < 0 ? "-" : "";
        int abs = Math.Abs(Sixteenths);
        int whole = abs / SixteenthsPerInch;
        int rem = abs % SixteenthsPerInch;

        if (rem == 0)
            return $"{sign}{whole}\"";

        int gcd = GreatestCommonDivisor(rem, SixteenthsPerInch);
        int num = rem / gcd;
        int den = SixteenthsPerInch / gcd;

        return whole == 0
            ? $"{sign}{num}/{den}\""
            : $"{sign}{whole} {num}/{den}\"";
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
}