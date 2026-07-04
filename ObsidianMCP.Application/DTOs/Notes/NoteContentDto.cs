namespace ObsidianMCP.Application.DTOs.Notes;

/// <summary>Full content of a single note.</summary>
public sealed class NoteContentDto
{
    /// <summary>Vault-relative path of the note.</summary>
    public required string Path { get; init; }

    /// <summary>Resolved title of the note (from its "leafwiki_title" frontmatter, its first H1 heading, or its file name).</summary>
    public required string Title { get; init; }

    /// <summary>Full markdown body of the note, with the YAML frontmatter block already stripped.</summary>
    public required string Content { get; init; }
}
