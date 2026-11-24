using NoteWork.Shared.Entidades;

namespace NoteWork.Shared.Dtos;

public sealed record EmpresaDto(
    Guid Id,
    string Nombre,
    string? Subtitulo,
    string? Rnc,
    string? LogoRuta,
    string? Telefono,
    string? Direccion,
    string Moneda,
    UnidadMedida UnidadPorDefecto);

public sealed record GuardarEmpresaDto(
    string Nombre,
    string? Subtitulo,
    string? Rnc,
    string? LogoRuta,
    string? Telefono,
    string? Direccion,
    string Moneda,
    UnidadMedida UnidadPorDefecto);