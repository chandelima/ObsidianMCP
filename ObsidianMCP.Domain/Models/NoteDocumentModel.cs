namespace ObsidianMCP.Domain.Models;

public sealed class NoteDocumentModel
{
    public required string Path { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required DateTime LastModifiedUtc { get; init; }
}
