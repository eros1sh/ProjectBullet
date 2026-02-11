using DeviceId;
using ProjectBullet.Core.Models.Marketplace;
using ProjectBullet.Core.Models.Settings;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectBullet.Core.Services;

public class MarketplaceApiService
{
    private const string BaseUrl = "https://projectbullet.eros.sh/api";

    private readonly ProjectBulletSettingsService _settingsService;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _reAuthLock = new(1, 1);
    private bool _isReAuthenticating;

    private MarketplaceSettings Settings => _settingsService.Settings.MarketplaceSettings;

    public MarketplaceUser CurrentUser { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Settings.AuthToken);
    public bool IsRegisteredUser => Settings.IsRegisteredUser;
    public string Username => Settings.CachedUsername;

    public MarketplaceApiService(ProjectBulletSettingsService settingsService)
    {
        _settingsService = settingsService;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ProjectBullet-Marketplace/2.0");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    private void SetAuthHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            !string.IsNullOrEmpty(Settings.AuthToken)
                ? new AuthenticationHeaderValue("Bearer", Settings.AuthToken)
                : null;
    }

    /// <summary>
    /// Re-authenticate via HWID init when a 401 is received.
    /// Thread-safe: only one re-auth happens at a time.
    /// </summary>
    private async Task<bool> ReAuthenticateAsync()
    {
        if (_isReAuthenticating) return false;

        await _reAuthLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _isReAuthenticating = true;

            var hwid = GetHWID();
            var payload = JsonSerializer.Serialize(new { hwid });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            // Don't set auth header for init - it doesn't require auth
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var response = await _httpClient.PostAsync($"{BaseUrl}/auth/init", content).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var authResp = JsonSerializer.Deserialize<AuthResponse>(json, _jsonOptions);
                if (authResp != null)
                {
                    Settings.AuthToken = authResp.Token;
                    Settings.CachedUsername = authResp.User?.Username ?? Settings.CachedUsername;
                    Settings.CachedUserId = authResp.User?.Id ?? Settings.CachedUserId;
                    Settings.IsRegisteredUser = authResp.User?.IsRegistered ?? Settings.IsRegisteredUser;
                    CurrentUser = authResp.User;
                    SetAuthHeader();
                    await _settingsService.SaveAsync().ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _isReAuthenticating = false;
            _reAuthLock.Release();
        }
    }

    private static string GetHWID()
    {
        var builder = new DeviceIdBuilder()
            .AddUserName()
            .AddMachineName()
            .AddOSVersion()
            .AddMacAddress()
            .AddSystemDriveSerialNumber()
            .AddOSInstallationID();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder
                .AddProcessorId()
                .AddMotherboardSerialNumber()
                .AddSystemUUID();
        }

