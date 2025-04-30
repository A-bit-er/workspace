namespace ADScimApi.Models;

public class BusinessRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty; // User, Group, GMSA
    public string Condition { get; set; } = string.Empty; // JSON condition
    public int OrganizationalUnitId { get; set; }
    public OrganizationalUnit? OrganizationalUnit { get; set; }
    public string TargetOU { get; set; } = string.Empty; // Distinguished name of target OU
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}