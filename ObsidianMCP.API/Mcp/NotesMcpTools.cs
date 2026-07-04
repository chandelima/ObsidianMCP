using System.ComponentModel;
using ModelContextProtocol.Server;
using ObsidianMCP.Application.DTOs.Notes;
using ObsidianMCP.Application.Interfaces.Services;

namespace ObsidianMCP.API.Mcp;

[McpServerToolType]
public class NotesMcpTools(IObsidianIndexService indexService)
{
    [McpServerTool(Name = "search_notes")]
    [Description("Searches the Obsidian vault notes by free-text terms. Returns the most relevant results, each with a snippet highlighting where the term was found.")]
    public async Task<SearchResponseDto> SearchNotes(
        [Description("Search terms (free text).")] string query,
        [Description("Maximum number of results. Default 20, capped at 100.")] int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        return await indexService.SearchAsync(query, maxResults, cancellationToken);
    }

    [McpServerTool(Name = "get_note_content")]
    [Description("Returns the full content of a note from the vault (frontmatter YAML stripped), given the path returned by 'search_notes'.")]
    public async Task<object> GetNoteContent(
        [Description("Relative path of the note inside the vault, e.g. 'projects/example.md'.")] string path,
        CancellationToken cancellationToken = default)
    {
        var note = await indexService.GetNoteAsync(path, cancellationToken);
        return note is null
            ? $"No note found for path '{path}'. You may need to run 'reindex_notes'."
            : note;
    }

    [McpServerTool(Name = "reindex_notes")]
    [Description("Incrementally reindexes the vault: adds new notes, updates changed ones, and removes notes no longer present on disk. Run this before searching if the vault changed recently.")]
    public async Task<object> ReindexNotes(CancellationToken cancellationToken = default)
    {
        var result = await indexService.ReindexAsync(cancellationToken);
        return result is null
            ? "A reindex is already in progress. Try again shortly."
            : result;
    }
}
