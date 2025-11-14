using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Quartz;
using Serilog;

namespace FolderCopyService;

public class Program
{
    public static void Main(string[] args)
    {
        // Configuração do Serilog
        string logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDir);

        string logFilePath = Path.Combine(logsDir, "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,      // 1 arquivo por dia
                retainedFileCountLimit: 30,                 // mantém só 30 dias
                shared: true,
                outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Iniciando FolderCopyService...");

            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "FolderCopyService";
                })
                .UseSerilog() // <- integra Serilog com o pipeline de logging
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;

                    // Lê BackupOptions do appsettings.json
                    services.Configure<BackupOptions>(
                        configuration.GetSection("BackupOptions"));

                    // Pega uma instância pra ler o intervalo na config do Quartz
                    var backupOptions = configuration
                        .GetSection("BackupOptions")
                        .Get<BackupOptions>() ?? new BackupOptions();

                    int intervalMinutes = backupOptions.IntervalMinutes > 0
                        ? backupOptions.IntervalMinutes
                        : 20;

                    // Serviço de cópia
                    services.AddSingleton<IFolderSyncService, FolderSyncService>();

                    // ---------- QUARTZ ----------
                    services.AddQuartz(q =>
                    {
                        q.UseMicrosoftDependencyInjectionJobFactory();

                        var jobKey = new JobKey("FolderCopyJob");

                        q.AddJob<FolderCopyJob>(opts => opts
                            .WithIdentity(jobKey));

                        q.AddTrigger(opts => opts
                            .ForJob(jobKey)
                            .WithIdentity("FolderCopyJob-trigger")
                            .WithSimpleSchedule(x => x
                                .WithInterval(TimeSpan.FromMinutes(intervalMinutes))
                                .RepeatForever()));
                    });

                    services.AddQuartzHostedService(options =>
                    {
                        options.WaitForJobsToComplete = true;
                    });
                    // -----------------------------
                })
                .Build()
                .Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "FolderCopyService finalizado devido a erro inesperado.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
