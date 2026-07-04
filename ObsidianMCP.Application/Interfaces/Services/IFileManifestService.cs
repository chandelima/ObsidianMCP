using ObsidianMCP.Domain.Models;

namespace ObsidianMCP.Application.Interfaces.Services;

public interface IFileManifestService
{
    Task<VaultManifestModel> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(VaultManifestModel manifest, CancellationToken ct = default);

    /// <summary>Scans the vault for .md files, skipping ".obsidian". Returns relative path -> last write time (UTC).</summary>
    Dictionary<string, DateTime> ScanVault(string vaultRootPath);
}
