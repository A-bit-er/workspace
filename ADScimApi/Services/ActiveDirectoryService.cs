using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using ADScimApi.Models;

namespace ADScimApi.Services;

public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _domainName;
    private readonly string _ldapPath;
    private readonly string _username;
    private readonly string _password;

    public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Get AD connection settings from configuration
        _domainName = _configuration["ActiveDirectory:Domain"] ?? "example.com";
        _ldapPath = _configuration["ActiveDirectory:LdapPath"] ?? "LDAP://DC=example,DC=com";
        _username = _configuration["ActiveDirectory:Username"] ?? "";
        _password = _configuration["ActiveDirectory:Password"] ?? "";
    }

    private DirectoryEntry GetDirectoryEntry(string path = "")
    {
        string ldapPath = string.IsNullOrEmpty(path) ? _ldapPath : path;
        
        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
        {
            return new DirectoryEntry(ldapPath, _username, _password);
        }
        
        return new DirectoryEntry(ldapPath);
    }

    private PrincipalContext GetPrincipalContext(ContextType contextType = ContextType.Domain)
    {
        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
        {
            return new PrincipalContext(contextType, _domainName, _username, _password);
        }
        
        return new PrincipalContext(contextType, _domainName);
    }

    private ActiveDirectoryObject MapToActiveDirectoryObject(DirectoryEntry entry, string objectType)
    {
        var adObject = new ActiveDirectoryObject
        {
            ObjectGuid = entry.Guid.ToString(),
            DistinguishedName = entry.Properties["distinguishedName"].Value?.ToString() ?? string.Empty,
            Name = entry.Properties["name"].Value?.ToString() ?? string.Empty,
            ObjectType = objectType,
            Attributes = new Dictionary<string, object>()
        };

        // Map common attributes
        foreach (PropertyValueCollection prop in entry.Properties)
        {
            if (prop.Value != null)
            {
                if (prop.Count > 1)
                {
                    var values = new List<object>();
                    foreach (var val in prop)
                    {
                        values.Add(val);
                    }
                    adObject.Attributes[prop.PropertyName] = values;
                }
                else
                {
                    adObject.Attributes[prop.PropertyName] = prop.Value;
                }
            }
        }

        return adObject;
    }

    // User operations
    public async Task<IEnumerable<ActiveDirectoryObject>> GetUsersAsync(string? filter = null)
    {
        var users = new List<ActiveDirectoryObject>();
        
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var userPrincipal = new UserPrincipal(context);
            
            if (!string.IsNullOrEmpty(filter))
            {
                // Apply filter logic here
                if (filter.Contains("="))
                {
                    var parts = filter.Split('=');
                    if (parts.Length == 2)
                    {
                        var property = parts[0].Trim();
                        var value = parts[1].Trim();
                        
                        switch (property.ToLower())
                        {
                            case "samaccountname":
                                userPrincipal.SamAccountName = value;
                                break;
                            case "displayname":
                                userPrincipal.DisplayName = value;
                                break;
                            case "givenname":
                                userPrincipal.GivenName = value;
                                break;
                            case "surname":
                                userPrincipal.Surname = value;
                                break;
                            case "userprincipalname":
                                userPrincipal.UserPrincipalName = value;
                                break;
                        }
                    }
                }
            }
            
            using var searcher = new PrincipalSearcher(userPrincipal);
            var results = searcher.FindAll();
            
            foreach (UserPrincipal user in results)
            {
                using var entry = user.GetUnderlyingObject() as DirectoryEntry;
                if (entry != null)
                {
                    users.Add(MapToActiveDirectoryObject(entry, "User"));
                }
            }
        });
        
        return users;
    }

    public async Task<ActiveDirectoryObject?> GetUserByIdAsync(string id)
    {
        ActiveDirectoryObject? user = null;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new UserPrincipal(context) { Guid = new Guid(id) });
                var result = searcher.FindOne() as UserPrincipal;
                
                if (result != null)
                {
                    using var entry = result.GetUnderlyingObject() as DirectoryEntry;
                    if (entry != null)
                    {
                        user = MapToActiveDirectoryObject(entry, "User");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {Id}", id);
            }
        });
        
        return user;
    }

    public async Task<ActiveDirectoryObject> CreateUserAsync(ActiveDirectoryObject user)
    {
        ActiveDirectoryObject? createdUser = null;
        
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var userPrincipal = new UserPrincipal(context);
            
            // Set required properties
            userPrincipal.SamAccountName = user.Attributes.ContainsKey("sAMAccountName") 
                ? user.Attributes["sAMAccountName"].ToString() 
                : user.Name;
                
            userPrincipal.Name = user.Name;
            
            if (user.Attributes.ContainsKey("givenName"))
                userPrincipal.GivenName = user.Attributes["givenName"].ToString();
                
            if (user.Attributes.ContainsKey("sn"))
                userPrincipal.Surname = user.Attributes["sn"].ToString();
                
            if (user.Attributes.ContainsKey("userPrincipalName"))
                userPrincipal.UserPrincipalName = user.Attributes["userPrincipalName"].ToString();
                
            if (user.Attributes.ContainsKey("displayName"))
                userPrincipal.DisplayName = user.Attributes["displayName"].ToString();
                
            if (user.Attributes.ContainsKey("description"))
                userPrincipal.Description = user.Attributes["description"].ToString();
                
            if (user.Attributes.ContainsKey("password"))
                userPrincipal.SetPassword(user.Attributes["password"].ToString());
            else
                userPrincipal.SetPassword(Guid.NewGuid().ToString()); // Generate random password
                
            userPrincipal.Enabled = true;
            userPrincipal.Save();
            
            // Get the created user
            using var entry = userPrincipal.GetUnderlyingObject() as DirectoryEntry;
            if (entry != null)
            {
                createdUser = MapToActiveDirectoryObject(entry, "User");
                
                // Move to specified OU if provided
                if (!string.IsNullOrEmpty(user.DistinguishedName))
                {
                    string targetOu = user.DistinguishedName.Substring(user.DistinguishedName.IndexOf(',') + 1);
                    MoveObjectToOUAsync(createdUser.ObjectGuid, targetOu).Wait();
                    
                    // Refresh the user object after moving
                    using var movedEntry = new DirectoryEntry($"LDAP://<GUID={createdUser.ObjectGuid}>");
                    createdUser = MapToActiveDirectoryObject(movedEntry, "User");
                }
            }
        });
        
        return createdUser ?? throw new Exception("Failed to create user");
    }

    public async Task<ActiveDirectoryObject?> UpdateUserAsync(string id, ActiveDirectoryObject user)
    {
        ActiveDirectoryObject? updatedUser = null;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new UserPrincipal(context) { Guid = new Guid(id) });
                var result = searcher.FindOne() as UserPrincipal;
                
                if (result != null)
                {
                    // Update properties
                    if (user.Attributes.ContainsKey("givenName"))
                        result.GivenName = user.Attributes["givenName"].ToString();
                        
                    if (user.Attributes.ContainsKey("sn"))
                        result.Surname = user.Attributes["sn"].ToString();
                        
                    if (user.Attributes.ContainsKey("displayName"))
                        result.DisplayName = user.Attributes["displayName"].ToString();
                        
                    if (user.Attributes.ContainsKey("description"))
                        result.Description = user.Attributes["description"].ToString();
                        
                    if (user.Attributes.ContainsKey("userPrincipalName"))
                        result.UserPrincipalName = user.Attributes["userPrincipalName"].ToString();
                        
                    if (user.Attributes.ContainsKey("password"))
                        result.SetPassword(user.Attributes["password"].ToString());
                        
                    if (user.Attributes.ContainsKey("enabled"))
                        result.Enabled = Convert.ToBoolean(user.Attributes["enabled"]);
                        
                    result.Save();
                    
                    // Get the updated user
                    using var entry = result.GetUnderlyingObject() as DirectoryEntry;
                    if (entry != null)
                    {
                        updatedUser = MapToActiveDirectoryObject(entry, "User");
                        
                        // Move to specified OU if provided and different from current
                        if (!string.IsNullOrEmpty(user.DistinguishedName) && 
                            user.DistinguishedName.Substring(user.DistinguishedName.IndexOf(',') + 1) != 
                            updatedUser.DistinguishedName.Substring(updatedUser.DistinguishedName.IndexOf(',') + 1))
                        {
                            string targetOu = user.DistinguishedName.Substring(user.DistinguishedName.IndexOf(',') + 1);
                            MoveObjectToOUAsync(updatedUser.ObjectGuid, targetOu).Wait();
                            
                            // Refresh the user object after moving
                            using var movedEntry = new DirectoryEntry($"LDAP://<GUID={updatedUser.ObjectGuid}>");
                            updatedUser = MapToActiveDirectoryObject(movedEntry, "User");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {Id}", id);
            }
        });
        
        return updatedUser;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        bool success = false;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new UserPrincipal(context) { Guid = new Guid(id) });
                var result = searcher.FindOne() as UserPrincipal;
                
                if (result != null)
                {
                    result.Delete();
                    success = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {Id}", id);
            }
        });
        
        return success;
    }

    // Group operations
    public async Task<IEnumerable<ActiveDirectoryObject>> GetGroupsAsync(string? filter = null)
    {
        var groups = new List<ActiveDirectoryObject>();
        
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var groupPrincipal = new GroupPrincipal(context);
            
            if (!string.IsNullOrEmpty(filter))
            {
                // Apply filter logic here
                if (filter.Contains("="))
                {
                    var parts = filter.Split('=');
                    if (parts.Length == 2)
                    {
                        var property = parts[0].Trim();
                        var value = parts[1].Trim();
                        
                        switch (property.ToLower())
                        {
                            case "samaccountname":
                                groupPrincipal.SamAccountName = value;
                                break;
                            case "displayname":
                                groupPrincipal.DisplayName = value;
                                break;
                            case "description":
                                groupPrincipal.Description = value;
                                break;
                        }
                    }
                }
            }
            
            using var searcher = new PrincipalSearcher(groupPrincipal);
            var results = searcher.FindAll();
            
            foreach (GroupPrincipal group in results)
            {
                using var entry = group.GetUnderlyingObject() as DirectoryEntry;
                if (entry != null)
                {
                    groups.Add(MapToActiveDirectoryObject(entry, "Group"));
                }
            }
        });
        
        return groups;
    }

    public async Task<ActiveDirectoryObject?> GetGroupByIdAsync(string id)
    {
        ActiveDirectoryObject? group = null;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new GroupPrincipal(context) { Guid = new Guid(id) });
                var result = searcher.FindOne() as GroupPrincipal;
                
                if (result != null)
                {
                    using var entry = result.GetUnderlyingObject() as DirectoryEntry;
                    if (entry != null)
                    {
                        group = MapToActiveDirectoryObject(entry, "Group");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group by ID {Id}", id);
            }
        });
        
        return group;
    }

    public async Task<ActiveDirectoryObject> CreateGroupAsync(ActiveDirectoryObject group)
    {
        ActiveDirectoryObject? createdGroup = null;
        
        await Task.Run(() =>
        {
            using var context = GetPrincipalContext();
            using var groupPrincipal = new GroupPrincipal(context);
            
            // Set required properties
            groupPrincipal.SamAccountName = group.Attributes.ContainsKey("sAMAccountName") 
                ? group.Attributes["sAMAccountName"].ToString() 
                : group.Name;
                
            groupPrincipal.Name = group.Name;
            
            if (group.Attributes.ContainsKey("displayName"))
                groupPrincipal.DisplayName = group.Attributes["displayName"].ToString();
                
            if (group.Attributes.ContainsKey("description"))
                groupPrincipal.Description = group.Attributes["description"].ToString();
                
            if (group.Attributes.ContainsKey("groupScope") && 
                Enum.TryParse<GroupScope>(group.Attributes["groupScope"].ToString(), out var scope))
                groupPrincipal.GroupScope = scope;
            else
                groupPrincipal.GroupScope = GroupScope.Universal;
                
            if (group.Attributes.ContainsKey("isSecurityGroup"))
                groupPrincipal.IsSecurityGroup = Convert.ToBoolean(group.Attributes["isSecurityGroup"]);
            else
                groupPrincipal.IsSecurityGroup = true;
                
            groupPrincipal.Save();
            
            // Get the created group
            using var entry = groupPrincipal.GetUnderlyingObject() as DirectoryEntry;
            if (entry != null)
            {
                createdGroup = MapToActiveDirectoryObject(entry, "Group");
                
                // Move to specified OU if provided
                if (!string.IsNullOrEmpty(group.DistinguishedName))
                {
                    string targetOu = group.DistinguishedName.Substring(group.DistinguishedName.IndexOf(',') + 1);
                    MoveObjectToOUAsync(createdGroup.ObjectGuid, targetOu).Wait();
                    
                    // Refresh the group object after moving
                    using var movedEntry = new DirectoryEntry($"LDAP://<GUID={createdGroup.ObjectGuid}>");
                    createdGroup = MapToActiveDirectoryObject(movedEntry, "Group");
                }
            }
        });
        
        return createdGroup ?? throw new Exception("Failed to create group");
    }

    public async Task<ActiveDirectoryObject?> UpdateGroupAsync(string id, ActiveDirectoryObject group)
    {
        ActiveDirectoryObject? updatedGroup = null;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new GroupPrincipal(context) { Guid = new Guid(id) });
                var result = searcher.FindOne() as GroupPrincipal;
                
                if (result != null)
                {
                    // Update properties
                    if (group.Attributes.ContainsKey("displayName"))
                        result.DisplayName = group.Attributes["displayName"].ToString();
                        
                    if (group.Attributes.ContainsKey("description"))
                        result.Description = group.Attributes["description"].ToString();
                        
                    result.Save();
                    
                    // Get the updated group
                    using var entry = result.GetUnderlyingObject() as DirectoryEntry;
                    if (entry != null)
                    {
                        updatedGroup = MapToActiveDirectoryObject(entry, "Group");
                        
                        // Move to specified OU if provided and different from current
                        if (!string.IsNullOrEmpty(group.DistinguishedName) && 
                            group.DistinguishedName.Substring(group.DistinguishedName.IndexOf(',') + 1) != 
                            updatedGroup.DistinguishedName.Substring(updatedGroup.DistinguishedName.IndexOf(',') + 1))
                        {
                            string targetOu = group.DistinguishedName.Substring(group.DistinguishedName.IndexOf(',') + 1);
                            MoveObjectToOUAsync(updatedGroup.ObjectGuid, targetOu).Wait();
                            
                            // Refresh the group object after moving
                            using var movedEntry = new DirectoryEntry($"LDAP://<GUID={updatedGroup.ObjectGuid}>");
                            updatedGroup = MapToActiveDirectoryObject(movedEntry, "Group");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group with ID {Id}", id);
            }
        });
        
        return updatedGroup;
    }

    public async Task<bool> DeleteGroupAsync(string id)
    {
        bool success = false;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var searcher = new PrincipalSearcher(new GroupPrincipal(context) { Guid = new Guid(id) });
                var result = searcher.FindOne() as GroupPrincipal;
                
                if (result != null)
                {
                    result.Delete();
                    success = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group with ID {Id}", id);
            }
        });
        
        return success;
    }

    // gMSA operations
    public async Task<IEnumerable<ActiveDirectoryObject>> GetGMSAAccountsAsync(string? filter = null)
    {
        var gmsaAccounts = new List<ActiveDirectoryObject>();
        
        await Task.Run(() =>
        {
            using var directoryEntry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(directoryEntry)
            {
                Filter = "(&(objectClass=msDS-GroupManagedServiceAccount))"
            };
            
            if (!string.IsNullOrEmpty(filter))
            {
                // Apply filter logic here
                if (filter.Contains("="))
                {
                    var parts = filter.Split('=');
                    if (parts.Length == 2)
                    {
                        var property = parts[0].Trim();
                        var value = parts[1].Trim();
                        
                        searcher.Filter = $"(&(objectClass=msDS-GroupManagedServiceAccount)({property}={value}))";
                    }
                }
            }
            
            var results = searcher.FindAll();
            
            foreach (SearchResult result in results)
            {
                using var entry = result.GetDirectoryEntry();
                gmsaAccounts.Add(MapToActiveDirectoryObject(entry, "GMSA"));
            }
        });
        
        return gmsaAccounts;
    }

    public async Task<ActiveDirectoryObject?> GetGMSAAccountByIdAsync(string id)
    {
        ActiveDirectoryObject? gmsa = null;
        
        await Task.Run(() =>
        {
            try
            {
                using var directoryEntry = new DirectoryEntry($"LDAP://<GUID={id}>");
                gmsa = MapToActiveDirectoryObject(directoryEntry, "GMSA");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gMSA account by ID {Id}", id);
            }
        });
        
        return gmsa;
    }

    public async Task<ActiveDirectoryObject> CreateGMSAAccountAsync(ActiveDirectoryObject gmsa)
    {
        // gMSA creation requires PowerShell cmdlets, so this is a simplified implementation
        throw new NotImplementedException("gMSA creation requires PowerShell cmdlets and is not implemented in this service");
    }

    public async Task<ActiveDirectoryObject?> UpdateGMSAAccountAsync(string id, ActiveDirectoryObject gmsa)
    {
        // gMSA update requires PowerShell cmdlets, so this is a simplified implementation
        throw new NotImplementedException("gMSA update requires PowerShell cmdlets and is not implemented in this service");
    }

    public async Task<bool> DeleteGMSAAccountAsync(string id)
    {
        // gMSA deletion requires PowerShell cmdlets, so this is a simplified implementation
        throw new NotImplementedException("gMSA deletion requires PowerShell cmdlets and is not implemented in this service");
    }

    // Group membership operations
    public async Task<bool> AddUserToGroupAsync(string userId, string groupId)
    {
        bool success = false;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var userSearcher = new PrincipalSearcher(new UserPrincipal(context) { Guid = new Guid(userId) });
                var user = userSearcher.FindOne() as UserPrincipal;
                
                using var groupSearcher = new PrincipalSearcher(new GroupPrincipal(context) { Guid = new Guid(groupId) });
                var group = groupSearcher.FindOne() as GroupPrincipal;
                
                if (user != null && group != null)
                {
                    if (!group.Members.Contains(user))
                    {
                        group.Members.Add(user);
                        group.Save();
                        success = true;
                    }
                    else
                    {
                        // User is already a member of the group
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId} to group {GroupId}", userId, groupId);
            }
        });
        
        return success;
    }

    public async Task<bool> RemoveUserFromGroupAsync(string userId, string groupId)
    {
        bool success = false;
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var userSearcher = new PrincipalSearcher(new UserPrincipal(context) { Guid = new Guid(userId) });
                var user = userSearcher.FindOne() as UserPrincipal;
                
                using var groupSearcher = new PrincipalSearcher(new GroupPrincipal(context) { Guid = new Guid(groupId) });
                var group = groupSearcher.FindOne() as GroupPrincipal;
                
                if (user != null && group != null)
                {
                    if (group.Members.Contains(user))
                    {
                        group.Members.Remove(user);
                        group.Save();
                        success = true;
                    }
                    else
                    {
                        // User is not a member of the group
                        success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user {UserId} from group {GroupId}", userId, groupId);
            }
        });
        
        return success;
    }

    public async Task<IEnumerable<ActiveDirectoryObject>> GetGroupMembersAsync(string groupId)
    {
        var members = new List<ActiveDirectoryObject>();
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var groupSearcher = new PrincipalSearcher(new GroupPrincipal(context) { Guid = new Guid(groupId) });
                var group = groupSearcher.FindOne() as GroupPrincipal;
                
                if (group != null)
                {
                    foreach (Principal member in group.Members)
                    {
                        using var entry = member.GetUnderlyingObject() as DirectoryEntry;
                        if (entry != null)
                        {
                            string objectType = "Unknown";
                            if (member is UserPrincipal) objectType = "User";
                            else if (member is GroupPrincipal) objectType = "Group";
                            
                            members.Add(MapToActiveDirectoryObject(entry, objectType));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting members of group {GroupId}", groupId);
            }
        });
        
        return members;
    }

    public async Task<IEnumerable<ActiveDirectoryObject>> GetUserGroupsAsync(string userId)
    {
        var groups = new List<ActiveDirectoryObject>();
        
        await Task.Run(() =>
        {
            try
            {
                using var context = GetPrincipalContext();
                using var userSearcher = new PrincipalSearcher(new UserPrincipal(context) { Guid = new Guid(userId) });
                var user = userSearcher.FindOne() as UserPrincipal;
                
                if (user != null)
                {
                    var userGroups = user.GetGroups();
                    
                    foreach (Principal group in userGroups)
                    {
                        using var entry = group.GetUnderlyingObject() as DirectoryEntry;
                        if (entry != null)
                        {
                            groups.Add(MapToActiveDirectoryObject(entry, "Group"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
            }
        });
        
        return groups;
    }

    // OU operations
    public async Task<IEnumerable<string>> GetOrganizationalUnitsAsync()
    {
        var ous = new List<string>();
        
        await Task.Run(() =>
        {
            using var directoryEntry = GetDirectoryEntry();
            using var searcher = new DirectorySearcher(directoryEntry)
            {
                Filter = "(objectClass=organizationalUnit)"
            };
            
            var results = searcher.FindAll();
            
            foreach (SearchResult result in results)
            {
                using var entry = result.GetDirectoryEntry();
                var dn = entry.Properties["distinguishedName"].Value?.ToString();
                if (!string.IsNullOrEmpty(dn))
                {
                    ous.Add(dn);
                }
            }
        });
        
        return ous;
    }

    public async Task<bool> MoveObjectToOUAsync(string objectId, string ouDistinguishedName)
    {
        bool success = false;
        
        await Task.Run(() =>
        {
            try
            {
                using var objectEntry = new DirectoryEntry($"LDAP://<GUID={objectId}>");
                using var targetOuEntry = new DirectoryEntry($"LDAP://{ouDistinguishedName}");
                
                // Get the object's RDN (Relative Distinguished Name)
                string objectDn = objectEntry.Properties["distinguishedName"].Value.ToString();
                string objectRdn = objectDn.Split(',')[0];
                
                // Move the object
                objectEntry.MoveTo(targetOuEntry, objectRdn);
                success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving object {ObjectId} to OU {OuDn}", objectId, ouDistinguishedName);
            }
        });
        
        return success;
    }
}