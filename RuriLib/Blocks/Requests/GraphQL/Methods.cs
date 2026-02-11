using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Requests.GraphQL
{
    [BlockCategory("GraphQL", "Blocks for GraphQL API requests", "#e535ab")]
    public static class Methods
    {
        [Block("Sends a GraphQL query to the specified URL and returns the JSON response")]
        public static async Task<string> GraphQLQuery(BotData data, string url, string query,
            string variables = "{}", string operationName = "", int timeoutMs = 15000)
        {
            data.Logger.LogHeader();

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

            var payload = new
            {
                query,
                variables,
                operationName
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, data.CancellationToken);

            var response = await client.PostAsync(url, content, linkedCts.Token).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

            data.Logger.Log($"GraphQL query to {url}", LogColors.YellowGreen);
            data.Logger.Log($"Response: {responseBody}", LogColors.YellowGreen);

            return responseBody;
        }

        [Block("Sends a GraphQL query with custom headers to the specified URL and returns the JSON response")]
        public static async Task<string> GraphQLQueryWithHeaders(BotData data, string url, string query,
            string variables = "{}", [Variable] Dictionary<string, string> headers = null, int timeoutMs = 15000)
        {
            data.Logger.LogHeader();

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

            var payload = new
            {
                query,
                variables,
                operationName = ""
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, data.CancellationToken);

            var response = await client.PostAsync(url, content, linkedCts.Token).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

            data.Logger.Log($"GraphQL query with headers to {url}", LogColors.YellowGreen);
            data.Logger.Log($"Response: {responseBody}", LogColors.YellowGreen);

            return responseBody;
        }
    }
}
