namespace ObsidianMCP.Application.DTOs.Notes;

/// <summary>A single search hit.</summary>
public sealed class SearchResultDto
{
    /// <summary>Vault-relative path of the note. Pass this as-is to <c>GET /api/notes?path=...</c> to read the full content.</summary>
    public required string Path { get; init; }

    /// <summary>Resolved title of the note (from its "leafwiki_title" frontmatter, its first H1 heading, or its file name).</summary>
    public required string Title { get; init; }

    /// <summary>Lucene relevance score for this hit. Higher means more relevant; not comparable across different queries.</summary>
    public required float Score { get; init; }

    /// <summary>Last-modified timestamp of the underlying file (UTC), as of the last reindex.</summary>
    public required DateTime LastModifiedUtc { get; init; }

    /// <summary>HTML snippet with matched terms wrapped in &lt;mark&gt; tags, taken from the note's content. Null if no match was found in the content itself (e.g. the match was only in the title).</summary>
    public string? Snippet { get; init; }
}
