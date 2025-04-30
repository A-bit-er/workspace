using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADScimApi.SCIM;
using ADScimApi.SCIM.Models;
using ADScimApi.Services;
using ADScimApi.Business;

namespace ADScimApi.Controllers;

[ApiController]
[Route("scim/v2/Users")]
[Authorize]
public class ScimUsersController : ControllerBase
{
    private readonly IScimService _scimService;
    private readonly IBusinessRuleService _businessRuleService;
    private readonly ILogger<ScimUsersController> _logger;

    public ScimUsersController(
        IScimService scimService,
        IBusinessRuleService businessRuleService,
        ILogger<ScimUsersController> logger)
    {
        _scimService = scimService;
        _businessRuleService = businessRuleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ScimListResponse<ScimUser>>> GetUsers(
        [FromQuery] string? filter = null,
        [FromQuery] int startIndex = 1,
        [FromQuery] int count = 100,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        try
        {
            var users = await _scimService.GetUsersAsync(filter, startIndex, count, sortBy, sortOrder);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return BadRequest(new ScimError
            {
                Status = "400",
                ScimType = "invalidFilter",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ScimUser>> GetUser(string id)
    {
        try
        {
            var user = await _scimService.GetUserAsync(id);
            if (user == null)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"User with id {id} not found"
                });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpPost]
    [Authorize(Policy = "UserManager")]
    public async Task<ActionResult<ScimUser>> CreateUser(ScimUser user)
    {
        try
        {
            // Apply business rules to determine OU
            var targetOU = await _businessRuleService.DetermineTargetOUAsync("User", user);
            
            // Create user
            var createdUser = await _scimService.CreateUserAsync(user, targetOU);
            
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, createdUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "UserManager")]
    public async Task<ActionResult<ScimUser>> UpdateUser(string id, ScimUser user)
    {
        if (id != user.Id)
        {
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = "Id in URL does not match Id in request body"
            });
        }

        try
        {
            var updatedUser = await _scimService.UpdateUserAsync(id, user);
            if (updatedUser == null)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"User with id {id} not found"
                });
            }

            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = "UserManager")]
    public async Task<ActionResult<ScimUser>> PatchUser(string id, ScimPatchRequest patchRequest)
    {
        try
        {
            var updatedUser = await _scimService.PatchUserAsync(id, patchRequest);
            if (updatedUser == null)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"User with id {id} not found"
                });
            }

            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching user {UserId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "UserManager")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var result = await _scimService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound(new ScimError
                {
                    Status = "404",
                    Detail = $"User with id {id} not found"
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return BadRequest(new ScimError
            {
                Status = "400",
                Detail = ex.Message
            });
        }
    }
}