namespace FolderCopyService;

public class BackupOptions
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;

    // Intervalo em minutos (lido do appsettings.json)
    public int IntervalMinutes { get; set; } = 20;

    public TimeSpan Interval => TimeSpan.FromMinutes(IntervalMinutes);
}
