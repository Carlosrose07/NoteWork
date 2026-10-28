namespace NoteWork.Shared.Comun;

/// <summary>
/// Una PÁGINA de resultados de cualquier lista (clientes, proyectos, etc.).
///
/// Por qué existe (rendimiento): si un día hay 500 clientes, mandarlos todos
/// en una sola respuesta haría lenta la carga en un celular modesto.
/// En cambio la Api manda 20 por vez y esta clase dice cuántos hay en total.
///
/// El "<T>" significa "de cualquier tipo": PaginaResultado&lt;ClienteResumen&gt;
/// es una página de clientes, y más adelante PaginaResultado&lt;ProyectoResumen&gt;
/// será una página de proyectos, sin escribir otra clase.
///
/// Es un record porque solo se LEE: la Api lo llena y la pantalla lo muestra.
/// </summary>
/// <param name="Items">Los elementos de esta página (por ejemplo, hasta 20 clientes).</param>
/// <param name="Total">Cuántos elementos existen en total (no solo en esta página).</param>
/// <param name="Pagina">Número de esta página; la primera es la 1.</param>
/// <param name="TamanoPagina">Cuántos elementos caben por página.</param>
public record PaginaResultado<T>(List<T> Items, int Total, int Pagina, int TamanoPagina)
{
    // Calculado, no se guarda ni viaja: la pantalla lo usa para saber si
    // mostrar el botón "Cargar más". Hay más si lo que ya vimos hasta esta
    // página es menor que el total.
    public bool HayMas => Pagina * TamanoPagina < Total;
}