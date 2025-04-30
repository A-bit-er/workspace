using System.Text.Json.Serialization;

namespace ADScimApi.SCIM.Models;

public class ScimUser : ScimResource
{
    public static readonly string SchemaUri = "urn:ietf:params:scim:schemas:core:2.0:User";
    public static readonly string ExtensionSchemaUri = "urn:ietf:params:scim:schemas:extension:enterprise:2.0:User";
    
    public ScimUser()
    {
        Schemas.Add(SchemaUri);
    }
    
    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public ScimName? Name { get; set; }
    
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    
    [JsonPropertyName("nickName")]
    public string? NickName { get; set; }
    
    [JsonPropertyName("profileUrl")]
    public string? ProfileUrl { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("userType")]
    public string? UserType { get; set; }
    
    [JsonPropertyName("preferredLanguage")]
    public string? PreferredLanguage { get; set; }
    
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
    
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }
    
    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;
    
    [JsonPropertyName("password")]
    public string? Password { get; set; }
    
    [JsonPropertyName("emails")]
    public List<ScimEmail>? Emails { get; set; }
    
    [JsonPropertyName("phoneNumbers")]
    public List<ScimPhoneNumber>? PhoneNumbers { get; set; }
    
    [JsonPropertyName("addresses")]
    public List<ScimAddress>? Addresses { get; set; }
    
    [JsonPropertyName("groups")]
    public List<ScimGroupRef>? Groups { get; set; }
    
    [JsonPropertyName("urn:ietf:params:scim:schemas:extension:enterprise:2.0:User")]
    public ScimEnterpriseUser? EnterpriseUser { get; set; }
}

public class ScimName
{
    [JsonPropertyName("formatted")]
    public string? Formatted { get; set; }
    
    [JsonPropertyName("familyName")]
    public string? FamilyName { get; set; }
    
    [JsonPropertyName("givenName")]
    public string? GivenName { get; set; }
    
    [JsonPropertyName("middleName")]
    public string? MiddleName { get; set; }
    
    [JsonPropertyName("honorificPrefix")]
    public string? HonorificPrefix { get; set; }
    
    [JsonPropertyName("honorificSuffix")]
    public string? HonorificSuffix { get; set; }
}

public class ScimEmail
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}

public class ScimPhoneNumber
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class ScimAddress
{
    [JsonPropertyName("formatted")]
    public string? Formatted { get; set; }
    
    [JsonPropertyName("streetAddress")]
    public string? StreetAddress { get; set; }
    
    [JsonPropertyName("locality")]
    public string? Locality { get; set; }
    
    [JsonPropertyName("region")]
    public string? Region { get; set; }
    
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}

public class ScimGroupRef
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }
    
    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

public class ScimEnterpriseUser
{
    [JsonPropertyName("employeeNumber")]
    public string? EmployeeNumber { get; set; }
    
    [JsonPropertyName("costCenter")]
    public string? CostCenter { get; set; }
    
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }
    
    [JsonPropertyName("division")]
    public string? Division { get; set; }
    
    [JsonPropertyName("department")]
    public string? Department { get; set; }
    
    [JsonPropertyName("manager")]
    public ScimManager? Manager { get; set; }
}

public class ScimManager
{
    [JsonPropertyName("value")]
    public string? Value { get; set; }
    
    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }
    
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}