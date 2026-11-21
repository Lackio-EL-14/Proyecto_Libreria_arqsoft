using System.ComponentModel.DataAnnotations;

namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record CreateUserRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    string FirstName,
    [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
    string LastName,
    [Required(ErrorMessage = "El correo es obligatorio.")]
    string Email,
    [Required(ErrorMessage = "Debe seleccionar un rol válido.")]
    string Role,
    string? MaternalLastName = null);
