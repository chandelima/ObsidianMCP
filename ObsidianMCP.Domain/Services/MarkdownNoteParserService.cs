namespace ObsidianMCP.Domain.Services;

public static class MarkdownNoteParserService
{
    private const string FrontmatterDelimiter = "---";
    private const string LeafWikiTitleKey = "leafwiki_title";

    /// <summary>
    /// Removes the leading YAML frontmatter block (if any) and returns the "leafwiki_title" value
    /// found inside it, along with the remaining markdown body.
    /// </summary>
    public static (string? FrontmatterTitle, string Body) StripFrontmatter(string rawText)
    {
        var lines = rawText.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != FrontmatterDelimiter)
        {
            return (null, rawText);
        }

        var closingIndex = -1;
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == FrontmatterDelimiter)
            {
                closingIndex = i;
                break;
            }
        }

        if (closingIndex == -1)
        {
            return (null, rawText);
        }

        string? title = null;
        for (var i = 1; i < closingIndex; i++)
        {
            var separatorIndex = lines[i].IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = lines[i][..separatorIndex].Trim();
            if (!key.Equals(LeafWikiTitleKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            title = lines[i][(separatorIndex + 1)..].Trim().Trim('"');
            break;
        }

        var body = string.Join('\n', lines.Skip(closingIndex + 1));
        return (title, body);
    }

    /// <summary>Returns the text of the first level-1 heading ("# Title") found in the body, if any.</summary>
    public static string? ExtractFirstH1(string body)
    {
        foreach (var rawLine in body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                return line[2..].Trim();
            }
        }

        return null;
    }

    public static string ResolveTitle(string? frontmatterTitle, string? h1Title, string fileNameWithoutExtension)
    {
        if (!string.IsNullOrWhiteSpace(frontmatterTitle))
        {
            return frontmatterTitle;
        }

        if (!string.IsNullOrWhiteSpace(h1Title))
        {
            return h1Title;
        }

        return fileNameWithoutExtension;
    }
}
