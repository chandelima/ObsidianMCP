# ObsidianMCP

Personal project I built to solve a need of my own: let an AI agent search and read the notes
in my vault (exported from a tool called LeafWiki, in Markdown format, similar to Obsidian)
without me having to copy and paste content manually. It's not a product — it's a personal-use
tool, with no authentication, meant to run on a local network.

## What it does

- Indexes a vault of Markdown notes using **Lucene.NET**.
- Exposes the same functionality in two ways:
  - **REST API** (for testing/debugging via `curl`/Swagger).
  - **MCP server** (for an agent to use directly as tools).
- **Incremental** reindexing: keeps a manifest (`manifest.json`) with each file's timestamp, so
  it only reprocesses what changed (added, modified, or removed) — and reindexes itself every
  N minutes in the background, on top of being triggerable manually.
- Resolves each note's title in this priority order: `leafwiki_title` field in the YAML
  frontmatter → first `# H1` heading in the markdown → file name. The frontmatter is always
  stripped from the indexed/returned content.

## Architecture

.NET 10 solution organized into 4 projects (based on Clean Architecture):

| Project | Responsibility |
|---|---|
| `ObsidianMCP.Domain` | Pure models and business rules (note title, manifest), no external dependencies. |
| `ObsidianMCP.Application` | Contracts (`IObsidianIndexService`, `IFileManifestService`) and DTOs — the "port" that Infrastructure implements and API consumes. |
| `ObsidianMCP.Infrastructure` | Concrete implementation: Lucene.NET, file system access, configuration, periodic reindex job. |
| `ObsidianMCP.API` | Composition (DI, `Program.cs`), REST endpoints, and MCP tools. |

## REST Endpoints

| Method | Route | What it does |
|---|---|---|
| `GET` | `/api/notes?path={path}` | Returns the full content of a note (frontmatter stripped). |
| `GET` | `/api/notes/search?q={terms}&max={n}` | Full-text search over the vault, with a highlighted snippet. |
| `POST` | `/api/notes/index` | Triggers an incremental reindex. Returns `409` if one is already running. |

In the `Development` environment, the Swagger UI is available at the host's root.

## MCP Server

The same `IObsidianIndexService` is exposed as MCP tools over HTTP (official
`ModelContextProtocol.AspNetCore` SDK), on the same host as the REST API, at the `/mcp` route:

| Tool | REST equivalent |
|---|---|
| `search_notes` | `GET /api/notes/search` |
| `get_note_content` | `GET /api/notes` |
| `reindex_notes` | `POST /api/notes/index` |

To test it quickly from the browser without setting up a real MCP client, you can use the
[MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```bash
npx @modelcontextprotocol/inspector
```

Connect using the "Streamable HTTP" transport, pointing at `http://localhost:5115/mcp` (or
whichever port is configured). CORS is wide open (`AllowAnyOrigin`) — acceptable here because
this is a local tool with no authentication; it's not a configuration meant for public exposure.

## Configuration

Configuration lives under the `Obsidian` section of `appsettings.json`:

```json
{
  "Obsidian": {
    "VaultPath": "",
    "DataPath": "App_Data",
    "ReindexIntervalMinutes": null
  }
}
```

| Key | Required | Description |
|---|---|---|
| `VaultPath` | Yes | Root folder of the vault containing the `.md` files. The application fails to start if the path doesn't exist. |
| `DataPath` | Yes | Folder where the application keeps its own state (Lucene index and `manifest.json`). Created automatically if it doesn't exist. |
| `ReindexIntervalMinutes` | No | Interval, in minutes, between automatic reindexes. If omitted/`null`, defaults to 5. |

Relative paths in `VaultPath`/`DataPath` are resolved against the application's root directory
(`ContentRootPath`), not the directory the command is run from.

### Overriding with environment variables (Linux)

ASP.NET Core reads environment variables as another configuration source, and they take
priority over `appsettings.json`. For a nested key like `Obsidian:VaultPath`, the environment
variable name replaces `:` with `__` (double underscore) — that's the configuration provider's
convention, not something specific to this project:

```bash
export Obsidian__VaultPath="/home/user/ObsidianVault"
export Obsidian__DataPath="/home/user/.local/share/obsidian-mcp"
export Obsidian__ReindexIntervalMinutes=10

dotnet run --project ObsidianMCP.API
```

Or all on one line, without exporting for the whole session:

```bash
Obsidian__VaultPath=/home/user/ObsidianVault \
Obsidian__DataPath=/home/user/.local/share/obsidian-mcp \
dotnet run --project ObsidianMCP.API
```

You can also control the ASP.NET Core port/environment the same way, for example:

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="http://0.0.0.0:5115"
```

(HTTPS redirection is only forced in the `Production` environment. `Development` and `Docker`
both leave it off, to simplify local testing with tools like the MCP Inspector.)

## Running locally

```bash
dotnet build
dotnet run --project ObsidianMCP.API
```

The API comes up on `http://localhost:5115` (the `http` profile in `launchSettings.json`) by
default in development.

## Running with Docker

The `Dockerfile` builds and publishes `ObsidianMCP.API`; `docker-compose.yml` runs it under a
dedicated `Docker` environment. `appsettings.Docker.json` already fixes the container-internal
paths (`VaultPath=/vault`, `DataPath=/data`), so the only things left to configure at deploy
time are the **host** paths for the two volumes:

| Environment variable | What it is |
|---|---|
| `OBSIDIAN_VAULT_HOST_PATH` | Host path to your vault. Mounted read-only at `/vault`. |
| `OBSIDIAN_DATA_HOST_PATH` | Host path where the Lucene index and `manifest.json` are persisted. Mounted at `/data`. |
| `OBSIDIAN_HOST_PORT` | Host port to publish, mapped to the container's internal port `8080`. |

Neither `volumes:` nor `ports:` are hardcoded with fixed values in `docker-compose.yml` on
purpose — this was built to be deployed as a Portainer stack straight from the GitHub repo, and
Portainer's stack UI for that flow only lets you set environment variables, not volumes or port
mappings directly.

```bash
OBSIDIAN_VAULT_HOST_PATH=/home/user/ObsidianVault \
OBSIDIAN_DATA_HOST_PATH=/home/user/.local/share/obsidian-mcp \
OBSIDIAN_HOST_PORT=5115 \
docker compose up --build
```

In Portainer, set `OBSIDIAN_VAULT_HOST_PATH`, `OBSIDIAN_DATA_HOST_PATH`, and
`OBSIDIAN_HOST_PORT` in the stack's "Environment variables" section when deploying from the
repository — Portainer feeds them to `docker compose` the same way a `.env` file would.
