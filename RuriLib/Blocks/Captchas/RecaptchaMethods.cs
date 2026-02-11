using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace RuriLib.Blocks.Captchas;

[BlockCategory("Recaptcha", "Blocks for solving ReCaptcha directly via anchor/reload API", "#4285F4")]
public static class RecaptchaMethods
{
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36";

    [Block("Solves a ReCaptcha by making direct requests to the anchor and reload endpoints",
        extraInfo = "Extract all parameters from the reCAPTCHA anchor URL found in DevTools (search for https://www.google.com/recaptcha/api2/anchor). The anchor URL contains ar, k (sitekey), co, hl, v, size, cb parameters.")]
    public static async Task<string> Recaptcha(BotData data,
        string ar = "1",
        string sitekey = "",
        string co = "",
        string hl = "en",
        string v = "",
        string size = "invisible",
        string action = "",
        string cb = "",
        string anchor = "https://www.google.com/recaptcha/api2/anchor",
        string reload = "https://www.google.com/recaptcha/api2/reload",
        string useragent = DefaultUserAgent)
    {
        data.Logger.LogHeader();

        if (string.IsNullOrEmpty(sitekey))
            throw new ArgumentException("Sitekey is required");

        if (string.IsNullOrEmpty(v))
            throw new ArgumentException("V (version) parameter is required");

        // Build anchor URL with query parameters
        var anchorUrl = $"{anchor}?ar={Uri.EscapeDataString(ar)}" +
                        $"&k={Uri.EscapeDataString(sitekey)}" +
                        $"&co={Uri.EscapeDataString(co)}" +
                        $"&hl={Uri.EscapeDataString(hl)}" +
                        $"&v={Uri.EscapeDataString(v)}" +
                        $"&size={Uri.EscapeDataString(size)}" +
                        $"&cb={Uri.EscapeDataString(cb)}";

        data.Logger.Log($"Fetching anchor page...", LogColors.ElectricBlue);

        using var httpClient = CreateHttpClient(useragent);

        // Step 1: GET anchor page to extract recaptcha-token
        var anchorResponse = await httpClient.GetStringAsync(anchorUrl).ConfigureAwait(false);

        var recaptchaToken = ExtractRecaptchaToken(anchorResponse);
        if (string.IsNullOrEmpty(recaptchaToken))
            throw new Exception("Failed to extract recaptcha-token from anchor page");

        data.Logger.Log($"Got recaptcha-token: {recaptchaToken[..Math.Min(50, recaptchaToken.Length)]}...", LogColors.ElectricBlue);

        // Step 2: POST to reload endpoint
        var reloadUrl = $"{reload}?k={Uri.EscapeDataString(sitekey)}";

        var postData = new Dictionary<string, string>
        {
            ["v"] = v,
            ["reason"] = "q",
            ["c"] = recaptchaToken,
            ["k"] = sitekey,
            ["co"] = co,
            ["hl"] = hl,
            ["size"] = size,
            ["chr"] = "[89,64,27]",
            ["vh"] = "-1",
            ["bg"] = ""
        };

        if (!string.IsNullOrEmpty(action))
        {
            postData["action"] = action;
            postData["sa"] = action;
        }

        data.Logger.Log($"Sending reload request...", LogColors.ElectricBlue);

        var reloadResponse = await httpClient.PostAsync(reloadUrl,
            new FormUrlEncodedContent(postData), data.CancellationToken).ConfigureAwait(false);

        var reloadBody = await reloadResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        var gRecaptchaResponse = ExtractGRecaptchaResponse(reloadBody);
        if (string.IsNullOrEmpty(gRecaptchaResponse))
            throw new Exception("Failed to extract g-recaptcha-response from reload response");

        data.Logger.Log($"Got g-recaptcha-response (length: {gRecaptchaResponse.Length})", LogColors.ElectricBlue);

        return gRecaptchaResponse;
    }

