namespace ObsidianMCP.Application.DTOs.Notes;

public sealed class NoteContentDto
{
    public required string Path { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
}
