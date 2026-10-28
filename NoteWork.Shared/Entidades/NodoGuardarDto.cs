using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Entidades;

/// <summary>
/// DTO para crear o actualizar un nodo (Piso, Apartamento o Área).
/// </summary>
public class NodoGuardarDto
{
    [Required(ErrorMessage = "El proyecto es obligatorio.")]
    public Guid ProyectoId { get; set; }

    // Es nulo si el nodo cuelga directamente de la raíz del proyecto
    public Guid? PadreId { get; set; }

    [Required(ErrorMessage = "El tipo de nodo es obligatorio.")]
    public TipoNodo Tipo { get; set; } = TipoNodo.Area;

    [Required(ErrorMessage = "El nombre del nodo es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public int Orden { get; set; }
}