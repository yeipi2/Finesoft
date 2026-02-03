using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class ProjectDto
{
    public int? Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required] public int ClientId { get; set; }
    public bool IsActive { get; set; } = true;
}