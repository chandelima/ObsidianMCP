namespace ObsidianMCP.Application.DTOs.Notes;

/// <summary>Result of a vault search.</summary>
public sealed class SearchResponseDto
{
    /// <summary>The search terms that were used, echoed back as received.</summary>
    public required string Query { get; init; }

    /// <summary>Total number of matching notes found (may be larger than <see cref="Results"/>.Count if the result set was capped by <c>max</c>).</summary>
    public required int TotalHits { get; init; }

    /// <summary>The matching notes, ordered by relevance (most relevant first).</summary>
    public required IReadOnlyList<SearchResultDto> Results { get; init; }
}
