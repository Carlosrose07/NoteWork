namespace NoteWork.Shared.Clientes;

/// <summary>
/// Una fila de la LISTA de clientes. Es una versión reducida de Cliente:
/// solo trae lo que la lista necesita dibujar.
///
/// Por qué existe (rendimiento): si la Api devolviera la entidad Cliente
/// completa, cada fila viajaría con Direccion, Notas, fechas, EmpresaId, etc.
/// En una lista de 50 clientes eso es mucho dato inútil para un celular modesto.
/// Con esta clase solo viaja lo mínimo.
///
/// Es un record porque solo se LEE (la Api lo llena y la pantalla lo muestra);
/// nunca se enlaza a un formulario, así que "init" no es problema aquí.
/// </summary>
/// <param name="Id">Identificador del cliente. La pantalla lo usa para abrirlo al tocar la fila.</param>
/// <param name="Nombre">Nombre que se muestra en la lista.</param>
/// <param name="Telefono">Opcional. Se muestra debajo del nombre si existe.</param>
public record ClienteResumen(Guid Id, string Nombre, string? Telefono);