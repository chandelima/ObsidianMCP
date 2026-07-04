namespace ObsidianMCP.Application.DTOs.Notes;

public sealed class SearchResultDto
{
    public required string Path { get; init; }
    public required string Title { get; init; }
    public required float Score { get; init; }
    public required DateTime LastModifiedUtc { get; init; }
    public string? Snippet { get; init; }
}
