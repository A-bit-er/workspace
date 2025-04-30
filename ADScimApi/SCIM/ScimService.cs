using ADScimApi.Business;
using ADScimApi.Models;
using ADScimApi.SCIM.Models;
using ADScimApi.Services;

namespace ADScimApi.SCIM;

public class ScimService : IScimService
{
    private readonly IActiveDirectoryService _adService;
    private readonly IBusinessRuleService _businessRuleService;
    private readonly ILogger<ScimService> _logger;
    private readonly string _baseUrl;

    public ScimService(
        IActiveDirectoryService adService,
        IBusinessRuleService businessRuleService,
        ILogger<ScimService> logger,
        IConfiguration configuration)
    {
        _adService = adService;
        _businessRuleService = businessRuleService;
        _logger = logger;
        _baseUrl = configuration["BaseUrl"] ?? "https://localhost:5001";
    }

    // Helper methods for mapping between AD and SCIM models
    private ScimUser MapToScimUser(ActiveDirectoryObject adUser)
    {
        var scimUser = new ScimUser
        {
            Id = adUser.ObjectGuid,
            UserName = adUser.Attributes.ContainsKey("sAMAccountName") 
                ? adUser.Attributes["sAMAccountName"].ToString() ?? string.Empty 
                : adUser.Name,
            DisplayName = adUser.Attributes.ContainsKey("displayName") 
                ? adUser.Attributes["displayName"].ToString() 
                : null,
            Name = new ScimName
            {
                GivenName = adUser.Attributes.ContainsKey("givenName") 
                    ? adUser.Attributes["givenName"].ToString() 
                    : null,
                FamilyName = adUser.Attributes.ContainsKey("sn") 
                    ? adUser.Attributes["sn"].ToString() 
                    : null,
                Formatted = adUser.Attributes.ContainsKey("cn") 
                    ? adUser.Attributes["cn"].ToString() 
                    : null
            },
            Active = adUser.Attributes.ContainsKey("userAccountControl") 
                ? !Convert.ToBoolean(Convert.ToInt32(adUser.Attributes["userAccountControl"]) & 0x2) 
                : true,
            Meta = new ScimMeta
            {
                ResourceType = "User",
                Created = adUser.Attributes.ContainsKey("whenCreated") 
                    ? DateTime.Parse(adUser.Attributes["whenCreated"].ToString() ?? string.Empty) 
                    : null,
                LastModified = adUser.Attributes.ContainsKey("whenChanged") 
                    ? DateTime.Parse(adUser.Attributes["whenChanged"].ToString() ?? string.Empty) 
                    : null,
                Location = $"{_baseUrl}/scim/v2/Users/{adUser.ObjectGuid}"
            }
        };

        // Add emails if available
        if (adUser.Attributes.ContainsKey("mail"))
        {
            scimUser.Emails = new List<ScimEmail>
            {
                new ScimEmail
                {
                    Value = adUser.Attributes["mail"].ToString(),
                    Type = "work",
                    Primary = true
                }
            };
        }

        // Add phone numbers if available
        if (adUser.Attributes.ContainsKey("telephoneNumber"))
        {
            scimUser.PhoneNumbers = new List<ScimPhoneNumber>
            {
                new ScimPhoneNumber
                {
                    Value = adUser.Attributes["telephoneNumber"].ToString(),
                    Type = "work"
                }
            };
        }

        // Add enterprise extension if needed
        if (adUser.Attributes.ContainsKey("department") || 
            adUser.Attributes.ContainsKey("division") || 
            adUser.Attributes.ContainsKey("employeeID"))
        {
            scimUser.Schemas.Add(ScimUser.ExtensionSchemaUri);
            scimUser.EnterpriseUser = new ScimEnterpriseUser
            {
                Department = adUser.Attributes.ContainsKey("department") 
                    ? adUser.Attributes["department"].ToString() 
                    : null,
                Division = adUser.Attributes.ContainsKey("division") 
                    ? adUser.Attributes["division"].ToString() 
                    : null,
                EmployeeNumber = adUser.Attributes.ContainsKey("employeeID") 
                    ? adUser.Attributes["employeeID"].ToString() 
                    : null
            };
        }

        return scimUser;
    }

