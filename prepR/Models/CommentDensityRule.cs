namespace Prepr.Models;

public class MinCommentDensityRule : PathBasedRule
{
    public MinCommentDensityRule(Dictionary<string, int>? configRules, int? cliDefault)
        : base(configRules, cliDefault) { }
}

public class MaxCommentDensityRule : PathBasedRule
{
    public MaxCommentDensityRule(Dictionary<string, int>? configRules, int? cliDefault)
        : base(configRules, cliDefault) { }
}
