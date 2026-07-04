using Microsoft.AspNetCore.Mvc;
using ObsidianMCP.Application.Interfaces.Services;

namespace ObsidianMCP.API.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController(IObsidianIndexService indexService) : ControllerBase
{
    [HttpGet]
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

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int max = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("O parâmetro 'q' é obrigatório.");
        }

        var result = await indexService.SearchAsync(q, max, ct);
        return Ok(result);
    }

    [HttpPost("index")]
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
