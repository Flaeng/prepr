namespace Prepr.Models;

public class LineLimitRule(Dictionary<string, int>? configRules, int? cliDefault)
    : PathBasedRule(configRules, cliDefault);
