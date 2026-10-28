namespace NoteWork.Shared.Entidades;

/// <summary>
/// Clase base de todas las entidades de NoteWork.
/// Contiene los 5 campos que, según el documento del proyecto, lleva TODA tabla.
/// "abstract" significa que no se puede crear una EntidadBase suelta: solo sirve
/// para que otras clases (Empresa, Cliente, Proyecto...) hereden de ella.
/// </summary>
public abstract class EntidadBase
{
    // Id tipo GUID generado por la app (no por la base de datos).
    // Por qué: cuando agreguemos el modo offline (V3), el celular podrá crear
    // registros sin internet y los IDs nunca chocarán con los de otros usuarios.
    // Con números consecutivos (1, 2, 3...) sí chocarían.
    public Guid Id { get; set; } = Guid.NewGuid();

    // Preparado para varias empresas desde el día 1.
    // Hoy solo existe la empresa del hermano, pero así no hay que
    // reescribir las tablas cuando entre una segunda empresa.
    public Guid EmpresaId { get; set; }

    // Se usa DateTime.UtcNow (hora universal) y no DateTime.Now (hora local).
    // Por qué: PostgreSQL con Npgsql exige fechas en UTC para el tipo
    // "timestamp with time zone", y además evita líos si el celular y el
    // servidor están en zonas horarias distintas.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Clave para la sincronización offline de V3: permitirá saber qué registro
    // es más reciente. Debemos actualizarla cada vez que se modifique algo.
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    // Borrado lógico: en vez de borrar la fila, la marcamos como eliminada.
    // Por qué: en la sincronización offline, si borráramos de verdad, el celular
    // no se enteraría de que ese registro ya no existe. Además permite recuperar
    // un presupuesto borrado por error.
    public bool Eliminado { get; set; }
}