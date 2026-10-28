namespace NoteWork.Shared.Entidades;

/// <summary>
/// DTO para transferir información de un nodo (Piso, Apartamento o Área)
/// entre la API y el cliente, incluyendo su jerarquía.
/// </summary>
public class NodoDto
{
    public Guid Id { get; set; }
    public Guid ProyectoId { get; set; }
    public Guid? PadreId { get; set; }
    public TipoNodo Tipo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }

    // Subnodos directos (ej. los Apartamentos dentro de un Piso)
    public List<NodoDto> Hijos { get; set; } = new();

    // Cantidad de partidas asociadas (útil para mostrar conteos en la interfaz sin cargar todas las partidas)
    public int TotalPartidas { get; set; }

    // NUEVO: Monto acumulado de todas las partidas dentro de este nodo y sus hijos
    public decimal Subtotal { get; set; }
}