    private ActiveDirectoryObject MapToAdUser(ScimUser scimUser)
    {
        var adUser = new ActiveDirectoryObject
        {
            ObjectGuid = scimUser.Id,
            Name = scimUser.DisplayName ?? scimUser.UserName,
            ObjectType = "User",
            Attributes = new Dictionary<string, object>
            {
                ["sAMAccountName"] = scimUser.UserName,
                ["userPrincipalName"] = scimUser.UserName + "@example.com" // This should be configurable
            }
        };

        if (scimUser.Name != null)
        {
            if (!string.IsNullOrEmpty(scimUser.Name.GivenName))
                adUser.Attributes["givenName"] = scimUser.Name.GivenName;
                
            if (!string.IsNullOrEmpty(scimUser.Name.FamilyName))
                adUser.Attributes["sn"] = scimUser.Name.FamilyName;
                
            if (!string.IsNullOrEmpty(scimUser.Name.Formatted))
                adUser.Attributes["cn"] = scimUser.Name.Formatted;
        }

        if (!string.IsNullOrEmpty(scimUser.DisplayName))
            adUser.Attributes["displayName"] = scimUser.DisplayName;

        if (scimUser.Emails != null && scimUser.Emails.Count > 0)
        {
            var primaryEmail = scimUser.Emails.FirstOrDefault(e => e.Primary) ?? scimUser.Emails.First();
            if (!string.IsNullOrEmpty(primaryEmail.Value))
                adUser.Attributes["mail"] = primaryEmail.Value;
        }

        if (scimUser.PhoneNumbers != null && scimUser.PhoneNumbers.Count > 0)
        {
            var workPhone = scimUser.PhoneNumbers.FirstOrDefault(p => p.Type?.ToLower() == "work") ?? scimUser.PhoneNumbers.First();
            if (!string.IsNullOrEmpty(workPhone.Value))
                adUser.Attributes["telephoneNumber"] = workPhone.Value;
        }

        if (scimUser.EnterpriseUser != null)
        {
            if (!string.IsNullOrEmpty(scimUser.EnterpriseUser.Department))
                adUser.Attributes["department"] = scimUser.EnterpriseUser.Department;
                
            if (!string.IsNullOrEmpty(scimUser.EnterpriseUser.Division))
                adUser.Attributes["division"] = scimUser.EnterpriseUser.Division;
                
            if (!string.IsNullOrEmpty(scimUser.EnterpriseUser.EmployeeNumber))
                adUser.Attributes["employeeID"] = scimUser.EnterpriseUser.EmployeeNumber;
        }

        if (!string.IsNullOrEmpty(scimUser.Password))
            adUser.Attributes["password"] = scimUser.Password;

        adUser.Attributes["enabled"] = scimUser.Active;

        return adUser;
    }

    private ScimGroup MapToScimGroup(ActiveDirectoryObject adGroup)
    {
        var scimGroup = new ScimGroup
        {
            Id = adGroup.ObjectGuid,
            DisplayName = adGroup.Name,
            Meta = new ScimMeta
            {
                ResourceType = "Group",
                Created = adGroup.Attributes.ContainsKey("whenCreated") 
                    ? DateTime.Parse(adGroup.Attributes["whenCreated"].ToString() ?? string.Empty) 
                    : null,
                LastModified = adGroup.Attributes.ContainsKey("whenChanged") 
                    ? DateTime.Parse(adGroup.Attributes["whenChanged"].ToString() ?? string.Empty) 
                    : null,
                Location = $"{_baseUrl}/scim/v2/Groups/{adGroup.ObjectGuid}"
            }
        };

        return scimGroup;
    }

    private ActiveDirectoryObject MapToAdGroup(ScimGroup scimGroup)
    {
        var adGroup = new ActiveDirectoryObject
        {
            ObjectGuid = scimGroup.Id,
            Name = scimGroup.DisplayName,
            ObjectType = "Group",
            Attributes = new Dictionary<string, object>
            {
                ["sAMAccountName"] = scimGroup.DisplayName,
                ["displayName"] = scimGroup.DisplayName
            }
        };

        return adGroup;
    }

