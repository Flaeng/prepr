namespace Prepr.Models;

public class IndentationRule(Dictionary<string, int>? configRules, int? cliDefault)
    : PathBasedRule(configRules, cliDefault);
