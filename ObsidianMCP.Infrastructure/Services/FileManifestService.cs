using System.Text.Json;
using ObsidianMCP.Application.Interfaces.Services;
using ObsidianMCP.Domain.Models;
using ObsidianMCP.Infrastructure.Settings;

namespace ObsidianMCP.Infrastructure.Services;

internal sealed class FileManifestService(ObsidianSettings settings) : IFileManifestService
{
    private const string IgnoredDirectoryName = ".obsidian";

    public async Task<VaultManifestModel> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(settings.ManifestPath))
        {
            return new VaultManifestModel();
        }

        await using var stream = File.OpenRead(settings.ManifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<VaultManifestModel>(stream, cancellationToken: ct);
        return manifest ?? new VaultManifestModel();
    }

    public async Task SaveAsync(VaultManifestModel manifest, CancellationToken ct = default)
    {
        Directory.CreateDirectory(settings.DataPath);

        await using var stream = File.Create(settings.ManifestPath);
        await JsonSerializer.SerializeAsync(stream, manifest, new JsonSerializerOptions { WriteIndented = true }, ct);
    }

    public Dictionary<string, DateTime> ScanVault(string vaultRootPath)
    {
        var result = new Dictionary<string, DateTime>();

        foreach (var fullPath in Directory.EnumerateFiles(vaultRootPath, "*.md", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(vaultRootPath, fullPath).Replace('\\', '/');
            if (relativePath.StartsWith(IgnoredDirectoryName + "/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result[relativePath] = File.GetLastWriteTimeUtc(fullPath);
        }

        return result;
    }
}
