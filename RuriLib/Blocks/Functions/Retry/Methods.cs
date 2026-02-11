using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Functions.RetryFunctions
{
    [BlockCategory("Retry & Loop", "Blocks for retry logic and loop control", "#ffa726")]
    public static class Methods
    {
        [Block("Configures retry parameters by storing max retries and delay into BotData")]
        public static int RetryOnException(BotData data, int maxRetries = 3, int delayMs = 1000)
        {
            data.Logger.LogHeader();

            data.SetObject("retryMaxRetries", maxRetries, disposeExisting: false);
            data.SetObject("retryDelayMs", delayMs, disposeExisting: false);
            data.SetObject("retryCounter", 0, disposeExisting: false);

            data.Logger.Log($"Retry configured: max {maxRetries}, delay {delayMs}ms", LogColors.YellowGreen);
            return maxRetries;
        }

        [Block("Checks if a retry should be attempted based on condition and retry counter")]
        public static bool ShouldRetry(BotData data, bool condition)
        {
            data.Logger.LogHeader();

            if (!condition)
            {
                data.Logger.Log("Condition is false, no retry needed", LogColors.YellowGreen);
                return false;
            }

            var maxRetries = data.TryGetObject<object>("retryMaxRetries");
            var counterObj = data.TryGetObject<object>("retryCounter");

            int max = maxRetries != null ? Convert.ToInt32(maxRetries) : 3;
            int counter = counterObj != null ? Convert.ToInt32(counterObj) : 0;

            if (counter < max)
            {
                counter++;
                data.SetObject("retryCounter", counter, disposeExisting: false);
                data.Logger.Log($"Retry {counter}/{max} - should retry: true", LogColors.YellowGreen);
                return true;
            }

            data.Logger.Log($"Retry {counter}/{max} - max retries reached, should retry: false", LogColors.YellowGreen);
            return false;
        }

        [Block("Resets the retry counter to zero")]
        public static void ResetRetryCounter(BotData data)
        {
            data.Logger.LogHeader();

            data.SetObject("retryCounter", 0, disposeExisting: false);

            data.Logger.Log("Retry counter reset to 0", LogColors.YellowGreen);
        }

        [Block("Increments a loop iteration counter and returns whether the loop should continue")]
        public static bool LoopWhile(BotData data, bool condition, int maxIterations = 1000)
        {
            data.Logger.LogHeader();

            var iterObj = data.TryGetObject<object>("loopIterationCounter");
            int iterations = iterObj != null ? Convert.ToInt32(iterObj) : 0;

            iterations++;
            data.SetObject("loopIterationCounter", iterations, disposeExisting: false);

            if (condition && iterations < maxIterations)
            {
                data.Logger.Log($"Loop iteration {iterations}/{maxIterations} - continuing", LogColors.YellowGreen);
                return true;
            }

            if (!condition)
            {
                data.Logger.Log($"Loop iteration {iterations} - condition is false, stopping", LogColors.YellowGreen);
            }
            else
            {
                data.Logger.Log($"Loop iteration {iterations}/{maxIterations} - max iterations reached, stopping", LogColors.YellowGreen);
            }

            return false;
        }

        [Block("Resets the loop iteration counter to zero")]
        public static void LoopReset(BotData data)
        {
            data.Logger.LogHeader();

            data.SetObject("loopIterationCounter", 0, disposeExisting: false);

            data.Logger.Log("Loop iteration counter reset to 0", LogColors.YellowGreen);
        }

        [Block("Delays for a random duration between min and max milliseconds")]
        public static async Task DelayBetweenRetries(BotData data, int minMs = 500, int maxMs = 2000)
        {
            data.Logger.LogHeader();

            var delay = data.Random.Next(minMs, maxMs + 1);
            data.Logger.Log($"Delaying for {delay}ms (range {minMs}-{maxMs}ms)", LogColors.YellowGreen);

            await Task.Delay(delay, data.CancellationToken).ConfigureAwait(false);
        }
    }
}
