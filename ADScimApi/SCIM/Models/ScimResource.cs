using System.Text.Json.Serialization;

namespace ADScimApi.SCIM.Models;

public abstract class ScimResource
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } = new List<string>();
    
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("externalId")]
    public string? ExternalId { get; set; }
    
    [JsonPropertyName("meta")]
    public ScimMeta? Meta { get; set; }
}