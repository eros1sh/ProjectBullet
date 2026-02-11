using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectBullet.Core;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Logging;
using ProjectBullet.Native.Helpers;
using ProjectBullet.Native.Services;
using ProjectBullet.Native.Views.Pages.Shared;
using RuriLib.Logging;
using RuriLib.Providers.RandomNumbers;
using RuriLib.Providers.UserAgents;
using RuriLib.Services;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ProjectBullet.Core.Models.Proxies;
using ProjectBullet.Telegram.Services;

namespace ProjectBullet.Native
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IConfiguration config;

        public App()
        {
            Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskException;

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(exeDir);
            Directory.CreateDirectory("UserData");

            var builder = new ConfigurationBuilder()
                .SetBasePath(exeDir)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IConfiguration>(_ => builder.Build());
            ConfigureServices(serviceCollection);
            serviceProvider = serviceCollection.BuildServiceProvider();
            SP.Init(serviceProvider);

            config = SP.GetService<IConfiguration>();
            var workerThreads = config.GetSection("Resources").GetValue("WorkerThreads", 1000);
            var ioThreads = config.GetSection("Resources").GetValue("IOThreads", 1000);
            var connectionLimit = config.GetSection("Resources").GetValue("ConnectionLimit", 1000);

            ThreadPool.SetMinThreads(workerThreads, ioThreads);
#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete but still needed for legacy HTTP connections
            ServicePointManager.DefaultConnectionLimit = connectionLimit;
#pragma warning restore SYSLIB0014

            // Apply DB migrations or create a DB if it doesn't exist
            using (var serviceScope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate();
            }

            // Load the configs
            var configService = serviceProvider.GetService<ConfigService>();
            configService.ReloadConfigsAsync().Wait();

            AutocompletionProvider.Init();

            // Start the job monitor at the start of the application,
            // otherwise it will only be started when navigating to the page
            _ = serviceProvider.GetService<JobMonitorService>();

            // Start Telegram bot if enabled
            _ = serviceProvider.GetService<TelegramBotService>();
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

            // Webhook Polling (must be after TelegramBotService)
            services.AddSingleton<WebhookPollingService>();

            // Marketplace API
            services.AddSingleton<MarketplaceApiService>();
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var settings = serviceProvider.GetService<ProjectBulletSettingsService>().Settings;

            // App Lock check
            if (settings.AppLockSettings.Enabled && !string.IsNullOrEmpty(settings.AppLockSettings.PasswordHash))
            {
                var lockDialog = new MainDialog(
                    new Views.Dialogs.AppLockDialog(settings.AppLockSettings),
                    "ProjectBullet - Locked");

                if (lockDialog.ShowDialog() != true)
                {
                    Current.Shutdown();
                    return;
                }
            }

            var mainWindow = serviceProvider.GetService<MainWindow>();
            mainWindow.NavigateTo(MainWindowPage.Home);
            mainWindow.Show();

            // Initialize marketplace auth in background, then start webhook polling
            Task.Run(async () =>
            {
                await serviceProvider.GetService<MarketplaceApiService>().InitAsync();
                serviceProvider.GetService<WebhookPollingService>().Restart();
            });
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ReportCrash(e.Exception);
            e.Handled = true; // Set to false to close the app on exception
        }

        private void OnTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved(); // Comment this line to close the app on task exception

            // I decided to disable the code below since usually task exceptions are not critical to the application

            /*
            if (e.Exception.InnerException is not null)
            {
                if (e.Exception.InnerException is PuppeteerSharp.PuppeteerException // https://github.com/hardkoded/puppeteer-sharp/issues/891
                    or System.Net.Sockets.SocketException // Seems like all networking-related things can cause unhandled task exceptions
                    or TimeoutException) // This is again thrown by Puppeteer
                {
                    return;
                }
            }

            ReportCrash(e.Exception);
            */
        }

        private static void ReportCrash(Exception ex)
        {
            File.WriteAllText("crash.log", $"Unhandled exception thrown on {DateTime.Now}\r\n{ex}");

            Alert.Error("Unhandled exception", $"An unhandled exception was thrown, the application will try to continue running." +
                $" Please open the crash.log file, copy the error message inside it and open an issue on the official github repository." +
                $" A few details about the exception: {ex.Message}");
        }
    }
}
