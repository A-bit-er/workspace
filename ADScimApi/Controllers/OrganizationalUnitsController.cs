using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADScimApi.Models;
using ADScimApi.Services;
using ADScimApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ADScimApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationalUnitsController : ControllerBase
{
    private readonly IActiveDirectoryService _adService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrganizationalUnitsController> _logger;

    public OrganizationalUnitsController(
        IActiveDirectoryService adService,
        ApplicationDbContext context,
        ILogger<OrganizationalUnitsController> logger)
    {
        _adService = adService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationalUnit>>> GetOrganizationalUnits()
    {
        return await _context.OrganizationalUnits.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationalUnit>> GetOrganizationalUnit(int id)
    {
        var ou = await _context.OrganizationalUnits.FindAsync(id);

        if (ou == null)
        {
            return NotFound();
        }

        return ou;
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<OrganizationalUnit>> CreateOrganizationalUnit(OrganizationalUnit ou)
    {
        try
        {
            // Add to database only for now
            _context.OrganizationalUnits.Add(ou);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrganizationalUnit), new { id = ou.Id }, ou);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organizational unit");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateOrganizationalUnit(int id, OrganizationalUnit ou)
    {
        if (id != ou.Id)
        {
            return BadRequest();
        }

        try
        {
            // Get existing OU
            var existingOU = await _context.OrganizationalUnits.FindAsync(id);
            if (existingOU == null)
            {
                return NotFound();
            }

            // Update in database only for now

            // Update in database
            existingOU.Name = ou.Name;
            existingOU.Description = ou.Description;
            // Note: We don't update DistinguishedName as it's the identifier in AD

            _context.Entry(existingOU).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organizational unit");
            
            if (!OrganizationalUnitExists(id))
            {
                return NotFound();
            }
            
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteOrganizationalUnit(int id)
    {
        var ou = await _context.OrganizationalUnits.FindAsync(id);
        if (ou == null)
        {
            return NotFound();
        }

        try
        {
            // Delete from database only for now
            
            // Delete from database
            _context.OrganizationalUnits.Remove(ou);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting organizational unit");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SyncOrganizationalUnits()
    {
        try
        {
            // Get OUs from Active Directory
            var adOUs = await _adService.GetOrganizationalUnitsAsync();
            
            // Get existing OUs from database
            var dbOUs = await _context.OrganizationalUnits.ToListAsync();
            
            // Add new OUs
            foreach (var adOU in adOUs)
            {
                if (!dbOUs.Any(o => o.DistinguishedName == adOU.DistinguishedName))
                {
                    _context.OrganizationalUnits.Add(adOU);
                }
            }
            
            // Remove OUs that no longer exist in AD
            foreach (var dbOU in dbOUs)
            {
                if (!adOUs.Any(o => o.DistinguishedName == dbOU.DistinguishedName))
                {
                    _context.OrganizationalUnits.Remove(dbOU);
                }
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Organizational units synchronized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error synchronizing organizational units");
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool OrganizationalUnitExists(int id)
    {
        return _context.OrganizationalUnits.Any(e => e.Id == id);
    }
}