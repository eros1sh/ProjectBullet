using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Captchas;

[BlockCategory("SolverTr", "Blocks for solving captchas via Solver.tr API", "#9333ea")]
public static class SolverTrMethods
{
    [Block("Solves a Cloudflare Turnstile captcha using Solver.tr API",
        extraInfo = "Directly calls the Solver.tr API to solve Turnstile challenges. Requires an API key from https://solver.tr/")]
    public static async Task<string> SolveSolverTrTurnstile(BotData data,
        [BlockParam("API Key", "Your Solver.tr API key")] string apiKey,
        [BlockParam("URL", "The page URL where the Turnstile captcha is located")] string url,
        [BlockParam("Site Key", "The Cloudflare Turnstile site key")] string siteKey,
        string action = "",
        string cData = "")
    {
        data.Logger.LogHeader();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var requestUrl = $"https://api.solver.tr/v1/turnstile/solve?apikey={Uri.EscapeDataString(apiKey)}&url={Uri.EscapeDataString(url)}&sitekey={Uri.EscapeDataString(siteKey)}";

        if (!string.IsNullOrEmpty(action))
            requestUrl += $"&action={Uri.EscapeDataString(action)}";

        if (!string.IsNullOrEmpty(cData))
            requestUrl += $"&cdata={Uri.EscapeDataString(cData)}";

        data.Logger.Log($"[SolverTr] Solving Turnstile for {url}...", LogColors.ElectricBlue);

        var response = await httpClient.GetFromJsonAsync<SolverTrResponse>(
            requestUrl, data.CancellationToken).ConfigureAwait(false);

        if (response is null || !response.Success || string.IsNullOrEmpty(response.Token))
            throw new Exception($"Solver.tr failed to solve Turnstile captcha");

        data.Logger.Log($"[SolverTr] Solved in {response.ResponseTime}ms | Balance: {response.Balance}", LogColors.ElectricBlue);
        data.Logger.Log($"Got solution: {response.Token}", LogColors.ElectricBlue);

        return response.Token;
    }

    [Block("Checks the balance of your Solver.tr account")]
    public static async Task<float> SolverTrCheckBalance(BotData data,
        [BlockParam("API Key", "Your Solver.tr API key")] string apiKey)
    {
        data.Logger.LogHeader();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var response = await httpClient.GetFromJsonAsync<SolverTrBalanceResponse>(
            $"https://api.solver.tr/v1/balance?apikey={Uri.EscapeDataString(apiKey)}",
            data.CancellationToken).ConfigureAwait(false);

        if (response is null || !response.Success)
            throw new Exception("Failed to get Solver.tr balance");

        data.Logger.Log($"[SolverTr] Balance: {response.Balance} | User: {response.Username}", LogColors.ElectricBlue);

        return (float)response.Balance;
    }

    [Block("Gets usage statistics from your Solver.tr account")]
    public static async Task<string> SolverTrUsageStats(BotData data,
        [BlockParam("API Key", "Your Solver.tr API key")] string apiKey)
    {
        data.Logger.LogHeader();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var response = await httpClient.GetFromJsonAsync<SolverTrUsageResponse>(
            $"https://api.solver.tr/v1/usage-stats?apikey={Uri.EscapeDataString(apiKey)}",
            data.CancellationToken).ConfigureAwait(false);

        if (response is null || !response.Success)
            throw new Exception("Failed to get Solver.tr usage stats");

        var stats = $"Total: {response.TotalRequests} | Success: {response.SuccessfulRequests} | Failed: {response.FailedRequests} | Rate: {response.SuccessRate}%";
        data.Logger.Log($"[SolverTr] {stats}", LogColors.ElectricBlue);

        return stats;
    }

    #region Response Models
    private class SolverTrResponse
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

    private class SolverTrBalanceResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("balance")]
        public decimal Balance { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }

    private class SolverTrUsageResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("totalRequests")]
        public int TotalRequests { get; set; }

        [JsonPropertyName("successfulRequests")]
        public int SuccessfulRequests { get; set; }

        [JsonPropertyName("failedRequests")]
        public int FailedRequests { get; set; }

        [JsonPropertyName("successRate")]
        public double SuccessRate { get; set; }
    }
    #endregion
}
