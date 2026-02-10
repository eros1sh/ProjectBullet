using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Cloudflare
{
    [BlockCategory("Cloudflare", "Blocks for bypassing Cloudflare protection", "#F48120")]
    public static class Methods
    {
        [Block("Solves a Cloudflare JS challenge by navigating with Puppeteer and extracting clearance cookies",
            extraInfo = "Opens a browser, navigates to the URL, waits for the Cloudflare challenge to be solved, then extracts cookies. Requires Puppeteer browser to be opened first.")]
        public static async Task<Dictionary<string, string>> CloudflareBypassPuppeteer(
            BotData data,
            string url,
            int timeoutSeconds = 30,
            bool closeBrowserAfter = true)
        {
            data.Logger.LogHeader();
            data.Logger.Log($"Attempting Cloudflare bypass for {url}", "#F48120");

            var page = data.TryGetObject<PuppeteerSharp.IPage>("puppeteerPage")
                ?? throw new Exception("No Puppeteer page found. Please open a browser first using PuppeteerOpenBrowser.");

            await page.GoToAsync(url, new PuppeteerSharp.NavigationOptions
            {
                WaitUntil = new[] { PuppeteerSharp.WaitUntilNavigation.DOMContentLoaded },
                Timeout = timeoutSeconds * 1000
            }).ConfigureAwait(false);

            data.Logger.Log("Page loaded, waiting for Cloudflare challenge to resolve...", "#F48120");

            // Poll for cf_clearance cookie
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            PuppeteerSharp.CookieParam[] cookies = null;

            while (DateTime.UtcNow < deadline)
            {
                data.CancellationToken.ThrowIfCancellationRequested();

                cookies = await page.GetCookiesAsync().ConfigureAwait(false);

                if (cookies.Any(c => c.Name == "cf_clearance"))
                {
                    data.Logger.Log("cf_clearance cookie found!", "#F48120");
                    break;
                }

                await Task.Delay(1000, data.CancellationToken).ConfigureAwait(false);
            }

            // Extract all cookies
            var result = new Dictionary<string, string>();
            if (cookies != null)
            {
                foreach (var cookie in cookies)
                {
                    result[cookie.Name] = cookie.Value;
                    data.COOKIES[cookie.Name] = cookie.Value;
                }
            }

            data.Logger.Log($"Extracted {result.Count} cookies", "#F48120");

            if (closeBrowserAfter)
            {
                var browser = data.TryGetObject<PuppeteerSharp.IBrowser>("puppeteerBrowser");
                if (browser != null)
                {
                    await browser.CloseAsync().ConfigureAwait(false);
                    data.Logger.Log("Browser closed", "#F48120");
                }
            }

            return result;
        }

        [Block("Checks if a Cloudflare challenge page is present in the response")]
        public static bool IsCloudflareChallenge(BotData data, [Variable] string responseBody)
        {
            var indicators = new[]
            {
                "cf-browser-verification",
                "cf_chl_opt",
                "jschl_vc",
                "jschl_answer",
                "cf-challenge-running",
                "Checking your browser",
                "challenge-platform",
                "_cf_chl_opt"
            };

            var isChallenge = indicators.Any(i =>
                responseBody.Contains(i, StringComparison.OrdinalIgnoreCase));

            data.Logger.LogHeader();
            data.Logger.Log($"Cloudflare challenge detected: {isChallenge}", "#F48120");
            return isChallenge;
        }

        [Block("Extracts cf_clearance cookie value from current cookies")]
        public static string GetClearanceCookie(BotData data)
        {
            var value = data.COOKIES.TryGetValue("cf_clearance", out var v) ? v : string.Empty;

            data.Logger.LogHeader();

            if (string.IsNullOrEmpty(value))
                data.Logger.Log("cf_clearance cookie not found", "#F48120");
            else
                data.Logger.Log($"cf_clearance = {value}", "#F48120");

            return value;
        }
    }
}
