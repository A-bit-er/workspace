namespace ADScimApi.Models;

public class ActiveDirectoryObject
{
    public string ObjectGuid { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty; // User, Group, GMSA
    public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
}