namespace ObsidianMCP.Application.DTOs.Notes;

/// <summary>Summary of an incremental reindex pass.</summary>
public sealed class ReindexResultDto
{
    /// <summary>Number of notes that were newly added to the index.</summary>
    public required int Added { get; init; }

    /// <summary>Number of notes that were already indexed and got their content refreshed.</summary>
    public required int Updated { get; init; }

    /// <summary>Number of notes that were removed from the index because the underlying file no longer exists.</summary>
    public required int Removed { get; init; }

    /// <summary>Vault-relative paths of the notes that were added.</summary>
    public required IReadOnlyList<string> AddedPaths { get; init; }

    /// <summary>Vault-relative paths of the notes that were updated.</summary>
    public required IReadOnlyList<string> UpdatedPaths { get; init; }

    /// <summary>Vault-relative paths of the notes that were removed.</summary>
    public required IReadOnlyList<string> RemovedPaths { get; init; }

    /// <summary>Total number of notes in the index after this reindex pass completed.</summary>
    public required int TotalDocuments { get; init; }

    /// <summary>How long the reindex pass took, in milliseconds.</summary>
    public required long DurationMs { get; init; }

    /// <summary>UTC timestamp of when this reindex pass finished.</summary>
    public required DateTime CompletedAtUtc { get; init; }
}
