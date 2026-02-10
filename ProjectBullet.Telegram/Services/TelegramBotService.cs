using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Core.Services;
using RuriLib.Models.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ProjectBullet.Telegram.Services;

public class TelegramBotService : IDisposable
{
    private TelegramBotClient _bot;
    private CancellationTokenSource _cts;
    private readonly ProjectBulletSettingsService _settingsService;
    private readonly JobManagerService _jobManager;
    private readonly ConfigService _configService;
    private readonly JobFactoryService _jobFactory;

    private TelegramSettings Settings => _settingsService.Settings.TelegramSettings;

    public TelegramBotService(
        ProjectBulletSettingsService settingsService,
        JobManagerService jobManager,
        ConfigService configService,
        JobFactoryService jobFactory)
    {
        _settingsService = settingsService;
        _jobManager = jobManager;
        _configService = configService;
        _jobFactory = jobFactory;

        if (Settings.Enabled && !string.IsNullOrEmpty(Settings.BotToken))
        {
            Start();
        }
    }

    public void Start()
    {
        if (_bot != null) return;
        if (string.IsNullOrEmpty(Settings.BotToken)) return;

        _bot = new TelegramBotClient(Settings.BotToken);
        _cts = new CancellationTokenSource();

        // Only use long polling if webhook relay is NOT active
        if (!Settings.UseWebhookRelay)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );
        }

        // Subscribe to hit notifications on all existing jobs
        if (Settings.SendHitNotifications)
        {
            foreach (var job in _jobManager.Jobs.OfType<MultiRunJob>())
            {
                SubscribeToHits(job);
            }
        }
    }

    /// <summary>
    /// Process an update received via webhook relay polling.
    /// </summary>
    public async Task ProcessUpdateAsync(Update update)
    {
        if (_bot == null) return;
        await HandleUpdateAsync(_bot, update, _cts?.Token ?? default);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _bot = null;
    }

    public void Restart()
    {
        Stop();
        if (Settings.Enabled && !string.IsNullOrEmpty(Settings.BotToken))
        {
            Start();
        }
    }

    public void SubscribeToHits(MultiRunJob job)
    {
        if (_bot == null || !Settings.SendHitNotifications) return;

        job.OnHit += async (sender, hit) =>
        {
            try
            {
                if (_bot == null || Settings.ChatId == 0) return;
                var configName = job.Config?.Metadata?.Name ?? "Unknown";

                string msg;
                if (Settings.EnhancedHitFormat)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"[HIT] {configName}");
                    sb.AppendLine($"Data: {hit.Data}");
                    sb.AppendLine($"Type: {hit.Type}");
                    if (!string.IsNullOrEmpty(hit.CapturedDataString))
                        sb.AppendLine($"Capture: {hit.CapturedDataString}");
                    if (hit.Proxy != null)
                        sb.AppendLine($"Proxy: {hit.ProxyString}");
                    sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    msg = sb.ToString();
                }
                else
                {
                    msg = $"[HIT] {configName}\nData: {hit.Data}\nType: {hit.Type}";
                    if (!string.IsNullOrEmpty(hit.CapturedDataString))
                        msg += $"\nCapture: {hit.CapturedDataString}";
                }

                await _bot.SendMessage(Settings.ChatId, msg, cancellationToken: _cts?.Token ?? default);
            }
            catch { }
        };
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                await HandleMessage(update.Message, ct);
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                await HandleCallbackQuery(update.CallbackQuery, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Telegram error: {ex.Message}");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        Console.WriteLine($"Telegram bot error: {exception.Message}");
        return Task.CompletedTask;
    }

    private bool IsAuthorized(long userId)
    {
        if (Settings.WhitelistedUserIds.Count == 0) return true;
        return Settings.WhitelistedUserIds.Contains(userId);
    }

    private async Task HandleMessage(Message message, CancellationToken ct)
    {
        if (!IsAuthorized(message.From.Id))
        {
            await _bot.SendMessage(message.Chat.Id, "Unauthorized.", cancellationToken: ct);
            return;
        }

        var text = message.Text.Trim();

        if (text == "/start" || text == "/menu")
        {
            await SendMainMenu(message.Chat.Id, ct);
        }
        else if (text == "/status")
        {
            await SendStatus(message.Chat.Id, ct);
        }
        else if (text == "/stats")
        {
            await SendDetailedStats(message.Chat.Id, ct);
        }
        else if (text.StartsWith("/search "))
        {
            await SearchHits(message.Chat.Id, text[8..].Trim(), ct);
        }
        else if (text == "/configs")
        {
            await SendConfigList(message.Chat.Id, ct);
        }
        else if (text == "/proxies")
        {
            await SendProxyStats(message.Chat.Id, ct);
        }
        else if (text == "/help")
        {
            await SendHelp(message.Chat.Id, ct);
        }
    }

    private async Task HandleCallbackQuery(CallbackQuery query, CancellationToken ct)
    {
        if (!IsAuthorized(query.From.Id))
        {
            await _bot.AnswerCallbackQuery(query.Id, "Unauthorized.", cancellationToken: ct);
            return;
        }

        var chatId = query.Message.Chat.Id;
        var data = query.Data;

        await _bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);

        if (data == "menu")
        {
            await SendMainMenu(chatId, ct);
        }
        else if (data == "status")
        {
            await SendStatus(chatId, ct);
        }
        else if (data == "list_jobs")
        {
            await SendJobList(chatId, ct);
        }
        else if (data == "new_job")
        {
            await SendConfigSelection(chatId, ct);
        }
        else if (data.StartsWith("start_job_"))
        {
            var jobId = int.Parse(data.Replace("start_job_", ""));
            await StartJob(chatId, jobId, ct);
        }
        else if (data.StartsWith("stop_job_"))
        {
            var jobId = int.Parse(data.Replace("stop_job_", ""));
            await StopJob(chatId, jobId, ct);
        }
        else if (data.StartsWith("abort_job_"))
        {
            var jobId = int.Parse(data.Replace("abort_job_", ""));
            await AbortJob(chatId, jobId, ct);
        }
        else if (data.StartsWith("job_actions_"))
        {
            var jobId = int.Parse(data.Replace("job_actions_", ""));
            await SendJobActions(chatId, jobId, ct);
        }
    }

    private async Task SendMainMenu(long chatId, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("List Jobs", "list_jobs") },
            new[] { InlineKeyboardButton.WithCallbackData("Status", "status") },
        });

        await _bot.SendMessage(chatId, "ProjectBullet - Main Menu", replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task SendStatus(long chatId, CancellationToken ct)
    {
        var jobs = _jobManager.Jobs.ToList();
        var multiRunJobs = jobs.OfType<MultiRunJob>().ToList();

        var sb = new StringBuilder();
        sb.AppendLine("== ProjectBullet Status ==");
        sb.AppendLine($"Total Jobs: {jobs.Count}");
        sb.AppendLine($"Running: {jobs.Count(j => j.Status == JobStatus.Running)}");
        sb.AppendLine($"Idle: {jobs.Count(j => j.Status == JobStatus.Idle)}");
        sb.AppendLine();

        if (multiRunJobs.Any())
        {
            sb.AppendLine("-- Running Jobs --");
            foreach (var job in multiRunJobs.Where(j => j.Status == JobStatus.Running))
            {
                var total = job.DataPool?.Size ?? 0;
                var progress = total > 0 ? (double)job.DataTested / total * 100 : 0;
                sb.AppendLine($"#{job.Id} | {job.Config?.Metadata?.Name ?? "N/A"}");
                sb.AppendLine($"  Progress: {progress:F1}% | Hits: {job.DataHits} | CPM: {job.CPM}");
                sb.AppendLine($"  Tested: {job.DataTested} | Errors: {job.DataErrors}");
                sb.AppendLine();
            }
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Back to Menu", "menu") }
        });

        await _bot.SendMessage(chatId, sb.ToString(), replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task SendJobList(long chatId, CancellationToken ct)
    {
        var jobs = _jobManager.Jobs.ToList();

        if (!jobs.Any())
        {
            var emptyKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Back to Menu", "menu") }
            });
            await _bot.SendMessage(chatId, "No jobs found.", replyMarkup: emptyKeyboard, cancellationToken: ct);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var job in jobs)
        {
            var name = job is MultiRunJob mrj ? mrj.Config?.Metadata?.Name ?? "N/A" : "Proxy Check";
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(
                $"#{job.Id} [{job.Status}] {name}", $"job_actions_{job.Id}") });
        }
        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Back to Menu", "menu") });

        var keyboard = new InlineKeyboardMarkup(buttons);
        await _bot.SendMessage(chatId, "Select a job:", replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task SendJobActions(long chatId, int jobId, CancellationToken ct)
    {
        var job = _jobManager.Jobs.FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            await _bot.SendMessage(chatId, "Job not found.", cancellationToken: ct);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();
        if (job.Status == JobStatus.Idle)
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Start", $"start_job_{jobId}") });
        if (job.Status == JobStatus.Running || job.Status == JobStatus.Waiting)
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Stop", $"stop_job_{jobId}") });
        if (job.Status != JobStatus.Idle)
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Abort", $"abort_job_{jobId}") });

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Back to Jobs", "list_jobs") });

        var name = job is MultiRunJob mrj2 ? mrj2.Config?.Metadata?.Name ?? "N/A" : "Proxy Check";
        var info = $"Job #{jobId} - {name}\nStatus: {job.Status}";

        if (job is MultiRunJob mrj)
        {
            var total = mrj.DataPool?.Size ?? 0;
            var progress = total > 0 ? (double)mrj.DataTested / total * 100 : 0;
            info += $"\nProgress: {progress:F1}%\nHits: {mrj.DataHits} | CPM: {mrj.CPM}\nTested: {mrj.DataTested}/{total}";
        }

        var keyboard = new InlineKeyboardMarkup(buttons);
        await _bot.SendMessage(chatId, info, replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task SendConfigSelection(long chatId, CancellationToken ct)
    {
        var configs = _configService.Configs.ToList();
        if (!configs.Any())
        {
            await _bot.SendMessage(chatId, "No configs available.", cancellationToken: ct);
            return;
        }

        var sb = new StringBuilder("Available configs:\n");
        for (int i = 0; i < configs.Count && i < 20; i++)
        {
            sb.AppendLine($"{i + 1}. {configs[i].Metadata.Name}");
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Back to Menu", "menu") }
        });

        await _bot.SendMessage(chatId, sb.ToString(), replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task StartJob(long chatId, int jobId, CancellationToken ct)
    {
        var job = _jobManager.Jobs.FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            await _bot.SendMessage(chatId, "Job not found.", cancellationToken: ct);
            return;
        }

        try
        {
            await job.Start();
            if (job is MultiRunJob mrj && Settings.SendHitNotifications)
            {
                SubscribeToHits(mrj);
            }
            await _bot.SendMessage(chatId, $"Job #{jobId} started.", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId, $"Failed to start job: {ex.Message}", cancellationToken: ct);
        }
    }

    private async Task StopJob(long chatId, int jobId, CancellationToken ct)
    {
        var job = _jobManager.Jobs.FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            await _bot.SendMessage(chatId, "Job not found.", cancellationToken: ct);
            return;
        }

        try
        {
            await job.Stop();
            await _bot.SendMessage(chatId, $"Job #{jobId} stopped.", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId, $"Failed to stop job: {ex.Message}", cancellationToken: ct);
        }
    }

    private async Task AbortJob(long chatId, int jobId, CancellationToken ct)
    {
        var job = _jobManager.Jobs.FirstOrDefault(j => j.Id == jobId);
        if (job == null)
        {
            await _bot.SendMessage(chatId, "Job not found.", cancellationToken: ct);
            return;
        }

        try
        {
            await job.Abort();
            await _bot.SendMessage(chatId, $"Job #{jobId} aborted.", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await _bot.SendMessage(chatId, $"Failed to abort job: {ex.Message}", cancellationToken: ct);
        }
    }

    private async Task SendDetailedStats(long chatId, CancellationToken ct)
    {
        var jobs = _jobManager.Jobs.ToList();
        var multiRunJobs = jobs.OfType<MultiRunJob>().ToList();

        var sb = new StringBuilder();
        sb.AppendLine("== ProjectBullet Statistics ==");
        sb.AppendLine($"Total Jobs: {jobs.Count} | Running: {jobs.Count(j => j.Status == JobStatus.Running)} | Idle: {jobs.Count(j => j.Status == JobStatus.Idle)}");

        var totalHits = multiRunJobs.Sum(j => j.DataHits);
        var totalTested = multiRunJobs.Sum(j => j.DataTested);
        var successRate = totalTested > 0 ? (double)totalHits / totalTested * 100 : 0;
        var avgCpm = multiRunJobs.Where(j => j.Status == JobStatus.Running).Select(j => j.CPM).DefaultIfEmpty(0).Average();

        sb.AppendLine($"Total Hits: {totalHits:N0} | Success Rate: {successRate:F1}%");
        sb.AppendLine($"Average CPM: {avgCpm:F0}");
        sb.AppendLine();

        if (multiRunJobs.Any())
        {
            sb.AppendLine("-- Per Job Stats --");
            foreach (var job in multiRunJobs)
            {
                var total = job.DataPool?.Size ?? 0;
                var progress = total > 0 ? (double)job.DataTested / total * 100 : 0;
                sb.AppendLine($"#{job.Id} {job.Config?.Metadata?.Name ?? "N/A"} | {job.DataHits} hits | CPM: {job.CPM} | {progress:F1}%");
            }
        }

        await _bot.SendMessage(chatId, sb.ToString(), cancellationToken: ct);
    }

    private async Task SearchHits(long chatId, string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await _bot.SendMessage(chatId, "Usage: /search <query>", cancellationToken: ct);
            return;
        }

        // Search through running jobs' hits in memory
        var results = new List<string>();
        var multiRunJobs = _jobManager.Jobs.OfType<MultiRunJob>().ToList();

        foreach (var job in multiRunJobs)
        {
            var configName = job.Config?.Metadata?.Name ?? "Unknown";
            // We can only search through data that's been tested
            if (job.DataHits > 0)
            {
                results.Add($"Config: {configName} - {job.DataHits} hits (search in-memory not available, check Hits page)");
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Search results for '{query}':");

        if (!results.Any())
        {
            sb.AppendLine("No results found.");
        }
        else
        {
            foreach (var r in results.Take(10))
                sb.AppendLine(r);
        }

        await _bot.SendMessage(chatId, sb.ToString(), cancellationToken: ct);
    }

    private async Task SendConfigList(long chatId, CancellationToken ct)
    {
        var configs = _configService.Configs.ToList();
        if (!configs.Any())
        {
            await _bot.SendMessage(chatId, "No configs available.", cancellationToken: ct);
            return;
        }

        var sb = new StringBuilder("== Configs ==\n");
        for (int i = 0; i < configs.Count && i < 30; i++)
        {
            var c = configs[i];
            sb.AppendLine($"{i + 1}. {c.Metadata.Name} [{c.Metadata.Category}] by {c.Metadata.Author}");
        }

        if (configs.Count > 30)
            sb.AppendLine($"... and {configs.Count - 30} more");

        await _bot.SendMessage(chatId, sb.ToString(), cancellationToken: ct);
    }

    private async Task SendProxyStats(long chatId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("== Proxy Statistics ==");
        sb.AppendLine("Proxy statistics are available in the application UI.");
        sb.AppendLine("Use the Proxies page to view detailed proxy information.");

        await _bot.SendMessage(chatId, sb.ToString(), cancellationToken: ct);
    }

    private async Task SendHelp(long chatId, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("== ProjectBullet Bot Commands ==");
        sb.AppendLine("/start, /menu - Main menu");
        sb.AppendLine("/status - Quick status overview");
        sb.AppendLine("/stats - Detailed statistics");
        sb.AppendLine("/configs - List all configs");
        sb.AppendLine("/proxies - Proxy statistics");
        sb.AppendLine("/search <query> - Search hits");
        sb.AppendLine("/help - Show this help");

        await _bot.SendMessage(chatId, sb.ToString(), cancellationToken: ct);
    }

    public async Task SendMessageAsync(string text)
    {
        if (_bot == null || Settings.ChatId == 0) return;
        try
        {
            await _bot.SendMessage(Settings.ChatId, text);
        }
        catch { }
    }

    public void Dispose()
    {
        Stop();
    }
}
