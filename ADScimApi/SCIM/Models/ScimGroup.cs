using System.Text.Json.Serialization;

namespace ADScimApi.SCIM.Models;

public class ScimGroup : ScimResource
{
    public static readonly string SchemaUri = "urn:ietf:params:scim:schemas:core:2.0:Group";
    
    public ScimGroup()
    {
        Schemas.Add(SchemaUri);
    }
    
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;
    
    [JsonPropertyName("members")]
    public List<ScimMemberRef>? Members { get; set; }
}

public class ScimMemberRef
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }
    
    [JsonPropertyName("display")]
    public string? Display { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}