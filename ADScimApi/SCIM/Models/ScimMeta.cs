using System.Text.Json.Serialization;

namespace ADScimApi.SCIM.Models;

public class ScimMeta
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = string.Empty;
    
    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }
    
    [JsonPropertyName("lastModified")]
    public DateTime? LastModified { get; set; }
    
    [JsonPropertyName("location")]
    public string? Location { get; set; }
    
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}