using Microsoft.Extensions.Logging;
using Quartz;

namespace FolderCopyService;

public class FolderCopyJob : IJob
{
    private readonly ILogger<FolderCopyJob> _logger;
    private readonly IFolderSyncService _folderSyncService;

    public FolderCopyJob(
        ILogger<FolderCopyJob> logger,
        IFolderSyncService folderSyncService)
    {
        _logger = logger;
        _folderSyncService = folderSyncService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation("Quartz: job de cópia disparado em {Time}", DateTimeOffset.Now);

        try
        {
            await _folderSyncService.SyncAsync(cancellationToken);

            _logger.LogInformation("Quartz: job de cópia finalizado com sucesso em {Time}", DateTimeOffset.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quartz: erro ao executar cópia de pastas.");
            throw;
        }
    }
}
