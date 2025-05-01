using ADScimApi.Models;

namespace ADScimApi.Services;

public class MockActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<MockActiveDirectoryService> _logger;
    private readonly List<ActiveDirectoryObject> _users = new();
    private readonly List<ActiveDirectoryObject> _groups = new();
    private readonly List<ActiveDirectoryObject> _gmsaAccounts = new();
    private readonly Dictionary<string, List<string>> _groupMemberships = new();
    private readonly List<OrganizationalUnit> _organizationalUnits = new();

    public MockActiveDirectoryService(ILogger<MockActiveDirectoryService> logger)
    {
        _logger = logger;
        
        // Initialize with some sample data
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Add sample OUs
        _organizationalUnits.Add(new OrganizationalUnit
        {
            Id = 1,
            Name = "Users",
            DistinguishedName = "OU=Users,DC=example,DC=com",
            Description = "Container for user accounts"
        });
        
        _organizationalUnits.Add(new OrganizationalUnit
        {
            Id = 2,
            Name = "Groups",
            DistinguishedName = "OU=Groups,DC=example,DC=com",
            Description = "Container for security groups"
        });
        
        _organizationalUnits.Add(new OrganizationalUnit
        {
            Id = 3,
            Name = "Service Accounts",
            DistinguishedName = "OU=Service Accounts,DC=example,DC=com",
            Description = "Container for service accounts"
        });
        
        // Add sample users
        _users.Add(new ActiveDirectoryObject
        {
            ObjectGuid = Guid.NewGuid().ToString(),
            Name = "John Doe",
            ObjectType = "User",
            DistinguishedName = "CN=John Doe,OU=Users,DC=example,DC=com",
            Attributes = new Dictionary<string, object>
            {
                ["sAMAccountName"] = "jdoe",
                ["userPrincipalName"] = "jdoe@example.com",
                ["givenName"] = "John",
                ["sn"] = "Doe",
                ["displayName"] = "John Doe",
                ["mail"] = "jdoe@example.com",
                ["telephoneNumber"] = "555-1234",
                ["department"] = "IT",
                ["whenCreated"] = DateTime.UtcNow.AddDays(-30).ToString(),
                ["whenChanged"] = DateTime.UtcNow.AddDays(-5).ToString(),
                ["userAccountControl"] = 512 // Enabled account
            }
        });
        
        _users.Add(new ActiveDirectoryObject
        {
            ObjectGuid = Guid.NewGuid().ToString(),
            Name = "Jane Smith",
            ObjectType = "User",
            DistinguishedName = "CN=Jane Smith,OU=Users,DC=example,DC=com",
            Attributes = new Dictionary<string, object>
            {
                ["sAMAccountName"] = "jsmith",
                ["userPrincipalName"] = "jsmith@example.com",
                ["givenName"] = "Jane",
                ["sn"] = "Smith",
                ["displayName"] = "Jane Smith",
                ["mail"] = "jsmith@example.com",
                ["telephoneNumber"] = "555-5678",
                ["department"] = "HR",
                ["whenCreated"] = DateTime.UtcNow.AddDays(-20).ToString(),
                ["whenChanged"] = DateTime.UtcNow.AddDays(-2).ToString(),
                ["userAccountControl"] = 512 // Enabled account
            }
        });
        
        // Add sample groups
        _groups.Add(new ActiveDirectoryObject
        {
            ObjectGuid = Guid.NewGuid().ToString(),
            Name = "IT Department",
            ObjectType = "Group",
            DistinguishedName = "CN=IT Department,OU=Groups,DC=example,DC=com",
            Attributes = new Dictionary<string, object>
            {
                ["sAMAccountName"] = "IT_Department",
                ["displayName"] = "IT Department",
                ["description"] = "IT Department group",
                ["whenCreated"] = DateTime.UtcNow.AddDays(-30).ToString(),
                ["whenChanged"] = DateTime.UtcNow.AddDays(-5).ToString()
            }
        });
        
        _groups.Add(new ActiveDirectoryObject
        {
            ObjectGuid = Guid.NewGuid().ToString(),
            Name = "HR Department",
            ObjectType = "Group",
            DistinguishedName = "CN=HR Department,OU=Groups,DC=example,DC=com",
            Attributes = new Dictionary<string, object>
            {
                ["sAMAccountName"] = "HR_Department",
                ["displayName"] = "HR Department",
                ["description"] = "HR Department group",
                ["whenCreated"] = DateTime.UtcNow.AddDays(-25).ToString(),
                ["whenChanged"] = DateTime.UtcNow.AddDays(-3).ToString()
            }
        });
        
        // Add sample gMSA accounts
        _gmsaAccounts.Add(new ActiveDirectoryObject
        {
            ObjectGuid = Guid.NewGuid().ToString(),
            Name = "WebService",
            ObjectType = "gMSA",
            DistinguishedName = "CN=WebService,OU=Service Accounts,DC=example,DC=com",
            Attributes = new Dictionary<string, object>
            {
                ["sAMAccountName"] = "WebService$",
                ["displayName"] = "Web Service Account",
                ["description"] = "gMSA for web services",
                ["whenCreated"] = DateTime.UtcNow.AddDays(-15).ToString(),
                ["whenChanged"] = DateTime.UtcNow.AddDays(-1).ToString()
            }
        });
        
        // Set up group memberships
        _groupMemberships[_groups[0].ObjectGuid] = new List<string> { _users[0].ObjectGuid };
        _groupMemberships[_groups[1].ObjectGuid] = new List<string> { _users[1].ObjectGuid };
    }

    // User operations
    public async Task<IEnumerable<ActiveDirectoryObject>> GetUsersAsync(string? filter = null)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return _users;
        }
        
        // Simple filter implementation
        return _users.Where(u => 
            u.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            u.Attributes.Any(a => a.Value.ToString()?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true));
    }

    public async Task<ActiveDirectoryObject?> GetUserByIdAsync(string id)
    {
        return _users.FirstOrDefault(u => u.ObjectGuid == id);
    }

    public async Task<ActiveDirectoryObject> CreateUserAsync(ActiveDirectoryObject user)
    {
        if (string.IsNullOrEmpty(user.ObjectGuid))
        {
            user.ObjectGuid = Guid.NewGuid().ToString();
        }
        
        user.Attributes["whenCreated"] = DateTime.UtcNow.ToString();
        user.Attributes["whenChanged"] = DateTime.UtcNow.ToString();
        
        _users.Add(user);
        
        return user;
    }

    public async Task<ActiveDirectoryObject?> UpdateUserAsync(string id, ActiveDirectoryObject user)
    {
        var existingUser = await GetUserByIdAsync(id);
        
        if (existingUser == null)
        {
            return null;
        }
        
        // Update properties
        existingUser.Name = user.Name;
        existingUser.DistinguishedName = user.DistinguishedName;
        
        // Update attributes
        foreach (var attr in user.Attributes)
        {
            existingUser.Attributes[attr.Key] = attr.Value;
        }
        
        existingUser.Attributes["whenChanged"] = DateTime.UtcNow.ToString();
        
        return existingUser;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await GetUserByIdAsync(id);
        
        if (user == null)
        {
            return false;
        }
        
        _users.Remove(user);
        
        // Remove from all groups
        foreach (var groupId in _groupMemberships.Keys)
        {
            _groupMemberships[groupId].Remove(id);
        }
        
        return true;
    }

    // Group operations
    public async Task<IEnumerable<ActiveDirectoryObject>> GetGroupsAsync(string? filter = null)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return _groups;
        }
        
        // Simple filter implementation
        return _groups.Where(g => 
            g.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            g.Attributes.Any(a => a.Value.ToString()?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true));
    }

    public async Task<ActiveDirectoryObject?> GetGroupByIdAsync(string id)
    {
        return _groups.FirstOrDefault(g => g.ObjectGuid == id);
    }

    public async Task<ActiveDirectoryObject> CreateGroupAsync(ActiveDirectoryObject group)
    {
        if (string.IsNullOrEmpty(group.ObjectGuid))
        {
            group.ObjectGuid = Guid.NewGuid().ToString();
        }
        
        group.Attributes["whenCreated"] = DateTime.UtcNow.ToString();
        group.Attributes["whenChanged"] = DateTime.UtcNow.ToString();
        
        _groups.Add(group);
        _groupMemberships[group.ObjectGuid] = new List<string>();
        
        return group;
    }

    public async Task<ActiveDirectoryObject?> UpdateGroupAsync(string id, ActiveDirectoryObject group)
    {
        var existingGroup = await GetGroupByIdAsync(id);
        
        if (existingGroup == null)
        {
            return null;
        }
        
        // Update properties
        existingGroup.Name = group.Name;
        existingGroup.DistinguishedName = group.DistinguishedName;
        
        // Update attributes
        foreach (var attr in group.Attributes)
        {
            existingGroup.Attributes[attr.Key] = attr.Value;
        }
        
        existingGroup.Attributes["whenChanged"] = DateTime.UtcNow.ToString();
        
        return existingGroup;
    }

    public async Task<bool> DeleteGroupAsync(string id)
    {
        var group = await GetGroupByIdAsync(id);
        
        if (group == null)
        {
            return false;
        }
        
        _groups.Remove(group);
        _groupMemberships.Remove(id);
        
        return true;
    }

    // gMSA operations
    public async Task<IEnumerable<ActiveDirectoryObject>> GetGMSAAccountsAsync(string? filter = null)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return _gmsaAccounts;
        }
        
        // Simple filter implementation
        return _gmsaAccounts.Where(g => 
            g.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            g.Attributes.Any(a => a.Value.ToString()?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true));
    }

    public async Task<ActiveDirectoryObject?> GetGMSAAccountByIdAsync(string id)
    {
        return _gmsaAccounts.FirstOrDefault(g => g.ObjectGuid == id);
    }

    public async Task<ActiveDirectoryObject> CreateGMSAAccountAsync(ActiveDirectoryObject gmsa)
    {
        if (string.IsNullOrEmpty(gmsa.ObjectGuid))
        {
            gmsa.ObjectGuid = Guid.NewGuid().ToString();
        }
        
        gmsa.Attributes["whenCreated"] = DateTime.UtcNow.ToString();
        gmsa.Attributes["whenChanged"] = DateTime.UtcNow.ToString();
        
        _gmsaAccounts.Add(gmsa);
        
        return gmsa;
    }

    public async Task<ActiveDirectoryObject?> UpdateGMSAAccountAsync(string id, ActiveDirectoryObject gmsa)
    {
        var existingGmsa = await GetGMSAAccountByIdAsync(id);
        
        if (existingGmsa == null)
        {
            return null;
        }
        
        // Update properties
        existingGmsa.Name = gmsa.Name;
        existingGmsa.DistinguishedName = gmsa.DistinguishedName;
        
        // Update attributes
        foreach (var attr in gmsa.Attributes)
        {
            existingGmsa.Attributes[attr.Key] = attr.Value;
        }
        
        existingGmsa.Attributes["whenChanged"] = DateTime.UtcNow.ToString();
        
        return existingGmsa;
    }

    public async Task<bool> DeleteGMSAAccountAsync(string id)
    {
        var gmsa = await GetGMSAAccountByIdAsync(id);
        
        if (gmsa == null)
        {
            return false;
        }
        
        _gmsaAccounts.Remove(gmsa);
        
        return true;
    }

    // Group membership operations
    public async Task<bool> AddUserToGroupAsync(string userId, string groupId)
    {
        var user = await GetUserByIdAsync(userId);
        var group = await GetGroupByIdAsync(groupId);
        
        if (user == null || group == null)
        {
            return false;
        }
        
        if (!_groupMemberships.ContainsKey(groupId))
        {
            _groupMemberships[groupId] = new List<string>();
        }
        
        if (!_groupMemberships[groupId].Contains(userId))
        {
            _groupMemberships[groupId].Add(userId);
        }
        
        return true;
    }

    public async Task<bool> RemoveUserFromGroupAsync(string userId, string groupId)
    {
        if (!_groupMemberships.ContainsKey(groupId))
        {
            return false;
        }
        
        return _groupMemberships[groupId].Remove(userId);
    }

    public async Task<IEnumerable<ActiveDirectoryObject>> GetGroupMembersAsync(string groupId)
    {
        if (!_groupMemberships.ContainsKey(groupId))
        {
            return Enumerable.Empty<ActiveDirectoryObject>();
        }
        
        var memberIds = _groupMemberships[groupId];
        var members = new List<ActiveDirectoryObject>();
        
        foreach (var memberId in memberIds)
        {
            var user = await GetUserByIdAsync(memberId);
            if (user != null)
            {
                members.Add(user);
                continue;
            }
            
            var group = await GetGroupByIdAsync(memberId);
            if (group != null)
            {
                members.Add(group);
                continue;
            }
            
            var gmsa = await GetGMSAAccountByIdAsync(memberId);
            if (gmsa != null)
            {
                members.Add(gmsa);
            }
        }
        
        return members;
    }

    public async Task<IEnumerable<ActiveDirectoryObject>> GetUserGroupsAsync(string userId)
    {
        var groups = new List<ActiveDirectoryObject>();
        
        foreach (var groupId in _groupMemberships.Keys)
        {
            if (_groupMemberships[groupId].Contains(userId))
            {
                var group = await GetGroupByIdAsync(groupId);
                if (group != null)
                {
                    groups.Add(group);
                }
            }
        }
        
        return groups;
    }

    // OU operations
    public async Task<IEnumerable<string>> GetOrganizationalUnitsAsync()
    {
        return _organizationalUnits.Select(ou => ou.DistinguishedName).ToList();
    }

    public async Task<bool> MoveObjectToOUAsync(string objectId, string ouDistinguishedName)
    {
        // Check if OU exists
        if (!_organizationalUnits.Any(ou => ou.DistinguishedName == ouDistinguishedName))
        {
            return false;
        }
        
        // Try to find the object
        var user = await GetUserByIdAsync(objectId);
        if (user != null)
        {
            // Update the distinguished name
            var cn = user.DistinguishedName.Split(',')[0];
            user.DistinguishedName = $"{cn},{ouDistinguishedName}";
            return true;
        }
        
        var group = await GetGroupByIdAsync(objectId);
        if (group != null)
        {
            // Update the distinguished name
            var cn = group.DistinguishedName.Split(',')[0];
            group.DistinguishedName = $"{cn},{ouDistinguishedName}";
            return true;
        }
        
        var gmsa = await GetGMSAAccountByIdAsync(objectId);
        if (gmsa != null)
        {
            // Update the distinguished name
            var cn = gmsa.DistinguishedName.Split(',')[0];
            gmsa.DistinguishedName = $"{cn},{ouDistinguishedName}";
            return true;
        }
        
        return false;
    }
}