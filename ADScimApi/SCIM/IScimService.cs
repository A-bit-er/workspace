using ADScimApi.SCIM.Models;

namespace ADScimApi.SCIM;

public interface IScimService
{
    // User operations
    Task<ScimListResponse<ScimUser>> GetUsersAsync(string? filter = null, int? startIndex = null, int? count = null);
    Task<ScimUser?> GetUserByIdAsync(string id);
    Task<ScimUser> CreateUserAsync(ScimUser user);
    Task<ScimUser?> UpdateUserAsync(string id, ScimUser user);
    Task<ScimUser?> PatchUserAsync(string id, ScimPatchRequest patchRequest);
    Task<bool> DeleteUserAsync(string id);
    
    // Group operations
    Task<ScimListResponse<ScimGroup>> GetGroupsAsync(string? filter = null, int? startIndex = null, int? count = null);
    Task<ScimGroup?> GetGroupByIdAsync(string id);
    Task<ScimGroup> CreateGroupAsync(ScimGroup group);
    Task<ScimGroup?> UpdateGroupAsync(string id, ScimGroup group);
    Task<ScimGroup?> PatchGroupAsync(string id, ScimPatchRequest patchRequest);
    Task<bool> DeleteGroupAsync(string id);
}