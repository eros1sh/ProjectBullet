using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Core.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ProjectBullet.Telegram.Services;

public class WebhookPollingService : IDisposable
{
    private readonly MarketplaceApiService _marketplaceApi;
    private readonly TelegramBotService _telegramBot;
    private readonly ProjectBulletSettingsService _settingsService;
    private CancellationTokenSource _cts;
    private Task _pollingTask;

    private TelegramSettings Settings => _settingsService.Settings.TelegramSettings;

    public bool IsRunning => _pollingTask != null && !_pollingTask.IsCompleted;

    public WebhookPollingService(
        MarketplaceApiService marketplaceApi,
        TelegramBotService telegramBot,
        ProjectBulletSettingsService settingsService)
    {
        _marketplaceApi = marketplaceApi;
        _telegramBot = telegramBot;
        _settingsService = settingsService;

        if (Settings.Enabled && Settings.UseWebhookRelay && !string.IsNullOrEmpty(Settings.WebhookSecret))
        {
            Start();
        }
    }

    public void Start()
    {
        if (IsRunning) return;
        if (!Settings.UseWebhookRelay || string.IsNullOrEmpty(Settings.WebhookSecret)) return;

        _cts = new CancellationTokenSource();
        _pollingTask = PollLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _pollingTask = null;
    }

    public void Restart()
    {
        Stop();
        if (Settings.Enabled && Settings.UseWebhookRelay && !string.IsNullOrEmpty(Settings.WebhookSecret))
        {
            Start();
        }
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var (result, error) = await _marketplaceApi.PollWebhookAsync().ConfigureAwait(false);

                if (result?.Messages != null)
                {
                    foreach (var msg in result.Messages)
                    {
                        try
                        {
                            var update = JsonSerializer.Deserialize<Update>(msg.UpdateData);
                            if (update != null)
                            {
                                await _telegramBot.ProcessUpdateAsync(update).ConfigureAwait(false);
                            }

                            await _marketplaceApi.AckWebhookMessageAsync(msg.Id).ConfigureAwait(false);
                        }
                        catch
                        {
                            // Ack even on processing error to avoid stuck messages
                            await _marketplaceApi.AckWebhookMessageAsync(msg.Id).ConfigureAwait(false);
                        }
                    }
                }

                // Poll every 2 seconds
                await Task.Delay(2000, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // On error, wait longer before retrying
                try { await Task.Delay(5000, ct).ConfigureAwait(false); } catch { break; }
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
