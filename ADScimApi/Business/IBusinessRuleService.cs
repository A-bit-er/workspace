using ADScimApi.Models;

namespace ADScimApi.Business;

public interface IBusinessRuleService
{
    Task<IEnumerable<BusinessRule>> GetBusinessRulesAsync();
    Task<BusinessRule?> GetBusinessRuleByIdAsync(int id);
    Task<BusinessRule> CreateBusinessRuleAsync(BusinessRule rule);
    Task<BusinessRule?> UpdateBusinessRuleAsync(int id, BusinessRule rule);
    Task<bool> DeleteBusinessRuleAsync(int id);
    
    // Rule evaluation
    Task<string> EvaluateRulesForObjectAsync(ActiveDirectoryObject adObject);
    Task<bool> ValidateRuleConditionAsync(string condition, ActiveDirectoryObject adObject);
    Task<string> DetermineTargetOUAsync(ActiveDirectoryObject adObject);
}