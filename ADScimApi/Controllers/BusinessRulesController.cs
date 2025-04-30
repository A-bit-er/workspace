using ADScimApi.Business;
using ADScimApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADScimApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class BusinessRulesController : ControllerBase
{
    private readonly IBusinessRuleService _businessRuleService;
    private readonly ILogger<BusinessRulesController> _logger;

    public BusinessRulesController(IBusinessRuleService businessRuleService, ILogger<BusinessRulesController> logger)
    {
        _businessRuleService = businessRuleService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBusinessRules()
    {
        var rules = await _businessRuleService.GetBusinessRulesAsync();
        return Ok(rules);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBusinessRuleById(int id)
    {
        var rule = await _businessRuleService.GetBusinessRuleByIdAsync(id);
        
        if (rule == null)
        {
            return NotFound();
        }
        
        return Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBusinessRule([FromBody] BusinessRule rule)
    {
        var createdRule = await _businessRuleService.CreateBusinessRuleAsync(rule);
        return CreatedAtAction(nameof(GetBusinessRuleById), new { id = createdRule.Id }, createdRule);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBusinessRule(int id, [FromBody] BusinessRule rule)
    {
        var updatedRule = await _businessRuleService.UpdateBusinessRuleAsync(id, rule);
        
        if (updatedRule == null)
        {
            return NotFound();
        }
        
        return Ok(updatedRule);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBusinessRule(int id)
    {
        var success = await _businessRuleService.DeleteBusinessRuleAsync(id);
        
        if (!success)
        {
            return NotFound();
        }
        
        return NoContent();
    }
}