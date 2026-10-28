using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Clientes;

/// <summary>
/// Datos que el Client le envía a la Api para CREAR o EDITAR un cliente.
/// Se usa la misma clase para las dos cosas porque los campos son iguales.
///
/// Es una clase con "set" (no un record con "init") porque más adelante la
/// pantalla de Clientes la enlazará a un formulario, y los record con init
/// no sirven para eso.
///
/// A PROPÓSITO no tiene Id, EmpresaId, fechas ni Eliminado:
/// - EmpresaId lo decide la Api leyendo el token (nunca se confía en el Client).
/// - Id lo trae la dirección del endpoint al editar (/api/clientes/{id}).
/// - Las fechas y Eliminado los maneja la Api.
/// </summary>
public class ClienteGuardarSolicitud
{
    // Único campo obligatorio, igual que en la entidad Cliente.
    // Required rechaza también un texto de solo espacios.
    // StringLength pone un tope para que nadie pueda mandar un texto gigante
    // que llene la base y haga lento el celular al listarlo.
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    // Opcional. Es texto (no número) para conservar "+", guiones o un 0 inicial.
    [StringLength(30, ErrorMessage = "El teléfono no puede pasar de 30 caracteres.")]
    public string? Telefono { get; set; }

    // Opcional. Dirección de la obra o del cliente.
    [StringLength(300, ErrorMessage = "La dirección no puede pasar de 300 caracteres.")]
    public string? Direccion { get; set; }

    // Opcional. Notas internas: no salen en el PDF.
    [StringLength(1000, ErrorMessage = "Las notas no pueden pasar de 1000 caracteres.")]
    public string? Notas { get; set; }
}