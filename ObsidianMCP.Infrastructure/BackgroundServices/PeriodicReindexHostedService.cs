using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObsidianMCP.Application.Interfaces.Services;
using ObsidianMCP.Infrastructure.Settings;

namespace ObsidianMCP.Infrastructure.BackgroundServices;

internal sealed class PeriodicReindexHostedService(
    IObsidianIndexService indexService,
    ObsidianSettings settings,
    ILogger<PeriodicReindexHostedService> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(settings.ReindexIntervalMinutes ?? 5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        do
        {
            try
            {
                var result = await indexService.ReindexAsync(stoppingToken);
                if (result is null)
                {
                    logger.LogInformation("Reindexação automática ignorada: já havia uma reindexação em andamento.");
                }
                else
                {
                    logger.LogInformation(
                        "Reindexação automática concluída: {Added} adicionados, {Updated} atualizados, {Removed} removidos ({DurationMs}ms).",
                        result.Added, result.Updated, result.Removed, result.DurationMs);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao executar a reindexação automática.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
