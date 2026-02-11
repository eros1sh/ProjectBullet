using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Blocks.Functions.RandomFunctions
{
    [BlockCategory("Random Functions", "Blocks for generating random values", "#e066ff")]
    public static class Methods
    {
        [Block("Generates a random integer between min (inclusive) and max (exclusive)")]
        public static int RandomInt(BotData data, int min = 0, int max = 100)
        {
            var result = System.Random.Shared.Next(min, max);
            data.Logger.LogHeader();
            data.Logger.Log($"Random int [{min}, {max}): {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Generates a new GUID (UUID v4)")]
        public static string RandomGuid(BotData data)
        {
            var result = Guid.NewGuid().ToString();
            data.Logger.LogHeader();
            data.Logger.Log($"GUID: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Picks a random element from a list")]
        public static string RandomElement(BotData data, [Variable] List<string> list)
        {
            if (list.Count == 0)
            {
                data.Logger.LogHeader();
                data.Logger.Log("List is empty, returning empty string", LogColors.YellowGreen);
                return string.Empty;
            }

            var result = list[System.Random.Shared.Next(list.Count)];
            data.Logger.LogHeader();
            data.Logger.Log($"Random element: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Shuffles a list randomly and returns the shuffled list")]
        public static List<string> RandomShuffle(BotData data, [Variable] List<string> list)
        {
            var result = list.OrderBy(_ => System.Random.Shared.Next()).ToList();
            data.Logger.LogHeader();
            data.Logger.Log($"Shuffled list with {result.Count} elements", LogColors.YellowGreen);
            return result;
        }

        [Block("Generates a random boolean (true or false)")]
        public static bool RandomBool(BotData data)
        {
            var result = System.Random.Shared.Next(2) == 1;
            data.Logger.LogHeader();
            data.Logger.Log($"Random bool: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Generates random bytes of a specified length")]
        public static byte[] RandomBytes(BotData data, int length = 16)
        {
            var result = new byte[length];
            System.Random.Shared.NextBytes(result);
            data.Logger.LogHeader();
            data.Logger.Log($"Generated {length} random bytes", LogColors.YellowGreen);
            return result;
        }

        [Block("Generates a random hex string of a specified length")]
        public static string RandomHex(BotData data, int length = 32)
        {
            var bytes = new byte[length / 2 + 1];
            System.Random.Shared.NextBytes(bytes);
            var result = Convert.ToHexString(bytes)[..length].ToLower();
            data.Logger.LogHeader();
            data.Logger.Log($"Random hex: {result}", LogColors.YellowGreen);
            return result;
        }
    }
}
