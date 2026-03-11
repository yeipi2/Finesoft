namespace fs_front.DTO;

public class ProjectDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public bool IsActive { get; set; }
    
    // Datos del cliente
    public ClientDto? Client { get; set; }
    
    // Lista de servicios (opcional)
    public List<ServiceDetailDto>? Services { get; set; }
}