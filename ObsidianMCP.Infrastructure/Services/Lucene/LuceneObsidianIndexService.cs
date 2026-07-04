using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using ObsidianMCP.Application.DTOs.Notes;
using ObsidianMCP.Application.Interfaces.Services;
using ObsidianMCP.Domain.Models;
using ObsidianMCP.Domain.Services;
using ObsidianMCP.Infrastructure.Settings;
using System.Diagnostics;
using System.Globalization;

namespace ObsidianMCP.Infrastructure.Services.Lucene;

internal sealed class LuceneObsidianIndexService : IObsidianIndexService, IDisposable
{
    private const LuceneVersion Version = LuceneVersion.LUCENE_48;
    private const int MaxAllowedResults = 100;

    private const string FieldPath = "path";
    private const string FieldTitle = "title";
    private const string FieldContent = "content";
    private const string FieldLastModifiedUtc = "lastModifiedUtc";

    private readonly ObsidianSettings _settings;
    private readonly IFileManifestService _manifestService;
    private readonly ILogger<LuceneObsidianIndexService> _logger;

    private readonly FSDirectory _directory;
    private readonly StandardAnalyzer _analyzer;
    private readonly IndexWriter _writer;
    private readonly SearcherManager _searcherManager;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public LuceneObsidianIndexService(
        ObsidianSettings settings,
        IFileManifestService manifestService,
        ILogger<LuceneObsidianIndexService> logger)
    {
        _settings = settings;
        _manifestService = manifestService;
        _logger = logger;

        _directory = FSDirectory.Open(_settings.IndexPath);
        _analyzer = new StandardAnalyzer(Version);

        var writerConfig = new IndexWriterConfig(Version, _analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND
        };
        _writer = new IndexWriter(_directory, writerConfig);
        _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, searcherFactory: null);
    }

    public Task<SearchResponseDto> SearchAsync(string queryText, int maxResults = 20, CancellationToken ct = default)
    {
        maxResults = Math.Clamp(maxResults, 1, MaxAllowedResults);

        var parser = new MultiFieldQueryParser(
            Version,
            [FieldTitle, FieldContent],
            _analyzer,
            new Dictionary<string, float> { [FieldTitle] = 2.0f, [FieldContent] = 1.0f });

        var query = parser.Parse(QueryParserBase.Escape(queryText));

        var searcher = _searcherManager.Acquire();
        try
        {
            var topDocs = searcher.Search(query, maxResults);

            var highlighter = new Highlighter(new SimpleHTMLFormatter("<mark>", "</mark>"), new QueryScorer(query))
            {
                TextFragmenter = new SimpleFragmenter(150)
            };

            var results = new List<SearchResultDto>(topDocs.ScoreDocs.Length);
            foreach (var scoreDoc in topDocs.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);
                var content = doc.Get(FieldContent) ?? string.Empty;

                using var tokenStream = _analyzer.GetTokenStream(FieldContent, content);
                var snippet = highlighter.GetBestFragment(tokenStream, content);

                results.Add(new SearchResultDto
                {
                    Path = doc.Get(FieldPath) ?? string.Empty,
                    Title = doc.Get(FieldTitle) ?? string.Empty,
                    Score = scoreDoc.Score,
                    LastModifiedUtc = ParseLastModified(doc.Get(FieldLastModifiedUtc)),
                    Snippet = snippet
                });
            }

            return Task.FromResult(new SearchResponseDto
            {
                Query = queryText,
                TotalHits = topDocs.TotalHits,
                Results = results
            });
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    public Task<NoteContentDto?> GetNoteAsync(string relativePath, CancellationToken ct = default)
    {
        var searcher = _searcherManager.Acquire();
        try
        {
            var topDocs = searcher.Search(new TermQuery(new Term(FieldPath, relativePath)), 1);
            if (topDocs.ScoreDocs.Length == 0)
            {
                return Task.FromResult<NoteContentDto?>(null);
            }

            var doc = searcher.Doc(topDocs.ScoreDocs[0].Doc);
            return Task.FromResult<NoteContentDto?>(new NoteContentDto
            {
                Path = doc.Get(FieldPath) ?? relativePath,
                Title = doc.Get(FieldTitle) ?? string.Empty,
                Content = doc.Get(FieldContent) ?? string.Empty
            });
        }
        finally
        {
            _searcherManager.Release(searcher);
        }
    }

    public async Task<ReindexResultDto?> ReindexAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!await _writeLock.WaitAsync(0, ct))
        {
            return null;
        }

        try
        {
            var previousManifest = await _manifestService.LoadAsync(ct);
            var currentFiles = _manifestService.ScanVault(_settings.VaultPath);

            var addedPaths = new List<string>();
            var updatedPaths = new List<string>();
            var removedPaths = new List<string>();

            foreach (var (relativePath, lastModifiedUtc) in currentFiles)
            {
                var isNew = !previousManifest.Files.TryGetValue(relativePath, out var previousModifiedUtc);
                var isChanged = !isNew && previousModifiedUtc != lastModifiedUtc;

                if (!isNew && !isChanged)
                {
                    continue;
                }

                var note = LoadNote(relativePath, lastModifiedUtc);
                _writer.UpdateDocument(new Term(FieldPath, relativePath), ToLuceneDocument(note));

                if (isNew)
                {
                    addedPaths.Add(relativePath);
                }
                else
                {
                    updatedPaths.Add(relativePath);
                }
            }

            foreach (var relativePath in previousManifest.Files.Keys)
            {
                if (currentFiles.ContainsKey(relativePath))
                {
                    continue;
                }

                _writer.DeleteDocuments(new Term(FieldPath, relativePath));
                removedPaths.Add(relativePath);
            }

            _writer.Commit();
            _searcherManager.MaybeRefreshBlocking();

            await _manifestService.SaveAsync(new VaultManifestModel
            {
                GeneratedAtUtc = DateTime.UtcNow,
                Files = currentFiles
            }, ct);

            using var reader = DirectoryReader.Open(_writer, applyAllDeletes: true);

            stopwatch.Stop();

            return new ReindexResultDto
            {
                Added = addedPaths.Count,
                Updated = updatedPaths.Count,
                Removed = removedPaths.Count,
                AddedPaths = addedPaths,
                UpdatedPaths = updatedPaths,
                RemovedPaths = removedPaths,
                TotalDocuments = reader.NumDocs,
                DurationMs = stopwatch.ElapsedMilliseconds,
                CompletedAtUtc = DateTime.UtcNow
            };
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private NoteDocumentModel LoadNote(string relativePath, DateTime lastModifiedUtc)
    {
        var fullPath = Path.Combine(_settings.VaultPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var rawText = File.ReadAllText(fullPath);

        var (frontmatterTitle, body) = MarkdownNoteParserService.StripFrontmatter(rawText);
        var h1Title = MarkdownNoteParserService.ExtractFirstH1(body);
        var title = MarkdownNoteParserService.ResolveTitle(frontmatterTitle, h1Title, Path.GetFileNameWithoutExtension(relativePath));

        return new NoteDocumentModel
        {
            Path = relativePath,
            Title = title,
            Content = body,
            LastModifiedUtc = lastModifiedUtc
        };
    }

    private static Document ToLuceneDocument(NoteDocumentModel note) => new()
    {
        new StringField(FieldPath, note.Path, Field.Store.YES),
        new TextField(FieldTitle, note.Title, Field.Store.YES),
        new TextField(FieldContent, note.Content, Field.Store.YES),
        new StringField(FieldLastModifiedUtc, note.LastModifiedUtc.ToString("o"), Field.Store.YES)
    };

    private static DateTime ParseLastModified(string? value) =>
        string.IsNullOrEmpty(value)
            ? default
            : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public void Dispose()
    {
        try
        {
            _writer.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to commit Lucene index writer during shutdown.");
        }

        _searcherManager.Dispose();
        _writer.Dispose();
        _analyzer.Dispose();
        _directory.Dispose();
        _writeLock.Dispose();
    }
}
