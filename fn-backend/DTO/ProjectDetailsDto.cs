namespace fn_backend.DTO;

public class ProjectDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public bool IsActive { get; set; }

    public ClientDto? Client { get; set; }

    public List<ServiceDetailDto>? Services { get; set; }
}