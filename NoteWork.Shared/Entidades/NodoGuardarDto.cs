using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Entidades;

/// <summary>
/// DTO para crear o actualizar un nodo (Piso, Apartamento o Área).
/// </summary>
public class NodoGuardarDto
{
    // [Required] en un Guid: ValidacionFiltro (en la Api) también rechaza Guid.Empty.
    [Required(ErrorMessage = "El proyecto es obligatorio.")]
    public Guid ProyectoId { get; set; }

    // Es nulo si el nodo cuelga directamente de la raíz del proyecto
    public Guid? PadreId { get; set; }

    // CAMBIO (A2, hallazgo 2.1): [EnumDataType] rechaza números que no existen en TipoNodo
    // (por ejemplo 99). Sin esto, la Api aceptaba cualquier número como "tipo".
    [Required(ErrorMessage = "El tipo de nodo es obligatorio.")]
    [EnumDataType(typeof(TipoNodo), ErrorMessage = "El tipo de nodo no es válido.")]
    public TipoNodo Tipo { get; set; } = TipoNodo.Area;

    [Required(ErrorMessage = "El nombre del nodo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    // CAMBIO (A2, hallazgo 2.1): antes aceptaba cualquier número (negativos o enormes).
    [Range(0, 100000, ErrorMessage = "El orden debe ser un número entre 0 y 100000.")]
    public int Orden { get; set; }
}
