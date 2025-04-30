using ADScimApi.Models;

namespace ADScimApi.Services;

public interface IActiveDirectoryService
{
    // User operations
    Task<IEnumerable<ActiveDirectoryObject>> GetUsersAsync(string? filter = null);
    Task<ActiveDirectoryObject?> GetUserByIdAsync(string id);
    Task<ActiveDirectoryObject> CreateUserAsync(ActiveDirectoryObject user);
    Task<ActiveDirectoryObject?> UpdateUserAsync(string id, ActiveDirectoryObject user);
    Task<bool> DeleteUserAsync(string id);

    // Group operations
    Task<IEnumerable<ActiveDirectoryObject>> GetGroupsAsync(string? filter = null);
    Task<ActiveDirectoryObject?> GetGroupByIdAsync(string id);
    Task<ActiveDirectoryObject> CreateGroupAsync(ActiveDirectoryObject group);
    Task<ActiveDirectoryObject?> UpdateGroupAsync(string id, ActiveDirectoryObject group);
    Task<bool> DeleteGroupAsync(string id);

    // gMSA operations
    Task<IEnumerable<ActiveDirectoryObject>> GetGMSAAccountsAsync(string? filter = null);
    Task<ActiveDirectoryObject?> GetGMSAAccountByIdAsync(string id);
    Task<ActiveDirectoryObject> CreateGMSAAccountAsync(ActiveDirectoryObject gmsa);
    Task<ActiveDirectoryObject?> UpdateGMSAAccountAsync(string id, ActiveDirectoryObject gmsa);
    Task<bool> DeleteGMSAAccountAsync(string id);

    // Group membership operations
    Task<bool> AddUserToGroupAsync(string userId, string groupId);
    Task<bool> RemoveUserFromGroupAsync(string userId, string groupId);
    Task<IEnumerable<ActiveDirectoryObject>> GetGroupMembersAsync(string groupId);
    Task<IEnumerable<ActiveDirectoryObject>> GetUserGroupsAsync(string userId);

    // OU operations
    Task<IEnumerable<string>> GetOrganizationalUnitsAsync();
    Task<bool> MoveObjectToOUAsync(string objectId, string ouDistinguishedName);
}