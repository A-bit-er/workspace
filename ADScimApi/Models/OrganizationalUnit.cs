namespace ADScimApi.Models;

public class OrganizationalUnit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<BusinessRule>? BusinessRules { get; set; }
}