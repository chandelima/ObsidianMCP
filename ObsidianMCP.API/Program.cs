using ObsidianMCP.API;
using ObsidianMCP.Infrastructure;
using ObsidianMCP.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

var obsidianSettings = ObsidianSettings.FromConfiguration(builder.Configuration, builder.Environment);

builder.Services.AddSingleton(obsidianSettings);
builder.Services.AddObsidianMcpApi();
builder.Services.AddObsidianMcpInfrastructure(obsidianSettings);
builder.Services.AddObsidianMcpServer();

var app = builder.Build();

app.ApplyObsidianMcpSettings();
app.MapObsidianMcpServer();

app.Run();