    // User operations
    public async Task<ScimListResponse<ScimUser>> GetUsersAsync(string? filter = null, int? startIndex = null, int? count = null)
    {
        try
        {
            var adUsers = await _adService.GetUsersAsync(filter);
            var scimUsers = adUsers.Select(MapToScimUser).ToList();
            
            // Apply pagination
            int start = startIndex ?? 1;
            int itemsPerPage = count ?? 100;
            
            var pagedUsers = scimUsers
                .Skip(start - 1)
                .Take(itemsPerPage)
                .ToList();
            
            return new ScimListResponse<ScimUser>
            {
                TotalResults = scimUsers.Count,
                StartIndex = start,
                ItemsPerPage = pagedUsers.Count,
                Resources = pagedUsers
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users with filter {Filter}", filter);
            throw;
        }
    }

    public async Task<ScimUser?> GetUserByIdAsync(string id)
    {
        try
        {
            var adUser = await _adService.GetUserByIdAsync(id);
            
            if (adUser == null)
            {
                return null;
            }
            
            return MapToScimUser(adUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user with ID {Id}", id);
            throw;
        }
    }

    public async Task<ScimUser> CreateUserAsync(ScimUser user)
    {
        try
        {
            var adUser = MapToAdUser(user);
            
            // Apply business rules to determine OU
            string targetOu = await _businessRuleService.EvaluateRulesForObjectAsync(adUser);
            
            if (!string.IsNullOrEmpty(targetOu))
            {
                adUser.DistinguishedName = $"CN={adUser.Name},{targetOu}";
            }
            
            var createdUser = await _adService.CreateUserAsync(adUser);
            return MapToScimUser(createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {UserName}", user.UserName);
            throw;
        }
    }

    public async Task<ScimUser?> UpdateUserAsync(string id, ScimUser user)
    {
        try
        {
            var adUser = MapToAdUser(user);
            adUser.ObjectGuid = id;
            
            // Apply business rules to determine OU
            string targetOu = await _businessRuleService.EvaluateRulesForObjectAsync(adUser);
            
            if (!string.IsNullOrEmpty(targetOu))
            {
                adUser.DistinguishedName = $"CN={adUser.Name},{targetOu}";
            }
            
            var updatedUser = await _adService.UpdateUserAsync(id, adUser);
            
            if (updatedUser == null)
            {
                return null;
            }
            
            return MapToScimUser(updatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {Id}", id);
            throw;
        }
    }

    public async Task<ScimUser?> PatchUserAsync(string id, ScimPatchRequest patchRequest)
    {
        try
        {
            // Get the current user
            var adUser = await _adService.GetUserByIdAsync(id);
            
            if (adUser == null)
            {
                return null;
            }
            
            var scimUser = MapToScimUser(adUser);
            
            // Apply patch operations
            foreach (var operation in patchRequest.Operations)
            {
                switch (operation.Op.ToLower())
                {
                    case "add":
                        ApplyAddOperation(scimUser, operation);
                        break;
                    case "replace":
                        ApplyReplaceOperation(scimUser, operation);
                        break;
                    case "remove":
                        ApplyRemoveOperation(scimUser, operation);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported operation: {operation.Op}");
                }
            }
            
            // Update the user
            return await UpdateUserAsync(id, scimUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching user with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        try
        {
            return await _adService.DeleteUserAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {Id}", id);
            throw;
        }
    }

    // Group operations
    public async Task<ScimListResponse<ScimGroup>> GetGroupsAsync(string? filter = null, int? startIndex = null, int? count = null)
    {
        try
        {
            var adGroups = await _adService.GetGroupsAsync(filter);
            var scimGroups = adGroups.Select(MapToScimGroup).ToList();
            
            // Apply pagination
            int start = startIndex ?? 1;
            int itemsPerPage = count ?? 100;
            
            var pagedGroups = scimGroups
                .Skip(start - 1)
                .Take(itemsPerPage)
                .ToList();
            
            return new ScimListResponse<ScimGroup>
            {
                TotalResults = scimGroups.Count,
                StartIndex = start,
                ItemsPerPage = pagedGroups.Count,
                Resources = pagedGroups
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups with filter {Filter}", filter);
            throw;
        }
    }

    public async Task<ScimGroup?> GetGroupByIdAsync(string id)
    {
        try
        {
            var adGroup = await _adService.GetGroupByIdAsync(id);
            
            if (adGroup == null)
            {
                return null;
            }
            
            var scimGroup = MapToScimGroup(adGroup);
            
            // Get group members
            var members = await _adService.GetGroupMembersAsync(id);
            
            if (members.Any())
            {
                scimGroup.Members = members.Select(m => new ScimMemberRef
                {
                    Value = m.ObjectGuid,
                    Display = m.Name,
                    Type = m.ObjectType == "User" ? "User" : "Group"
                }).ToList();
            }
            
            return scimGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group with ID {Id}", id);
            throw;
        }
    }

    public async Task<ScimGroup> CreateGroupAsync(ScimGroup group)
    {
        try
        {
            var adGroup = MapToAdGroup(group);
            
            // Apply business rules to determine OU
            string targetOu = await _businessRuleService.EvaluateRulesForObjectAsync(adGroup);
            
            if (!string.IsNullOrEmpty(targetOu))
            {
                adGroup.DistinguishedName = $"CN={adGroup.Name},{targetOu}";
            }
            
            var createdGroup = await _adService.CreateGroupAsync(adGroup);
            var scimGroup = MapToScimGroup(createdGroup);
            
            // Add members if specified
            if (group.Members != null && group.Members.Any())
            {
                foreach (var member in group.Members)
                {
                    if (!string.IsNullOrEmpty(member.Value))
                    {
                        await _adService.AddUserToGroupAsync(member.Value, scimGroup.Id);
                    }
                }
                
                // Refresh group to include members
                return await GetGroupByIdAsync(scimGroup.Id) ?? scimGroup;
            }
            
            return scimGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group {DisplayName}", group.DisplayName);
            throw;
        }
    }

    public async Task<ScimGroup?> UpdateGroupAsync(string id, ScimGroup group)
    {
        try
        {
            var adGroup = MapToAdGroup(group);
            adGroup.ObjectGuid = id;
            
            // Apply business rules to determine OU
            string targetOu = await _businessRuleService.EvaluateRulesForObjectAsync(adGroup);
            
            if (!string.IsNullOrEmpty(targetOu))
            {
                adGroup.DistinguishedName = $"CN={adGroup.Name},{targetOu}";
            }
            
            var updatedGroup = await _adService.UpdateGroupAsync(id, adGroup);
            
            if (updatedGroup == null)
            {
                return null;
            }
            
            var scimGroup = MapToScimGroup(updatedGroup);
            
            // Update members if specified
            if (group.Members != null)
            {
                // Get current members
                var currentMembers = await _adService.GetGroupMembersAsync(id);
                var currentMemberIds = currentMembers.Select(m => m.ObjectGuid).ToList();
                
                // Get new member IDs
                var newMemberIds = group.Members
                    .Where(m => !string.IsNullOrEmpty(m.Value))
                    .Select(m => m.Value!)
                    .ToList();
                
                // Remove members that are no longer in the group
                foreach (var memberId in currentMemberIds)
                {
                    if (!newMemberIds.Contains(memberId))
                    {
                        await _adService.RemoveUserFromGroupAsync(memberId, id);
                    }
                }
                
                // Add new members
                foreach (var memberId in newMemberIds)
                {
                    if (!currentMemberIds.Contains(memberId))
                    {
                        await _adService.AddUserToGroupAsync(memberId, id);
                    }
                }
                
                // Refresh group to include updated members
                return await GetGroupByIdAsync(id) ?? scimGroup;
            }
            
            return scimGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group with ID {Id}", id);
            throw;
        }
    }

    public async Task<ScimGroup?> PatchGroupAsync(string id, ScimPatchRequest patchRequest)
    {
        try
        {
            // Get the current group
            var adGroup = await _adService.GetGroupByIdAsync(id);
            
            if (adGroup == null)
            {
                return null;
            }
            
            var scimGroup = await GetGroupByIdAsync(id);
            
            if (scimGroup == null)
            {
                return null;
            }
            
            // Apply patch operations
            foreach (var operation in patchRequest.Operations)
            {
                switch (operation.Op.ToLower())
                {
                    case "add":
                        ApplyAddOperation(scimGroup, operation);
                        break;
                    case "replace":
                        ApplyReplaceOperation(scimGroup, operation);
                        break;
                    case "remove":
                        ApplyRemoveOperation(scimGroup, operation);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported operation: {operation.Op}");
                }
            }
            
            // Update the group
            return await UpdateGroupAsync(id, scimGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching group with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteGroupAsync(string id)
    {
        try
        {
            return await _adService.DeleteGroupAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group with ID {Id}", id);
            throw;
        }
    }

    // Helper methods for PATCH operations
    private void ApplyAddOperation(ScimUser user, ScimPatchOperation operation)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            throw new ArgumentException("Path is required for add operation");
        }
        
        switch (operation.Path.ToLower())
        {
            case "emails":
                if (operation.Value is List<ScimEmail> emails)
                {
                    user.Emails ??= new List<ScimEmail>();
                    user.Emails.AddRange(emails);
                }
                break;
            case "phonenumbers":
                if (operation.Value is List<ScimPhoneNumber> phoneNumbers)
                {
                    user.PhoneNumbers ??= new List<ScimPhoneNumber>();
                    user.PhoneNumbers.AddRange(phoneNumbers);
                }
                break;
            case "addresses":
                if (operation.Value is List<ScimAddress> addresses)
                {
                    user.Addresses ??= new List<ScimAddress>();
                    user.Addresses.AddRange(addresses);
                }
                break;
            default:
                throw new ArgumentException($"Unsupported path for add operation: {operation.Path}");
        }
    }

    private void ApplyReplaceOperation(ScimUser user, ScimPatchOperation operation)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            // Replace the entire resource
            if (operation.Value is ScimUser newUser)
            {
                user.UserName = newUser.UserName;
                user.Name = newUser.Name;
                user.DisplayName = newUser.DisplayName;
                user.Active = newUser.Active;
                user.Emails = newUser.Emails;
                user.PhoneNumbers = newUser.PhoneNumbers;
                user.Addresses = newUser.Addresses;
                user.EnterpriseUser = newUser.EnterpriseUser;
            }
            return;
        }
        
        switch (operation.Path.ToLower())
        {
            case "username":
                if (operation.Value != null)
                {
                    user.UserName = operation.Value.ToString() ?? string.Empty;
                }
                break;
            case "displayname":
                if (operation.Value != null)
                {
                    user.DisplayName = operation.Value.ToString();
                }
                break;
            case "active":
                if (operation.Value != null)
                {
                    user.Active = Convert.ToBoolean(operation.Value);
                }
                break;
            case "name.givenname":
                user.Name ??= new ScimName();
                if (operation.Value != null)
                {
                    user.Name.GivenName = operation.Value.ToString();
                }
                break;
            case "name.familyname":
                user.Name ??= new ScimName();
                if (operation.Value != null)
                {
                    user.Name.FamilyName = operation.Value.ToString();
                }
                break;
            case "emails":
                if (operation.Value is List<ScimEmail> emails)
                {
                    user.Emails = emails;
                }
                break;
            case "phonenumbers":
                if (operation.Value is List<ScimPhoneNumber> phoneNumbers)
                {
                    user.PhoneNumbers = phoneNumbers;
                }
                break;
            case "addresses":
                if (operation.Value is List<ScimAddress> addresses)
                {
                    user.Addresses = addresses;
                }
                break;
            default:
                throw new ArgumentException($"Unsupported path for replace operation: {operation.Path}");
        }
    }

    private void ApplyRemoveOperation(ScimUser user, ScimPatchOperation operation)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            throw new ArgumentException("Path is required for remove operation");
        }
        
        switch (operation.Path.ToLower())
        {
            case "emails":
                user.Emails = null;
                break;
            case "phonenumbers":
                user.PhoneNumbers = null;
                break;
            case "addresses":
                user.Addresses = null;
                break;
            case "name.givenname":
                if (user.Name != null)
                {
                    user.Name.GivenName = null;
                }
                break;
            case "name.familyname":
                if (user.Name != null)
                {
                    user.Name.FamilyName = null;
                }
                break;
            case "displayname":
                user.DisplayName = null;
                break;
            default:
                throw new ArgumentException($"Unsupported path for remove operation: {operation.Path}");
        }
    }

    private void ApplyAddOperation(ScimGroup group, ScimPatchOperation operation)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            throw new ArgumentException("Path is required for add operation");
        }
        
        switch (operation.Path.ToLower())
        {
            case "members":
                if (operation.Value is List<ScimMemberRef> members)
                {
                    group.Members ??= new List<ScimMemberRef>();
                    group.Members.AddRange(members);
                }
                break;
            default:
                throw new ArgumentException($"Unsupported path for add operation: {operation.Path}");
        }
    }

    private void ApplyReplaceOperation(ScimGroup group, ScimPatchOperation operation)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            // Replace the entire resource
            if (operation.Value is ScimGroup newGroup)
            {
                group.DisplayName = newGroup.DisplayName;
                group.Members = newGroup.Members;
            }
            return;
        }
        
        switch (operation.Path.ToLower())
        {
            case "displayname":
                if (operation.Value != null)
                {
                    group.DisplayName = operation.Value.ToString() ?? string.Empty;
                }
                break;
            case "members":
                if (operation.Value is List<ScimMemberRef> members)
                {
                    group.Members = members;
                }
                break;
            default:
                throw new ArgumentException($"Unsupported path for replace operation: {operation.Path}");
        }
    }

    private void ApplyRemoveOperation(ScimGroup group, ScimPatchOperation operation)
    {
        if (string.IsNullOrEmpty(operation.Path))
        {
            throw new ArgumentException("Path is required for remove operation");
        }
        
        switch (operation.Path.ToLower())
        {
            case "members":
                group.Members = null;
                break;
            default:
                throw new ArgumentException($"Unsupported path for remove operation: {operation.Path}");
        }
    }
}