    [Block("Solves a ReCaptcha by providing the full captcha anchor URL",
        extraInfo = "Provide the full reCAPTCHA anchor URL (e.g. https://www.google.com/recaptcha/api2/anchor?ar=1&k=...). The block will automatically extract all parameters and solve the captcha.")]
    public static async Task<string> Recaptcha1(BotData data,
        string captchaurl = "",
        bool enterprise = false,
        string useragent = DefaultUserAgent)
    {
        data.Logger.LogHeader();

        if (string.IsNullOrEmpty(captchaurl))
            throw new ArgumentException("Captchaurl is required");

        data.Logger.Log($"Parsing captcha URL...", LogColors.ElectricBlue);

        // Parse URL to extract parameters
        var uri = new Uri(captchaurl);
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var sitekey = queryParams["k"] ?? "";
        var co = queryParams["co"] ?? "";
        var hl = queryParams["hl"] ?? "en";
        var v = queryParams["v"] ?? "";
        var size = queryParams["size"] ?? "invisible";
        var ar = queryParams["ar"] ?? "1";
        var cb = queryParams["cb"] ?? "";
        var action = queryParams["action"] ?? "";

        if (string.IsNullOrEmpty(sitekey))
            throw new Exception("Could not extract sitekey (k) from captcha URL");

        if (string.IsNullOrEmpty(v))
            throw new Exception("Could not extract version (v) from captcha URL");

        data.Logger.Log($"Sitekey: {sitekey}", LogColors.ElectricBlue);
        data.Logger.Log($"Enterprise: {enterprise}", LogColors.ElectricBlue);

        // Build base URLs based on enterprise flag
        var basePath = enterprise ? "/recaptcha/enterprise" : "/recaptcha/api2";
        var anchorBase = $"{uri.Scheme}://{uri.Host}{basePath}/anchor";
        var reloadBase = $"{uri.Scheme}://{uri.Host}{basePath}/reload";

        using var httpClient = CreateHttpClient(useragent);

        // Step 1: GET anchor page
        data.Logger.Log($"Fetching anchor page...", LogColors.ElectricBlue);
        var anchorResponse = await httpClient.GetStringAsync(captchaurl).ConfigureAwait(false);

        var recaptchaToken = ExtractRecaptchaToken(anchorResponse);
        if (string.IsNullOrEmpty(recaptchaToken))
            throw new Exception("Failed to extract recaptcha-token from anchor page");

        data.Logger.Log($"Got recaptcha-token: {recaptchaToken[..Math.Min(50, recaptchaToken.Length)]}...", LogColors.ElectricBlue);

        // Step 2: POST to reload endpoint
        var reloadUrl = $"{reloadBase}?k={Uri.EscapeDataString(sitekey)}";

        var postData = new Dictionary<string, string>
        {
            ["v"] = v,
            ["reason"] = "q",
            ["c"] = recaptchaToken,
            ["k"] = sitekey,
            ["co"] = co,
            ["hl"] = hl,
            ["size"] = size,
            ["chr"] = "[89,64,27]",
            ["vh"] = "-1",
            ["bg"] = ""
        };

        if (!string.IsNullOrEmpty(action))
        {
            postData["action"] = action;
            postData["sa"] = action;
        }

        data.Logger.Log($"Sending reload request...", LogColors.ElectricBlue);

        var reloadResponse = await httpClient.PostAsync(reloadUrl,
            new FormUrlEncodedContent(postData), data.CancellationToken).ConfigureAwait(false);

        var reloadBody = await reloadResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

        var gRecaptchaResponse = ExtractGRecaptchaResponse(reloadBody);
        if (string.IsNullOrEmpty(gRecaptchaResponse))
            throw new Exception("Failed to extract g-recaptcha-response from reload response");

        data.Logger.Log($"Got g-recaptcha-response (length: {gRecaptchaResponse.Length})", LogColors.ElectricBlue);

        return gRecaptchaResponse;
    }

    private static HttpClient CreateHttpClient(string userAgent)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

        return client;
    }

    private static string ExtractRecaptchaToken(string html)
    {
        // Look for: <input ... id="recaptcha-token" ... value="TOKEN">
        var match = Regex.Match(html, @"id=""recaptcha-token""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        // Fallback: value before id
        match = Regex.Match(html, @"value=""([^""]+)""[^>]*id=""recaptcha-token""", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        return string.Empty;
    }

    private static string ExtractGRecaptchaResponse(string body)
    {
        // Response format: )]}\n["rresp","TOKEN",null,120]
        var match = Regex.Match(body, @"\[""rresp"",""([^""]+)""", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value;

        // Fallback: try to find any long base64-like token in the response
        match = Regex.Match(body, @"""(03[A-Za-z0-9_-]{30,})""");
        if (match.Success)
            return match.Groups[1].Value;

        return string.Empty;
    }
}
