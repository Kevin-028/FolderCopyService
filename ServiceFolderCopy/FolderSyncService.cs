using Microsoft.Extensions.Options;

namespace FolderCopyService;

public interface IFolderSyncService
{
    Task SyncAsync(CancellationToken cancellationToken);
}
public class FolderSyncService : IFolderSyncService
{
    private readonly BackupOptions _options;
    private readonly ILogger<FolderSyncService> _logger;

    public FolderSyncService(
        IOptions<BackupOptions> options,
        ILogger<FolderSyncService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SyncAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Iniciando sincronização. SourcePath='{Source}', TargetPath='{Target}'",
            _options.SourcePath,
            _options.TargetPath);

        if (!Directory.Exists(_options.SourcePath))
        {
            var ex = new DirectoryNotFoundException(
                $"Pasta de origem não encontrada: {_options.SourcePath}");

            _logger.LogError(ex,
                "Falha na sincronização: pasta de origem '{Source}' não encontrada.",
                _options.SourcePath);

            throw ex;
        }

        Directory.CreateDirectory(_options.TargetPath);

        var source = new DirectoryInfo(_options.SourcePath);
        var target = new DirectoryInfo(_options.TargetPath);

        CopyAll(source, target, cancellationToken);

        _logger.LogInformation("Sincronização concluída com sucesso.");

        return Task.CompletedTask;
    }

    private void CopyAll(DirectoryInfo source, DirectoryInfo target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Cria subpastas
        foreach (var dir in source.GetDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetSubDir = target.CreateSubdirectory(dir.Name);
            CopyAll(dir, targetSubDir, cancellationToken);
        }

        // Copia arquivos
        foreach (var file in source.GetFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string destFile = Path.Combine(target.FullName, file.Name);
            file.CopyTo(destFile, overwrite: true);
        }
    }
}