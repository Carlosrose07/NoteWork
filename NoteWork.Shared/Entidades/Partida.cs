namespace NoteWork.Shared.Entidades;

/// <summary>
/// Una línea del presupuesto: lo que se compra o se instala
/// (ej. "plafón, hojas de 12 pies, 15 unidades = 3900").
/// Es el equivalente a cada renglón del cuaderno. Hereda de EntidadBase.
/// </summary>
public class Partida : EntidadBase
{
    // Área a la que pertenece. Obligatorio. Regla (se validará en código
    // más adelante): el nodo debe ser de tipo Area, no Piso ni Apartamento.
    public Guid NodoId { get; set; }

    // Qué es: "Plafón", "Puerta de pino", "Tornillos y masilla".
    // Es lo único de texto que es obligatorio, igual que en el cuaderno.
    public string Descripcion { get; set; } = string.Empty;

    // Texto libre para anotar lo que el usuario quiera: "hojas de 12 pies",
    // "color caoba", "parales". No tiene formato fijo A PROPÓSITO: en el cuaderno
    // cada quien escribe distinto, y obligar a llenar campos rígidos haría la
    // app más lenta que el cuaderno.
    public string? Especificacion { get; set; }

    // Opcionales, vienen del documento: algunos productos las usan y otros no.
    public string? Material { get; set; }
    public string? Marca { get; set; }

    // Cantidad: opcional, porque una línea como "clavos, tornillos y masilla = 1000"
    // no tiene cantidad. Es decimal para permitir cosas como 2.5 (metros, galones).
    public decimal? Cantidad { get; set; }

    // DATO PRINCIPAL: el total de la línea, tal como lo escribe el usuario.
    // Por qué el total y no el precio por unidad: el cuaderno guarda totales
    // ("15 hojas = 3900"). Con el total guardado, lo que se escribió es
    // exactamente lo que se muestra y se suma, sin errores de redondeo.
    // Se usa decimal (no double) porque double puede fallar en centavos.
    // La precisión de la columna (numeric) la configuramos al crear la base.
    public decimal TotalLinea { get; set; }

    // Precio por unidad: NO se guarda, se CALCULA. Como solo tiene "get" (sin "set"),
    // Entity Framework lo ignora y no crea columna en la base de datos.
    // Si no hay cantidad (o es 0), devuelve null en vez de dividir entre cero.
    public decimal? PrecioUnitario =>
        Cantidad is > 0 ? Math.Round(TotalLinea / Cantidad.Value, 2) : null;

    // Medidas en MILÍMETROS (número entero), siempre, sin importar si el usuario
    // las ve en pulgadas o centímetros. Solo cambia cómo se muestran.
    // Son opcionales: se usan en puertas y ventanas, no en "clavos y tornillos".
    public int? AnchoMm { get; set; }
    public int? AltoMm { get; set; }
    public int? ProfundidadMm { get; set; }

    // Descuento como MONTO en dinero (no porcentaje) que se resta del total.
    // Se guarda desde ya porque está en el modelo, pero se usará en V2.
    public decimal? Descuento { get; set; }

    // Campos INTERNOS: no salen en el PDF del cliente.
    // CostoInterno = lo que le costó comprar el material (para calcular ganancia).
    public decimal? CostoInterno { get; set; }
    public string? NotaInterna { get; set; }

    // Posición dentro del área (0, 1, 2...), para que la lista salga siempre
    // en el mismo orden y el usuario pueda reordenarla.
    public int Orden { get; set; }

    // Relación con el Área. No se carga sola: hay que pedirla expresamente.
    public Nodo? Nodo { get; set; }
}