        return builder.ToString();
    }

    public async Task InitAsync()
    {
        try
        {
            // If we already have a token, try to validate it
            if (!string.IsNullOrEmpty(Settings.AuthToken))
            {
                SetAuthHeader();
                var me = await GetMeAsync().ConfigureAwait(false);
                if (me != null)
                {
                    CurrentUser = me;
                    Settings.CachedUsername = me.Username;
                    Settings.CachedUserId = me.Id;
                    Settings.IsRegisteredUser = me.IsRegistered;
                    await _settingsService.SaveAsync().ConfigureAwait(false);
                    return;
                }
            }

            // Token invalid or missing — re-authenticate via HWID
            await ReAuthenticateAsync().ConfigureAwait(false);
        }
        catch
        {
            // Silently fail on init — marketplace is optional
            // Settings remain unchanged so registered users keep their cached state
        }
    }

    public async Task<(bool Success, string Error)> LoginAsync(string username, string password)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { username, password });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            SetAuthHeader();

            var response = await _httpClient.PostAsync($"{BaseUrl}/auth/login", content).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var authResp = JsonSerializer.Deserialize<AuthResponse>(json, _jsonOptions);
                if (authResp != null)
                {
                    Settings.AuthToken = authResp.Token;
                    Settings.CachedUsername = authResp.User?.Username ?? username;
                    Settings.CachedUserId = authResp.User?.Id ?? 0;
                    Settings.IsRegisteredUser = true;
                    CurrentUser = authResp.User;
                    SetAuthHeader();
                    await _settingsService.SaveAsync().ConfigureAwait(false);
                    return (true, null);
                }
            }

            var error = TryGetError(json);
            return (false, error ?? "Login failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Error)> RegisterAsync(string username, string password)
    {
        try
        {
            var hwid = GetHWID();
            var payload = JsonSerializer.Serialize(new { username, password, hwid });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            SetAuthHeader();

            var response = await _httpClient.PostAsync($"{BaseUrl}/auth/register", content).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var authResp = JsonSerializer.Deserialize<AuthResponse>(json, _jsonOptions);
                if (authResp != null)
                {
                    Settings.AuthToken = authResp.Token;
                    Settings.CachedUsername = authResp.User?.Username ?? username;
                    Settings.CachedUserId = authResp.User?.Id ?? 0;
                    Settings.IsRegisteredUser = true;
                    CurrentUser = authResp.User;
                    SetAuthHeader();
                    await _settingsService.SaveAsync().ConfigureAwait(false);
                    return (true, null);
                }
            }

            var error = TryGetError(json);
            return (false, error ?? "Registration failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<MarketplaceUser> GetMeAsync()
    {
        try
        {
            SetAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/auth/me").ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // The API returns { "user": { ... } } wrapper
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("user", out var userElement))
                {
                    return JsonSerializer.Deserialize<MarketplaceUser>(userElement.GetRawText(), _jsonOptions);
                }

                // Fallback: try direct deserialization
                return JsonSerializer.Deserialize<MarketplaceUser>(json, _jsonOptions);
            }

            // 401 means token is invalid/expired
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null; // Caller (InitAsync) will trigger re-auth
            }
        }
        catch { }

        return null;
    }

    public async Task Logout()
    {
        Settings.AuthToken = string.Empty;
        Settings.IsRegisteredUser = false;
        Settings.CachedUsername = string.Empty;
        Settings.CachedUserId = 0;
        CurrentUser = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        await _settingsService.SaveAsync().ConfigureAwait(false);
        await InitAsync().ConfigureAwait(false);
    }

    public async Task<ItemsResponse> GetItemsAsync(
        string category = null, string search = null,
        string sort = null, int page = 1)
    {
        try
        {
            SetAuthHeader();
            var query = $"?page={page}";
            if (!string.IsNullOrWhiteSpace(category) && category != "all")
                query += $"&category={Uri.EscapeDataString(category)}";
            if (!string.IsNullOrWhiteSpace(search))
                query += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrWhiteSpace(sort))
                query += $"&sort={Uri.EscapeDataString(sort)}";

            var response = await _httpClient.GetAsync($"{BaseUrl}/items{query}").ConfigureAwait(false);

            // Auto-recover from expired token
            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.GetAsync($"{BaseUrl}/items{query}").ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ItemsResponse>(json, _jsonOptions)
                    ?? new ItemsResponse();
            }
        }
        catch { }

        return new ItemsResponse();
    }

    public async Task<ItemsResponse> GetMyItemsAsync()
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/items?user_id={Settings.CachedUserId}";
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ItemsResponse>(json, _jsonOptions)
                    ?? new ItemsResponse();
            }
        }
        catch { }

        return new ItemsResponse();
    }

    public async Task<MarketplaceItemDetail> GetItemDetailAsync(int id)
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/items/{id}";
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var detail = JsonSerializer.Deserialize<ItemDetailResponse>(json, _jsonOptions);
                return detail?.Item;
            }
        }
        catch { }

        return null;
    }

    public async Task<(byte[] Data, string FileName, string Error)> DownloadItemAsync(int id, string password = null)
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/items/{id}/download";
            if (!string.IsNullOrEmpty(password))
                url += $"?password={Uri.EscapeDataString(password)}";

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                    ?? $"download_{id}";
                return (data, fileName, null);
            }

            var errorJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = TryGetError(errorJson);
            return (null, null, error ?? "Download failed.");
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }

    public async Task<(bool Success, string Error)> UploadItemAsync(
        string name, string category, string description,
        string version, string password, string filePath)
    {
        try
        {
            SetAuthHeader();

            MultipartFormDataContent BuildForm()
            {
                var form = new MultipartFormDataContent();
                form.Add(new StringContent(name), "name");
                form.Add(new StringContent(category), "category");
                form.Add(new StringContent(description ?? string.Empty), "description");
                form.Add(new StringContent(version), "version");
                if (!string.IsNullOrEmpty(password))
                    form.Add(new StringContent(password), "password");
                var fileBytes = File.ReadAllBytes(filePath);
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(fileContent, "file", Path.GetFileName(filePath));
                return form;
            }

            using var form1 = BuildForm();
            var response = await _httpClient.PostAsync($"{BaseUrl}/items", form1).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                using var form2 = BuildForm();
                response = await _httpClient.PostAsync($"{BaseUrl}/items", form2).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = TryGetError(json);
            return (false, error ?? "Upload failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Error)> UploadUserFileAsync(
        string name, string type, byte[] fileContent, string fileName)
    {
        try
        {
            SetAuthHeader();

            MultipartFormDataContent BuildForm()
            {
                var form = new MultipartFormDataContent();
                form.Add(new StringContent(name), "name");
                form.Add(new StringContent(type), "type");
                var content = new ByteArrayContent(fileContent);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(content, "file", fileName);
                return form;
            }

            using var form1 = BuildForm();
            var response = await _httpClient.PostAsync($"{BaseUrl}/uploads/send", form1).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                using var form2 = BuildForm();
                response = await _httpClient.PostAsync($"{BaseUrl}/uploads/send", form2).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return (true, null);

            var error = TryGetError(json);
            return (false, error ?? "Upload failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Error)> DeleteItemAsync(int id)
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/items/{id}";
            var response = await _httpClient.DeleteAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.DeleteAsync(url).ConfigureAwait(false);
            }

            if (response.IsSuccessStatusCode)
                return (true, null);

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = TryGetError(json);
            return (false, error ?? "Delete failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── Login Link ──

    public async Task<(string Url, string Error)> GetLoginLinkAsync()
    {
        try
        {
            SetAuthHeader();
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/auth/login-link", content).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                content = new StringContent("{}", Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync($"{BaseUrl}/auth/login-link", content).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(json);
                var url = doc.RootElement.GetProperty("url").GetString();
                return (url, null);
            }

            var error = TryGetError(json);
            return (null, error ?? "Failed to generate login link.");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // ── Webhook Relay ──

    public async Task<(WebhookSetupResponse Result, string Error)> SetupWebhookAsync(string botToken)
    {
        try
        {
            SetAuthHeader();
            var payload = JsonSerializer.Serialize(new { bot_token = botToken });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/webhook/setup", content).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                content = new StringContent(payload, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync($"{BaseUrl}/webhook/setup", content).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WebhookSetupResponse>(json, _jsonOptions);
                return (result, null);
            }

            var error = TryGetError(json);
            return (null, error ?? "Webhook setup failed.");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(WebhookConfigResponse Result, string Error)> GetWebhookConfigAsync()
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/webhook/config";
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WebhookConfigResponse>(json, _jsonOptions);
                return (result, null);
            }

            var error = TryGetError(json);
            return (null, error ?? "Failed to get webhook config.");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(bool Success, string Error)> DeleteWebhookAsync()
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/webhook/config";
            var response = await _httpClient.DeleteAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.DeleteAsync(url).ConfigureAwait(false);
            }

            if (response.IsSuccessStatusCode)
                return (true, null);

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var error = TryGetError(json);
            return (false, error ?? "Failed to delete webhook.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(WebhookPollResponse Result, string Error)> PollWebhookAsync()
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/webhook/poll";
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<WebhookPollResponse>(json, _jsonOptions);
                return (result, null);
            }

            var error = TryGetError(json);
            return (null, error ?? "Webhook poll failed.");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<bool> AckWebhookMessageAsync(int messageId)
    {
        try
        {
            SetAuthHeader();
            var url = $"{BaseUrl}/webhook/messages/{messageId}/ack";
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await ReAuthenticateAsync().ConfigureAwait(false))
            {
                SetAuthHeader();
                content = new StringContent("{}", Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync(url, content).ConfigureAwait(false);
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ── Heartbeat ──

    private Timer _heartbeatTimer;

    public void StartHeartbeat(string appVersion = null)
    {
        _heartbeatTimer?.Dispose();

        // Send immediately, then every 2 minutes
        _heartbeatTimer = new Timer(async _ =>
        {
            await SendHeartbeatAsync(appVersion).ConfigureAwait(false);
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
    }

    public void StopHeartbeat()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    public async Task SendHeartbeatAsync(string appVersion = null)
    {
        try
        {
            if (!IsAuthenticated) return;

            SetAuthHeader();
            var hwid = GetHWID();
            var os = RuntimeInformation.OSDescription;
            var version = appVersion ?? "unknown";

            var payload = JsonSerializer.Serialize(new { hwid, os, version });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{BaseUrl}/heartbeat", content).ConfigureAwait(false);

            // If token expired, re-authenticate and retry once
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (await ReAuthenticateAsync().ConfigureAwait(false))
                {
                    SetAuthHeader();
                    var retryPayload = JsonSerializer.Serialize(new { hwid, os, version });
                    var retryContent = new StringContent(retryPayload, Encoding.UTF8, "application/json");
                    await _httpClient.PostAsync($"{BaseUrl}/heartbeat", retryContent).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            // Silently fail — heartbeat is non-critical
        }
    }

    private string TryGetError(string json)
    {
        try
        {
            var err = JsonSerializer.Deserialize<ErrorResponse>(json, _jsonOptions);
            return err?.Error;
        }
        catch
        {
            return null;
        }
    }
}
