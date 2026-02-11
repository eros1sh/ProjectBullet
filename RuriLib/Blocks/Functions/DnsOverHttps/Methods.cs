using Newtonsoft.Json.Linq;
using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Functions.DnsOverHttpsFunctions
{
    [BlockCategory("DNS-over-HTTPS", "Blocks for secure DNS queries", "#42a5f5")]
    public static class Methods
    {
        [Block("Resolves a hostname using DNS-over-HTTPS and returns all matching records")]
        public static async Task<List<string>> DohResolve(BotData data, string hostname,
            string recordType = "A", string provider = "https://cloudflare-dns.com/dns-query")
        {
            data.Logger.LogHeader();

            using var client = new HttpClient();
            var requestUrl = $"{provider}?name={Uri.EscapeDataString(hostname)}&type={Uri.EscapeDataString(recordType)}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Accept", "application/dns-json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(data.CancellationToken);
            cts.CancelAfter(15000);

            var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

            var json = JObject.Parse(responseBody);
            var answers = json["Answer"];
            var results = new List<string>();

            if (answers != null)
            {
                results = answers.Select(a => a["data"]?.ToString()?.Trim('"') ?? string.Empty)
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToList();
            }

            data.Logger.Log($"DoH resolved {hostname} ({recordType}) via {provider}:", LogColors.YellowGreen);
            data.Logger.Log($"Got {results.Count} results:", LogColors.YellowGreen);
            foreach (var result in results)
            {
                data.Logger.Log($"  {result}", LogColors.YellowGreen);
            }

            return results;
        }

        [Block("Resolves a hostname using DNS-over-HTTPS and returns only the first result")]
        public static async Task<string> DohResolveFirst(BotData data, string hostname,
            string recordType = "A", string provider = "https://cloudflare-dns.com/dns-query")
        {
            data.Logger.LogHeader();

            using var client = new HttpClient();
            var requestUrl = $"{provider}?name={Uri.EscapeDataString(hostname)}&type={Uri.EscapeDataString(recordType)}";

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Accept", "application/dns-json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(data.CancellationToken);
            cts.CancelAfter(15000);

            var response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

            var json = JObject.Parse(responseBody);
            var answers = json["Answer"];
            var result = string.Empty;

            if (answers != null && answers.Any())
            {
                result = answers.First()["data"]?.ToString()?.Trim('"') ?? string.Empty;
            }

            data.Logger.Log($"DoH resolved first {hostname} ({recordType}) via {provider}: {result}", LogColors.YellowGreen);

            return result;
        }
    }
}
