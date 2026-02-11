using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Requests.Udp
{
    [BlockCategory("UDP", "Blocks for sending and receiving UDP datagrams", "#b19cd9", "#fff")]
    public static class Methods
    {
        [Block("Sends a UDP datagram with text content and optionally receives a response")]
        public static async Task<string> UdpSendRead(BotData data, string host, int port,
            string message, int timeoutMs = 5000, string encoding = "UTF8")
        {
            data.Logger.LogHeader();

            var enc = GetEncoding(encoding);
            var sendBytes = enc.GetBytes(message);

            using var client = new UdpClient();
            client.Client.ReceiveTimeout = timeoutMs;

            data.Logger.Log($"Sending {sendBytes.Length} bytes to {host}:{port}", LogColors.MediumPurple);
            await client.SendAsync(sendBytes, sendBytes.Length, host, port);

            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                var result = await client.ReceiveAsync(cts.Token);
                var response = enc.GetString(result.Buffer);
                data.Logger.Log($"Received {result.Buffer.Length} bytes: {response}", LogColors.MediumPurple);
                return response;
            }
            catch (OperationCanceledException)
            {
                data.Logger.Log("Receive timed out", LogColors.MediumPurple);
                return string.Empty;
            }
        }

        [Block("Sends a UDP datagram with raw bytes and optionally receives a response")]
        public static async Task<byte[]> UdpSendReadBytes(BotData data, string host, int port,
            byte[] messageBytes, int timeoutMs = 5000)
        {
            data.Logger.LogHeader();

            using var client = new UdpClient();
            client.Client.ReceiveTimeout = timeoutMs;

            data.Logger.Log($"Sending {messageBytes.Length} bytes to {host}:{port}", LogColors.MediumPurple);
            await client.SendAsync(messageBytes, messageBytes.Length, host, port);

            using var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                var result = await client.ReceiveAsync(cts.Token);
                data.Logger.Log($"Received {result.Buffer.Length} bytes", LogColors.MediumPurple);
                return result.Buffer;
            }
            catch (OperationCanceledException)
            {
                data.Logger.Log("Receive timed out", LogColors.MediumPurple);
                return Array.Empty<byte>();
            }
        }

        [Block("Sends a UDP datagram without waiting for a response")]
        public static async Task UdpSend(BotData data, string host, int port,
            string message, string encoding = "UTF8")
        {
            data.Logger.LogHeader();

            var enc = GetEncoding(encoding);
            var sendBytes = enc.GetBytes(message);

            using var client = new UdpClient();
            data.Logger.Log($"Sending {sendBytes.Length} bytes to {host}:{port}", LogColors.MediumPurple);
            await client.SendAsync(sendBytes, sendBytes.Length, host, port);
            data.Logger.Log("Sent (fire and forget)", LogColors.MediumPurple);
        }

        [Block("Sends raw bytes via UDP without waiting for a response")]
        public static async Task UdpSendBytes(BotData data, string host, int port, byte[] messageBytes)
        {
            data.Logger.LogHeader();

            using var client = new UdpClient();
            data.Logger.Log($"Sending {messageBytes.Length} bytes to {host}:{port}", LogColors.MediumPurple);
            await client.SendAsync(messageBytes, messageBytes.Length, host, port);
            data.Logger.Log("Sent (fire and forget)", LogColors.MediumPurple);
        }

        private static Encoding GetEncoding(string name) => name.ToUpperInvariant() switch
        {
            "UTF8" or "UTF-8" => Encoding.UTF8,
            "ASCII" => Encoding.ASCII,
            "UNICODE" or "UTF16" or "UTF-16" => Encoding.Unicode,
            "UTF32" or "UTF-32" => Encoding.UTF32,
            _ => Encoding.UTF8
        };
    }
}
