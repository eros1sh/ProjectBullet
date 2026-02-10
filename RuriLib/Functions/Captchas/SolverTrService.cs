using CaptchaSharp.Enums;
using CaptchaSharp.Models;
using CaptchaSharp.Models.CaptchaResponses;
using CaptchaSharp.Services;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Functions.Captchas;

/// <summary>
/// Captcha service implementation for Solver.tr (https://solver.tr/).
/// Supports Cloudflare Turnstile captcha solving.
/// </summary>
public class SolverTrService : CaptchaService
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.solver.tr";

    public SolverTrService(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }

    public override CaptchaServiceCapabilities Capabilities => new();

    public override async Task<decimal> GetBalanceAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<SolverTrBalanceResponse>(
            $"{BaseUrl}/v1/balance?apikey={Uri.EscapeDataString(_apiKey)}", cancellationToken).ConfigureAwait(false);

        if (response is null || !response.Success)
            throw new Exception("Failed to get balance from Solver.tr");

        return response.Balance;
    }

    public override async Task<CloudflareTurnstileResponse> SolveCloudflareTurnstileAsync(
        string siteKey, string siteUrl, string? action = null, string? data = null,
        string? pageData = null, SessionParams? sessionParams = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/v1/turnstile/solve?apikey={Uri.EscapeDataString(_apiKey)}&url={Uri.EscapeDataString(siteUrl)}&sitekey={Uri.EscapeDataString(siteKey)}";

        if (!string.IsNullOrEmpty(action))
            url += $"&action={Uri.EscapeDataString(action)}";

        if (!string.IsNullOrEmpty(data))
            url += $"&cdata={Uri.EscapeDataString(data)}";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        var response = await _httpClient.GetFromJsonAsync<SolverTrSolveResponse>(
            url, cts.Token).ConfigureAwait(false);

        if (response is null || !response.Success || string.IsNullOrEmpty(response.Token))
            throw new Exception("Failed to solve Turnstile captcha via Solver.tr");

        return new CloudflareTurnstileResponse
        {
            Id = response.RequestId ?? "",
            Response = response.Token
        };
    }

    #region Response Models
    private class SolverTrBalanceResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    private class SolverTrSolveResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("responseTime")]
        public int ResponseTime { get; set; }
    }
    #endregion
}
