using System.Text.Json.Serialization;

namespace ADScimApi.SCIM.Models;

public class ScimListResponse<T> where T : ScimResource
{
    [JsonPropertyName("schemas")]
    public List<string> Schemas { get; set; } = new List<string> { "urn:ietf:params:scim:api:messages:2.0:ListResponse" };
    
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }
    
    [JsonPropertyName("startIndex")]
    public int StartIndex { get; set; } = 1;
    
    [JsonPropertyName("itemsPerPage")]
    public int ItemsPerPage { get; set; }
    
    [JsonPropertyName("Resources")]
    public List<T> Resources { get; set; } = new List<T>();
}