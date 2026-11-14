namespace NoteWork.Shared.Entidades;

/// <summary>
/// La persona o empresa a quien se le hace el presupuesto.
/// Hereda de EntidadBase (Id, EmpresaId, fechas y borrado lógico).
/// </summary>
public class Cliente : EntidadBase
{
    // Es lo único obligatorio. En el cuaderno basta con un nombre para
    // identificar el trabajo, así que no pedimos más para no frenar al
    // usuario cuando está en obra con el celular en la mano.
    public string Nombre { get; set; } = string.Empty;

    // NUEVO (B1): dueño del cliente = el usuario que lo creó.
    // Es un Guid simple, sin clave foránea a propósito (no enlaza con la clase Usuario).
    // El usuario normal solo ve sus clientes; el administrador ve todos los de su empresa.
    // El dueño nunca cambia, ni siquiera cuando el administrador edita el cliente.
    public Guid UsuarioId { get; set; }

    // Opcional, pero es el dato más útil: el PDF se envía por WhatsApp.
    // Se guarda como texto (string) y no como número, porque un teléfono
    // puede empezar con 0 o llevar "+" y guiones, y un número perdería eso.
    public string? Telefono { get; set; }

    // Dirección de la obra o del cliente. Opcional.
    public string? Direccion { get; set; }

    // Notas libres sobre el cliente ("prefiere que lo llamen por la tarde").
    // Son internas: no salen en el PDF.
    public string? Notas { get; set; }

    // Un cliente puede tener muchos proyectos. Esta lista permite que EF Core
    // entienda la relación "uno a muchos" (un Cliente, varios Proyectos).
    // Se inicia vacía para evitar errores de valor nulo.
    // Ojo por rendimiento: esta lista NO se llena sola. Solo se carga cuando
    // se pide expresamente, así el celular no recibe datos que no necesita.
    public List<Proyecto> Proyectos { get; set; } = new();
}