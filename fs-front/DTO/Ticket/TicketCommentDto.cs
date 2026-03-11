using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class TicketCommentDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El comentario es obligatorio")]
    public string Comment { get; set; } = string.Empty;

    public bool IsInternal { get; set; } = false;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime? CreatedAt { get; set; }
}