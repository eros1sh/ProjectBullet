using MQTTnet;
using MQTTnet.Client;
using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Requests.Mqtt
{
    [BlockCategory("MQTT", "Blocks for MQTT protocol communication", "#00897b", "#fff")]
    public static class Methods
    {
        [Block("Connects to an MQTT broker with the specified parameters")]
        public static async Task MqttConnect(BotData data, string server, int port = 1883,
            string clientId = "", string username = "", string password = "",
            bool useTls = false, int timeoutMs = 10000)
        {
            data.Logger.LogHeader();

            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(server, port)
                .WithTimeout(TimeSpan.FromMilliseconds(timeoutMs));

            if (!string.IsNullOrEmpty(clientId))
            {
                optionsBuilder.WithClientId(clientId);
            }

            if (!string.IsNullOrEmpty(username))
            {
                optionsBuilder.WithCredentials(username, password);
            }

            if (useTls)
            {
                optionsBuilder.WithTlsOptions(o => o.UseTls());
            }

            var options = optionsBuilder.Build();

            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, data.CancellationToken);

            await client.ConnectAsync(options, linkedCts.Token).ConfigureAwait(false);

            data.SetObject("mqttClient", client);

            data.Logger.Log($"Connected to MQTT broker {server}:{port} (TLS: {useTls})", LogColors.YellowGreen);
        }

        [Block("Publishes a message to an MQTT topic")]
        public static async Task MqttPublish(BotData data, string topic, string payload, int qos = 0)
        {
            data.Logger.LogHeader();

            var client = data.TryGetObject<IMqttClient>("mqttClient")
                ?? throw new InvalidOperationException("No MQTT client connected. Call MqttConnect first.");

            var qualityOfService = qos switch
            {
                0 => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
                1 => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                2 => MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                _ => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
            };

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qualityOfService)
                .Build();

            await client.PublishAsync(message, data.CancellationToken).ConfigureAwait(false);

            data.Logger.Log($"Published to topic '{topic}': {payload}", LogColors.YellowGreen);
        }

        [Block("Subscribes to an MQTT topic and waits for a single message")]
        public static async Task<string> MqttSubscribeAndReceive(BotData data, string topic, int qos = 0, int timeoutMs = 10000)
        {
            data.Logger.LogHeader();

            var client = data.TryGetObject<IMqttClient>("mqttClient")
                ?? throw new InvalidOperationException("No MQTT client connected. Call MqttConnect first.");

            var qualityOfService = qos switch
            {
                0 => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
                1 => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
                2 => MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce,
                _ => MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
            };

            var tcs = new TaskCompletionSource<string>();

            client.ApplicationMessageReceivedAsync += e =>
            {
                var receivedPayload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                tcs.TrySetResult(receivedPayload);
                return Task.CompletedTask;
            };

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(qualityOfService))
                .Build();

            await client.SubscribeAsync(subscribeOptions, data.CancellationToken).ConfigureAwait(false);

            data.Logger.Log($"Subscribed to topic '{topic}', waiting for message...", LogColors.YellowGreen);

            using var cts = new CancellationTokenSource(timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, data.CancellationToken);

            await using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
            {
                var result = await tcs.Task.ConfigureAwait(false);
                data.Logger.Log($"Received message from '{topic}': {result}", LogColors.YellowGreen);
                return result;
            }
        }

        [Block("Disconnects from the MQTT broker")]
        public static async Task MqttDisconnect(BotData data)
        {
            data.Logger.LogHeader();

            var client = data.TryGetObject<IMqttClient>("mqttClient");

            if (client != null && client.IsConnected)
            {
                await client.DisconnectAsync(new MqttClientDisconnectOptionsBuilder()
                    .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                    .Build(), data.CancellationToken).ConfigureAwait(false);

                data.Logger.Log("Disconnected from MQTT broker", LogColors.YellowGreen);
            }
            else
            {
                data.Logger.Log("MQTT client was not connected", LogColors.YellowGreen);
            }
        }
    }
}
