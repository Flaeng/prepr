namespace Prepr;

public static class EarlyReturnAnalyzer
{
    public static List<EarlyReturnViolation> Analyze(IndexedLine[] lines)
    {
        var violations = new List<EarlyReturnViolation>();

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Text.Trim();

            if (!IsElseOpener(trimmed))
                continue;

            // Found a } else { — scan the else block
            int depth = 1;
            int blockLineCount = 0;
            bool hasReturnOrThrow = false;
            int j = i + 1;

            while (j < lines.Length && depth > 0)
            {
                var innerTrimmed = lines[j].Text.Trim();

                foreach (var ch in innerTrimmed)
                {
                    if (ch == '{')
                        depth++;
                    else if (ch == '}')
                        depth--;
                }

                if (depth > 0)
                {
                    blockLineCount++;
                    if (ContainsReturnOrThrow(innerTrimmed))
                        hasReturnOrThrow = true;
                }

                j++;
            }

            if (blockLineCount <= 3 && hasReturnOrThrow)
            {
                violations.Add(new EarlyReturnViolation(
                    lines[i].LineNumber,
                    "Short else block with return/throw — consider inverting to a guard clause"));
            }
        }

        return violations;
    }

    private static bool IsElseOpener(string trimmed)
    {
        return trimmed == "} else {" ||
               trimmed == "} else{" ||
               trimmed == "}else {" ||
               trimmed == "}else{" ||
               trimmed.StartsWith("} else {", StringComparison.Ordinal) ||
               trimmed.StartsWith("}else{", StringComparison.Ordinal);
    }

    private static bool ContainsReturnOrThrow(string trimmed)
    {
        return trimmed.StartsWith("return", StringComparison.Ordinal) ||
               trimmed.StartsWith("throw", StringComparison.Ordinal) ||
               trimmed.Contains(" return ", StringComparison.Ordinal) ||
               trimmed.Contains(" throw ", StringComparison.Ordinal);
    }
}
