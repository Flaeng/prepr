namespace Prepr.Models;

public class MagicNumberRule : PathBasedRule
{
    public MagicNumberRule(Dictionary<string, int>? configRules, int? cliDefault)
        : base(configRules, cliDefault) { }
}
