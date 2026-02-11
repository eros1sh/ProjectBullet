using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Requests.Grpc
{
    [BlockCategory("gRPC", "Blocks for gRPC protocol communication", "#4285f4")]
    public static class Methods
    {
        [Block("Makes a gRPC unary call with raw bytes and returns the response bytes as base64")]
        public static async Task<string> GrpcUnaryCall(BotData data, string url, string servicePath,
            string requestBase64, int timeoutMs = 15000)
        {
            data.Logger.LogHeader();

            var requestBytes = Convert.FromBase64String(requestBase64);

            // gRPC framing: [compressed flag (1 byte)] + [message length (4 bytes big-endian)] + [message]
            var framedData = new byte[requestBytes.Length + 5];
            framedData[0] = 0; // not compressed
            var lengthBytes = BitConverter.GetBytes(requestBytes.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
            Buffer.BlockCopy(lengthBytes, 0, framedData, 1, 4);
            Buffer.BlockCopy(requestBytes, 0, framedData, 5, requestBytes.Length);

            using var handler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };
            using var client = new HttpClient(handler);

            var fullUrl = url.TrimEnd('/') + "/" + servicePath.TrimStart('/');
            var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
            {
                Version = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                Content = new ByteArrayContent(framedData)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
            request.Headers.Add("TE", "trailers");

            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, data.CancellationToken);

            data.Logger.Log($"gRPC call to {fullUrl}", LogColors.YellowGreen);

            var response = await client.SendAsync(request, linkedCts.Token).ConfigureAwait(false);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(linkedCts.Token).ConfigureAwait(false);

            // Strip gRPC framing (5 byte header)
            string resultBase64;
            if (responseBytes.Length > 5)
            {
                var messageBytes = new byte[responseBytes.Length - 5];
                Buffer.BlockCopy(responseBytes, 5, messageBytes, 0, messageBytes.Length);
                resultBase64 = Convert.ToBase64String(messageBytes);
            }
            else
            {
                resultBase64 = string.Empty;
            }

            var grpcStatus = response.TrailingHeaders?.GetValues("grpc-status")?.FirstOrDefault() ?? "unknown";
            data.Logger.Log($"gRPC response status: {grpcStatus}, payload size: {responseBytes.Length} bytes", LogColors.YellowGreen);

            return resultBase64;
        }

        [Block("Makes a gRPC unary call with JSON payload (for grpc-json transcoding)")]
        public static async Task<string> GrpcJsonCall(BotData data, string url, string servicePath,
            string jsonPayload, int timeoutMs = 15000)
        {
            data.Logger.LogHeader();

            using var handler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };
            using var client = new HttpClient(handler);

            var fullUrl = url.TrimEnd('/') + "/" + servicePath.TrimStart('/');
            var request = new HttpRequestMessage(HttpMethod.Post, fullUrl)
            {
                Version = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, data.CancellationToken);

            data.Logger.Log($"gRPC-JSON call to {fullUrl}", LogColors.YellowGreen);

            var response = await client.SendAsync(request, linkedCts.Token).ConfigureAwait(false);
            var result = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);

            data.Logger.Log($"gRPC-JSON response ({(int)response.StatusCode}):", LogColors.YellowGreen);
            data.Logger.Log(result, LogColors.YellowGreen);

            return result;
        }
    }
}
