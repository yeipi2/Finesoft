using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class UserDto
{
    public string? Id { get; set; }

    [Required] public string UserName { get; set; } = string.Empty;

    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [MinLength(6)] public string? Password { get; set; }

    [Required] public string RoleName { get; set; } = string.Empty;
}