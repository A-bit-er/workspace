using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADScimApi.SCIM;
using ADScimApi.SCIM.Models;
using ADScimApi.Services;
using ADScimApi.Business;

namespace ADScimApi.Controllers;

[ApiController]
[Route("scim/v2/Groups")]
[Authorize]
public class ScimGroupsController : ControllerBase
{
    private readonly IScimService _scimService;
    private readonly IBusinessRuleService _businessRuleService;
    private readonly ILogger<ScimGroupsController> _logger;

    public ScimGroupsController(
        IScimService scimService,
        IBusinessRuleService businessRuleService,
        ILogger<ScimGroupsController> logger)
    {
        _scimService = scimService;
        _businessRuleService = businessRuleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ScimListResponse<ScimGroup>>> GetGroups(
        [FromQuery] string? filter = null,
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        try
        {
            var groups = await _scimService.GetGroupsAsync(filter, startIndex, count, sortBy, sortOrder);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups");
            return BadRequest(new ScimError
            {
                Status = "400",
                ScimType = "invalidFilter",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScimGroup>> GetGroup(string id)
    {
        try
        {
            var group = await _scimService.GetGroupAsync(id);
            if (group == null)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"Group with id {id} not found"
                });
            }

            return Ok(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group {GroupId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpPost]
    [Authorize(Policy = "GroupManager")]
    public async Task<ActionResult<ScimGroup>> CreateGroup(ScimGroup group)
    {
        try
        {
            // Apply business rules to determine OU
            var targetOU = await _businessRuleService.DetermineTargetOUAsync("Group", group);
            
            // Create group
            var createdGroup = await _scimService.CreateGroupAsync(group, targetOU);
            
            return CreatedAtAction(nameof(GetGroup), new { id = createdGroup.Id }, createdGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "GroupManager")]
    public async Task<ActionResult<ScimGroup>> UpdateGroup(string id, ScimGroup group)
    {
        if (id != group.Id)
        {
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = "Id in URL does not match Id in request body"
            });
        }

        try
        {
            var updatedGroup = await _scimService.UpdateGroupAsync(id, group);
            if (updatedGroup == null)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"Group with id {id} not found"
                });
            }

            return Ok(updatedGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "GroupManager")]
    public async Task<ActionResult<ScimGroup>> PatchGroup(string id, ScimPatchRequest patchRequest)
    {
        try
        {
            var updatedGroup = await _scimService.PatchGroupAsync(id, patchRequest);
            if (updatedGroup == null)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"Group with id {id} not found"
                });
            }

            return Ok(updatedGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching group {GroupId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "GroupManager")]
    public async Task<IActionResult> DeleteGroup(string id)
    {
        try
        {
            var result = await _scimService.DeleteGroupAsync(id);
            if (!result)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"Group with id {id} not found"
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }
}