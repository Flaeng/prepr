namespace Prepr;

public static class EarlyReturnAnalyzer
{
    public static List<EarlyReturnViolation> Analyze(IndexedLine[] lines)
    {
        var violations = new List<EarlyReturnViolation>();

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Text.Trim();

            if (IsElseOpener(trimmed))
            {
                CheckElseBlock(lines, i, violations);
                continue;
            }

            if (IsIfOpener(trimmed, lines, i))
            {
                CheckIfWrapsRemainingBody(lines, i, violations);
            }
        }

        return violations;
    }

    private static void CheckElseBlock(IndexedLine[] lines, int i, List<EarlyReturnViolation> violations)
    {
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

    private static bool IsIfOpener(string trimmed, IndexedLine[] lines, int index)
    {
        // Match "if (...)" or "if (...) {" patterns
        if (!trimmed.StartsWith("if ", StringComparison.Ordinal) &&
            !trimmed.StartsWith("if(", StringComparison.Ordinal))
            return false;

        // Must not be an "else if"
        if (index > 0 && lines[index - 1].Text.Trim().EndsWith("else", StringComparison.Ordinal))
            return false;

        return true;
    }

    private static void CheckIfWrapsRemainingBody(IndexedLine[] lines, int ifIndex, List<EarlyReturnViolation> violations)
    {
        // Find the opening brace for the if block
        int braceIndex = -1;
        var ifTrimmed = lines[ifIndex].Text.Trim();
        if (ifTrimmed.EndsWith("{", StringComparison.Ordinal))
        {
            braceIndex = ifIndex;
        }
        else
        {
            // Look for { on the next line
            for (int k = ifIndex + 1; k < lines.Length && k <= ifIndex + 2; k++)
            {
                var t = lines[k].Text.Trim();
                if (t == "{")
                {
                    braceIndex = k;
                    break;
                }
                if (t.Length > 0)
                    break;
            }
        }

        if (braceIndex < 0)
            return;

        // Find the matching closing brace
        int depth = 1;
        int closeIndex = -1;
        for (int j = braceIndex + 1; j < lines.Length && depth > 0; j++)
        {
            foreach (var ch in lines[j].Text)
            {
                if (ch == '{')
                    depth++;
                else if (ch == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        closeIndex = j;
                        break;
                    }
                }
            }
        }

        if (closeIndex < 0)
            return;

        // Check there's no else after the closing brace
        var afterClose = lines[closeIndex].Text.Trim();
        if (afterClose.Contains("else", StringComparison.OrdinalIgnoreCase))
            return;
        if (closeIndex + 1 < lines.Length)
        {
            var nextLine = lines[closeIndex + 1].Text.Trim();
            if (nextLine.StartsWith("else", StringComparison.Ordinal))
                return;
        }

        // Determine the indentation level of the if statement to find its enclosing scope
        int ifIndent = GetIndentLevel(lines[ifIndex].Text);

        // Check that only closing braces (and whitespace) follow after the if block
        // until a closing brace at or below the if's indentation (end of enclosing method)
        bool onlyClosingBraces = true;
        for (int j = closeIndex + 1; j < lines.Length; j++)
        {
            var t = lines[j].Text.Trim();
            if (t.Length == 0)
                continue;
            // A closing brace at a lower indent level means we reached the enclosing scope's end
            if (t == "}" && GetIndentLevel(lines[j].Text) < ifIndent)
                break;
            // Allow only lines that are just closing braces
            if (t.All(c => c == '}'))
                continue;
            onlyClosingBraces = false;
            break;
        }

        if (!onlyClosingBraces)
            return;

        // The if block must contain more than 1 line of content to be worth flagging
        int contentLines = 0;
        for (int j = braceIndex + 1; j < closeIndex; j++)
        {
            if (lines[j].Text.Trim().Length > 0)
                contentLines++;
        }

        if (contentLines < 2)
            return;

        violations.Add(new EarlyReturnViolation(
            lines[ifIndex].LineNumber,
            "if block wraps remaining method body — consider inverting to a guard clause with early return"));
    }

    private static int GetIndentLevel(string line)
    {
        int count = 0;
        foreach (var ch in line)
        {
            if (ch == ' ') count++;
            else if (ch == '\t') count += 4;
            else break;
        }
        return count;
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
