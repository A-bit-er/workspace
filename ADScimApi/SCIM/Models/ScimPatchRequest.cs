using System.Text.Json.Serialization;

namespace ADScimApi.SCIM.Models;

public class ScimPatchRequest
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } = new List<string> { "urn:ietf:params:scim:api:messages:2.0:PatchOp" };
    
    [JsonPropertyName("Operations")]
    public List<ScimPatchOperation> Operations { get; set; } = new List<ScimPatchOperation>();
}

public class ScimPatchOperation
{
    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty; // add, remove, replace
    
    [JsonPropertyName("path")]
    public string? Path { get; set; }
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}