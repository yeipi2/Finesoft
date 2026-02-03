using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class UserDto
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El rol es obligatorio")]
    public string RoleName { get; set; } = string.Empty;
}