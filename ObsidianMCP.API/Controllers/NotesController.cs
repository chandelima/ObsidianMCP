using Microsoft.AspNetCore.Mvc;
using ObsidianMCP.Application.DTOs.Notes;
using ObsidianMCP.Application.Interfaces.Services;

namespace ObsidianMCP.API.Controllers;

/// <summary>
/// Search, read, and reindex notes from the indexed Obsidian/LeafWiki vault.
/// </summary>
[ApiController]
[Route("api/notes")]
public class NotesController(IObsidianIndexService indexService) : ControllerBase
{
    /// <summary>
    /// Returns the full content of a single note.
    /// </summary>
    /// <remarks>
    /// <paramref name="path"/> must be the exact value returned in the <c>path</c> field of a
    /// <c>GET /api/notes/search</c> result. The content reflects the vault as of the last
    /// successful reindex (<c>POST /api/notes/index</c>) — if the note changed on disk after
    /// that, reindex before calling this again to get the up-to-date content.
    /// </remarks>
    /// <param name="path">Relative path of the note inside the vault, e.g. "projects/example.md".</param>
    [HttpGet]
    [ProducesResponseType(typeof(NoteContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] string path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("O parâmetro 'path' é obrigatório.");
        }

        var note = await indexService.GetNoteAsync(path, ct);
        if (note is null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    /// <summary>
    /// Searches the vault by free-text terms.
    /// </summary>
    /// <remarks>
    /// Full-text search over each note's title and content (title matches are weighted higher).
    /// Results reflect the vault as of the last successful reindex — call
    /// <c>POST /api/notes/index</c> first if the vault may have changed recently. Use the
    /// <c>path</c> of a result with <c>GET /api/notes</c> to fetch that note's full content.
    /// </remarks>
    /// <param name="q">Free-text search terms.</param>
    /// <param name="max">Maximum number of results to return. Default 20, capped at 100.</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int max = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("O parâmetro 'q' é obrigatório.");
        }

        var result = await indexService.SearchAsync(q, max, ct);
        return Ok(result);
    }

    /// <summary>
    /// Triggers an incremental reindex of the vault.
    /// </summary>
    /// <remarks>
    /// Adds notes that are new, updates ones that changed, and removes ones no longer present on
    /// disk. Safe to call anytime — only files that changed since the last run are reprocessed
    /// (tracked via a manifest). The vault also reindexes itself automatically on a fixed
    /// interval in the background, so calling this manually is mainly useful to force an
    /// immediate refresh right before searching.
    /// </remarks>
    [HttpPost("index")]
    [ProducesResponseType(typeof(ReindexResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reindex(CancellationToken ct = default)
    {
        var result = await indexService.ReindexAsync(ct);
        if (result is null)
        {
            return Conflict("Já existe uma reindexação em andamento.");
        }

        return Ok(result);
    }
}
