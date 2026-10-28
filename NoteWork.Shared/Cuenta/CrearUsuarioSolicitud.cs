using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Cuenta;

/// <summary>
/// Datos que envía un admin para crear un usuario nuevo en SU empresa.
/// OJO: no incluye EmpresaId ni Rol a propósito. La Api toma la empresa
/// del admin autenticado y asigna el rol "Usuario" por su cuenta, así
/// nadie puede mandar esos datos desde fuera para saltarse las reglas.
/// </summary>
public class CrearUsuarioSolicitud
{
    // Se usa para iniciar sesión y, más adelante, para recuperar la contraseña.
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    // Nombre que se mostrará en pantalla (ej. "Juan Pérez").
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    // Contraseña en texto solo mientras viaja a la Api; Identity guarda
    // únicamente su hash. Esta validación es una primera barrera rápida
    // (se revisa en el celular sin llamar al servidor); Identity vuelve a
    // validar las reglas completas de complejidad en la Api.
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;
}
