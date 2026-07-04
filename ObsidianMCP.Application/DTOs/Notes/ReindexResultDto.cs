namespace ObsidianMCP.Application.DTOs.Notes;

public sealed class ReindexResultDto
{
    public required int Added { get; init; }
    public required int Updated { get; init; }
    public required int Removed { get; init; }
    public required IReadOnlyList<string> AddedPaths { get; init; }
    public required IReadOnlyList<string> UpdatedPaths { get; init; }
    public required IReadOnlyList<string> RemovedPaths { get; init; }
    public required int TotalDocuments { get; init; }
    public required long DurationMs { get; init; }
    public required DateTime CompletedAtUtc { get; init; }
}
