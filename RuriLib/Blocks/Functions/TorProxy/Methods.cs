using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using RuriLib.Models.Proxies;
using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Functions.TorProxyFunctions
{
    [BlockCategory("TOR Proxy", "Blocks for TOR network integration", "#7e57c2")]
    public static class Methods
    {
        [Block("Sets a SOCKS5 proxy pointing to the local TOR instance")]
        public static string TorSetProxy(BotData data, string torHost = "127.0.0.1", int torPort = 9050)
        {
            data.Logger.LogHeader();

            data.Proxy = new Proxy(torHost, torPort, ProxyType.Socks5);
            data.UseProxy = true;

            var proxyString = $"socks5://{torHost}:{torPort}";
            data.Logger.Log($"TOR proxy set to {proxyString}", LogColors.YellowGreen);
            return proxyString;
        }

        [Block("Requests a new TOR circuit by sending SIGNAL NEWNYM to the control port")]
        public static async Task<bool> TorNewCircuit(BotData data, string controlHost = "127.0.0.1",
            int controlPort = 9051, string controlPassword = "")
        {
            data.Logger.LogHeader();

            try
            {
                using var tcpClient = new TcpClient();

                using var cts = new CancellationTokenSource(10000);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, data.CancellationToken);

                await tcpClient.ConnectAsync(controlHost, controlPort, linkedCts.Token).ConfigureAwait(false);

                using var stream = tcpClient.GetStream();
                stream.ReadTimeout = 10000;
                stream.WriteTimeout = 10000;

                // Authenticate
                var authCommand = string.IsNullOrEmpty(controlPassword)
                    ? "AUTHENTICATE\r\n"
                    : $"AUTHENTICATE \"{controlPassword}\"\r\n";

                var authBytes = Encoding.ASCII.GetBytes(authCommand);
                await stream.WriteAsync(authBytes.AsMemory(0, authBytes.Length), linkedCts.Token).ConfigureAwait(false);

                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), linkedCts.Token).ConfigureAwait(false);
                var authResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                if (!authResponse.StartsWith("250"))
                {
                    data.Logger.Log($"TOR authentication failed: {authResponse}", LogColors.YellowGreen);
                    return false;
                }

                // Send SIGNAL NEWNYM
                var signalBytes = Encoding.ASCII.GetBytes("SIGNAL NEWNYM\r\n");
                await stream.WriteAsync(signalBytes.AsMemory(0, signalBytes.Length), linkedCts.Token).ConfigureAwait(false);

                bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), linkedCts.Token).ConfigureAwait(false);
                var signalResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                var success = signalResponse.StartsWith("250");
                data.Logger.Log(success
                    ? "TOR new circuit requested successfully"
                    : $"TOR new circuit failed: {signalResponse}", LogColors.YellowGreen);

                return success;
            }
            catch (Exception ex)
            {
                data.Logger.Log($"TOR control connection failed: {ex.Message}", LogColors.YellowGreen);
                return false;
            }
        }

        [Block("Checks the TOR connection by querying the TOR Project API for the current IP")]
        public static async Task<string> TorCheckConnection(BotData data, int timeoutMs = 10000)
        {
            data.Logger.LogHeader();

            try
            {
                var handler = new HttpClientHandler();

                if (data.UseProxy && data.Proxy != null)
                {
                    var webProxy = new System.Net.WebProxy(data.Proxy.Host, data.Proxy.Port);
                    if (data.Proxy.NeedsAuthentication)
                    {
                        webProxy.Credentials = new System.Net.NetworkCredential(data.Proxy.Username, data.Proxy.Password);
                    }
                    handler.Proxy = webProxy;
                    handler.UseProxy = true;
                }

                using var httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMilliseconds(timeoutMs)
                };

                var response = await httpClient.GetStringAsync("https://check.torproject.org/api/ip").ConfigureAwait(false);

                data.Logger.Log($"TOR check response: {response}", LogColors.YellowGreen);
                return response;
            }
            catch (Exception ex)
            {
                data.Logger.Log($"TOR connection check failed: {ex.Message}", LogColors.YellowGreen);
                return $"ERROR: {ex.Message}";
            }
        }
    }
}
