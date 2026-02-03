using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class TypeActivityDto
{
    public int? Id { get; set; }
    
    [Required(ErrorMessage = "El nombre es obligatorio")]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
}