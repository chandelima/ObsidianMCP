namespace ObsidianMCP.Domain.Models;

public sealed class VaultManifestModel
{
    public DateTime GeneratedAtUtc { get; set; }
    public Dictionary<string, DateTime> Files { get; set; } = new();
}
