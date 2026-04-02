namespace Prepr.Models;

public class MagicStringRule : PathBasedRule
{
    public MagicStringRule(Dictionary<string, int>? configRules, int? cliDefault)
        : base(configRules, cliDefault) { }
}
