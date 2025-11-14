using Microsoft.Extensions.Options;

namespace FolderCopyService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IFolderSyncService _folderSyncService;
    private readonly BackupOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IFolderSyncService folderSyncService,
        IOptions<BackupOptions> options)
    {
        _logger = logger;
        _folderSyncService = folderSyncService;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Serviço de cópia iniciado em: {time}", DateTimeOffset.Now);

        await RunCopyAsync(stoppingToken);

        using var timer = new PeriodicTimer(_options.Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCopyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // cancelamento normal ao parar o serviço
        }

        _logger.LogInformation("Serviço de cópia finalizado em: {time}", DateTimeOffset.Now);
    }

    private async Task RunCopyAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando cópia de {source} para {target} em {time}",
                _options.SourcePath,
                _options.TargetPath,
                DateTimeOffset.Now);

            await _folderSyncService.SyncAsync(stoppingToken);

            _logger.LogInformation("Cópia concluída com sucesso em {time}", DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar cópia de pastas");
        }
    }
}
