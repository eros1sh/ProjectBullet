using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectBullet.Core;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Logging;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.Views.Pages.Shared;
using RuriLib.Logging;
using RuriLib.Providers.RandomNumbers;
using RuriLib.Providers.UserAgents;
using RuriLib.Services;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ProjectBullet.Core.Models.Proxies;
using ProjectBullet.Telegram.Services;

namespace ProjectBullet.Avalonia
{
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;
        private IConfiguration config;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            TaskScheduler.UnobservedTaskException += OnTaskException;

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(exeDir);
            Directory.CreateDirectory("UserData");

            var builder = new ConfigurationBuilder()
                .SetBasePath(exeDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IConfiguration>(_ => builder.Build());
            config = builder.Build();
            ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();
            SP.Init(serviceProvider);

            var workerThreads = config.GetSection("Resources").GetValue("WorkerThreads", 1000);
            var ioThreads = config.GetSection("Resources").GetValue("IOThreads", 1000);
            var connectionLimit = config.GetSection("Resources").GetValue("ConnectionLimit", 1000);

            ThreadPool.SetMinThreads(workerThreads, ioThreads);
#pragma warning disable SYSLIB0014
            ServicePointManager.DefaultConnectionLimit = connectionLimit;
#pragma warning restore SYSLIB0014

            // Apply DB migrations
            using (var serviceScope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();
            }

            // Load the configs
            var configService = serviceProvider.GetService<ConfigService>();
            configService.ReloadConfigsAsync().Wait();

            AutocompletionProvider.Init();

            // Start the job monitor
            _ = serviceProvider.GetService<JobMonitorService>();

            // Start Telegram bot if enabled
            _ = serviceProvider.GetService<TelegramBotService>();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var settings = serviceProvider.GetService<ProjectBulletSettingsService>().Settings;

                // App Lock check
                if (settings.AppLockSettings.Enabled && !string.IsNullOrEmpty(settings.AppLockSettings.PasswordHash))
                {
                    var lockDialog = new MainDialog(
                        new Views.Dialogs.AppLockDialog(settings.AppLockSettings),
                        "ProjectBullet - Locked");

                    lockDialog.ShowDialog(null);
                    // If the dialog was cancelled (not confirmed), exit
                    // The AppLockDialog sets DialogResult to true on success
                }

                var mainWindow = serviceProvider.GetService<MainWindow>();
                mainWindow.NavigateTo(MainWindowPage.Home);
                desktop.MainWindow = mainWindow;

                // Initialize marketplace auth in background, then start webhook polling + heartbeat
                Task.Run(async () =>
                {
                    var marketplace = serviceProvider.GetService<MarketplaceApiService>();
                    await marketplace.InitAsync();
                    serviceProvider.GetService<WebhookPollingService>().Restart();

                    // Start periodic heartbeat (every 2 minutes)
                    var version = serviceProvider.GetService<UpdateService>().CurrentVersion?.ToString() ?? "unknown";
                    marketplace.StartHeartbeat(version);
                });
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Windows and pages
            services.AddSingleton<MainWindow>();
            services.AddSingleton<Debugger>();

            // EF
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(config.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("ProjectBullet.Core")), ServiceLifetime.Transient);

            // Repositories
            services.AddSingleton<IProxyRepository, DbProxyRepository>();
            services.AddSingleton<IProxyGroupRepository, DbProxyGroupRepository>();
            services.AddSingleton<IHitRepository, DbHitRepository>();
            services.AddSingleton<IJobRepository, DbJobRepository>();
            services.AddSingleton<IRecordRepository, DbRecordRepository>();
            services.AddSingleton<IConfigRepository>(service =>
                new DiskConfigRepository(service.GetService<RuriLibSettingsService>(),
                "UserData/Configs"));
            services.AddSingleton<IWordlistRepository>(service =>
                new HybridWordlistRepository(service.GetService<ApplicationDbContext>(),
                "UserData/Wordlists"));

            // Transient services
            services.AddTransient<OB2MigrationService>();

            // Singletons
            services.AddSingleton<VolatileSettingsService>();
            services.AddSingleton<ViewModelsService>();
            services.AddSingleton<AnnouncementService>();
            services.AddSingleton<UpdateService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<ProxyReloadService>();
            services.AddSingleton<ProxyCheckOutputFactory>();
            services.AddSingleton<JobFactoryService>();
            services.AddSingleton<JobManagerService>();
            services.AddSingleton<JobMonitorService>();
            services.AddSingleton<HitStorageService>();
            services.AddSingleton<DataPoolFactoryService>();
            services.AddSingleton<ProxySourceFactoryService>();
            services.AddSingleton(_ => new RuriLibSettingsService("UserData"));
            services.AddSingleton(_ => new ProjectBulletSettingsService("UserData"));
            services.AddSingleton(_ => new PluginRepository("UserData/Plugins"));
            services.AddSingleton<IRandomUAProvider>(_ => new IntoliRandomUAProvider("user-agents.json"));
            services.AddSingleton<IRNGProvider, DefaultRNGProvider>();
            services.AddSingleton<MemoryJobLogger>();
            services.AddSingleton<IJobLogger>(service =>
                new FileJobLogger(service.GetService<RuriLibSettingsService>(),
                "UserData/Logs/Jobs"));

            // Telegram Bot
            services.AddSingleton<TelegramBotService>();

            // Webhook Polling
            services.AddSingleton<WebhookPollingService>();

            // Marketplace API
            services.AddSingleton<MarketplaceApiService>();
        }

        private void OnTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
        }

        public static void ReportCrash(Exception ex)
        {
            File.WriteAllText("crash.log", $"Unhandled exception thrown on {DateTime.Now}\r\n{ex}");

            Dispatcher.UIThread.Post(() =>
            {
                Alert.Error("Unhandled exception", $"An unhandled exception was thrown, the application will try to continue running." +
                    $" Please open the crash.log file, copy the error message inside it and open an issue on the official github repository." +
                    $" A few details about the exception: {ex.Message}");
            });
        }
    }
}
