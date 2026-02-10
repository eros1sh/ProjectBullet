using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Captchas;

[BlockCategory("12ws", "Blocks for solving captchas via 12ws (wssolver.net) API", "#e91e63")]
public static class Twelve12WsMethods
{
    [Block("Solves a captcha using the 12ws (wssolver.net) API",
        extraInfo = "Calls https://wssolver.net/token?key=<api_key>&site=<site_name> and returns the token as text. Requires an API key from https://wssolver.net/")]
    public static async Task<string> Solve12wsCaptcha(BotData data,
        [BlockParam("API Key", "Your 12ws (wssolver.net) API key")] string apiKey,
        [BlockParam("Site Name", "The site name parameter for the captcha")] string siteName)
    {
        data.Logger.LogHeader();

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(120);

        var requestUrl = $"https://wssolver.net/token?key={Uri.EscapeDataString(apiKey)}&site={Uri.EscapeDataString(siteName)}";

        data.Logger.Log($"[12ws] Solving captcha for site: {siteName}...", LogColors.ElectricBlue);

        var response = await httpClient.GetAsync(requestUrl, data.CancellationToken).ConfigureAwait(false);
        var token = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(token))
            throw new Exception($"12ws failed to solve captcha. Status: {response.StatusCode}, Response: {token}");

        token = token.Trim();

        data.Logger.Log($"[12ws] Got token: {token}", LogColors.ElectricBlue);

        return token;
    }
}
