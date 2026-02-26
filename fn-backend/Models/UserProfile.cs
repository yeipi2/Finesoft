// Models/UserProfile.cs
using System.ComponentModel.DataAnnotations;

namespace fn_backend.Models;

public class UserProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    // Base64 de avatar y portada
    public string? AvatarDataUrl { get; set; }
    public string? CoverDataUrl { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}