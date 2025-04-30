using System.Text.Json;
using ADScimApi.Data;
using ADScimApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ADScimApi.Business;

public class BusinessRuleService : IBusinessRuleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BusinessRuleService> _logger;

    public BusinessRuleService(ILogger<BusinessRuleService> logger)
    {
        _logger = logger;
        _dbContext = null!; // Will be initialized in production
    }

    public async Task<IEnumerable<BusinessRule>> GetBusinessRulesAsync()
    {
        return await _dbContext.BusinessRules
            .Include(r => r.OrganizationalUnit)
            .ToListAsync();
    }

    public async Task<BusinessRule?> GetBusinessRuleByIdAsync(int id)
    {
        return await _dbContext.BusinessRules
            .Include(r => r.OrganizationalUnit)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<BusinessRule> CreateBusinessRuleAsync(BusinessRule rule)
    {
        rule.CreatedAt = DateTime.UtcNow;
        
        _dbContext.BusinessRules.Add(rule);
        await _dbContext.SaveChangesAsync();
        
        return rule;
    }

    public async Task<BusinessRule?> UpdateBusinessRuleAsync(int id, BusinessRule rule)
    {
        var existingRule = await _dbContext.BusinessRules.FindAsync(id);
        
        if (existingRule == null)
        {
            return null;
        }
        
        existingRule.Name = rule.Name;
        existingRule.Description = rule.Description;
        existingRule.ObjectType = rule.ObjectType;
        existingRule.Condition = rule.Condition;
        existingRule.OrganizationalUnitId = rule.OrganizationalUnitId;
        existingRule.Priority = rule.Priority;
        existingRule.IsActive = rule.IsActive;
        existingRule.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync();
        
        return existingRule;
    }

    public async Task<bool> DeleteBusinessRuleAsync(int id)
    {
        var rule = await _dbContext.BusinessRules.FindAsync(id);
        
        if (rule == null)
        {
            return false;
        }
        
        _dbContext.BusinessRules.Remove(rule);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }

    public async Task<string> EvaluateRulesForObjectAsync(ActiveDirectoryObject adObject)
    {
        // Get all active rules for this object type, ordered by priority
        var rules = await _dbContext.BusinessRules
            .Include(r => r.OrganizationalUnit)
            .Where(r => r.IsActive && r.ObjectType == adObject.ObjectType)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();
        
        foreach (var rule in rules)
        {
            try
            {
                bool conditionMet = await ValidateRuleConditionAsync(rule.Condition, adObject);
                
                if (conditionMet && rule.OrganizationalUnit != null)
                {
                    return rule.OrganizationalUnit.DistinguishedName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule {RuleId} for object {ObjectName}", rule.Id, adObject.Name);
            }
        }
        
        // Return default OU if no rules match
        var defaultOu = await _dbContext.OrganizationalUnits.FirstOrDefaultAsync();
        return defaultOu?.DistinguishedName ?? string.Empty;
    }

    public async Task<bool> ValidateRuleConditionAsync(string condition, ActiveDirectoryObject adObject)
    {
        // For development/testing, always return true
        return true;
    }
    
    public async Task<string> DetermineTargetOUAsync(ActiveDirectoryObject adObject)
    {
        // For development/testing, return a default OU based on object type
        switch (adObject.ObjectType.ToLower())
        {
            case "user":
                return "OU=Users,DC=example,DC=com";
            case "group":
                return "OU=Groups,DC=example,DC=com";
            case "gmsa":
                return "OU=Service Accounts,DC=example,DC=com";
            default:
                return "DC=example,DC=com";
        }
    }
}