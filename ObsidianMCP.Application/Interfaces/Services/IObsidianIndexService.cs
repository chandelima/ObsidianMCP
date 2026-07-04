using ObsidianMCP.Application.DTOs.Notes;

namespace ObsidianMCP.Application.Interfaces.Services;

public interface IObsidianIndexService
{
    Task<SearchResponseDto> SearchAsync(string queryText, int maxResults = 20, CancellationToken ct = default);

    /// <summary>Retorna null se já houver uma reindexação em andamento (não espera a vez).</summary>
    Task<ReindexResultDto?> ReindexAsync(CancellationToken ct = default);

    Task<NoteContentDto?> GetNoteAsync(string relativePath, CancellationToken ct = default);
}
