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

        int copiedFiles = 0;
        int skippedFiles = 0;

        CopyAll(source, target, cancellationToken, ref copiedFiles, ref skippedFiles);

        _logger.LogInformation(
            "Sincronização concluída. Arquivos copiados: {Copied}, ignorados (iguais): {Skipped}",
            copiedFiles,
            skippedFiles);

        return Task.CompletedTask;
    }

    private void CopyAll(
        DirectoryInfo source,
        DirectoryInfo target,
        CancellationToken cancellationToken,
        ref int copiedFiles,
        ref int skippedFiles)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Cria subpastas
        foreach (var dir in source.GetDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetSubDir = target.CreateSubdirectory(dir.Name);
            CopyAll(dir, targetSubDir, cancellationToken, ref copiedFiles, ref skippedFiles);
        }

        // Copia arquivos (somente se forem diferentes)
        foreach (var file in source.GetFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string destFilePath = Path.Combine(target.FullName, file.Name);

            bool shouldCopy = true;

            if (File.Exists(destFilePath))
            {
                var destInfo = new FileInfo(destFilePath);

                // Compara tamanho e data de modificação
                if (destInfo.Length == file.Length &&
                    destInfo.LastWriteTimeUtc == file.LastWriteTimeUtc)
                {
                    // É igual, não precisa copiar
                    shouldCopy = false;
                }
            }

            if (shouldCopy)
            {
                file.CopyTo(destFilePath, overwrite: true);
                copiedFiles++;
                _logger.LogDebug("Arquivo copiado: {File}", destFilePath);
            }
            else
            {
                skippedFiles++;
                _logger.LogDebug("Arquivo ignorado (sem alterações): {File}", destFilePath);
            }
        }
    }
}
