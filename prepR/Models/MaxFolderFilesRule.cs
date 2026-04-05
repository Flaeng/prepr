namespace Prepr.Models;

public class MaxFolderFilesRule(Dictionary<string, int>? configRules, int? cliDefault)
    : PathBasedRule(configRules, cliDefault);
