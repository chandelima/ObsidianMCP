namespace ObsidianMCP.Application.DTOs.Notes;

public sealed class SearchResponseDto
{
    public required string Query { get; init; }
    public required int TotalHits { get; init; }
    public required IReadOnlyList<SearchResultDto> Results { get; init; }
}
