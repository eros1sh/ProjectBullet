using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Blocks.Functions.Dictionary
{
    [BlockCategory("Dictionary Functions", "Blocks for working with dictionaries", "#9acd32")]
    public static class Methods
    {
        [Block("Adds an item to the dictionary")]
        public static void AddKeyValuePair(BotData data, [Variable] Dictionary<string, string> dictionary, string key, string value)
        {
            dictionary.Add(key, value);
            data.Logger.LogHeader();
            data.Logger.Log($"Added ({key}, {value})", LogColors.YellowGreen);
        }

        [Block("Removes an item with a given key from the dictionary")]
        public static void RemoveByKey(BotData data, [Variable] Dictionary<string, string> dictionary, string key)
        {
            data.Logger.LogHeader();

            if (dictionary.Remove(key))
                data.Logger.Log($"Removed the item with key {key}", LogColors.YellowGreen);
            else
                data.Logger.Log($"Could not find an item with key {key}", LogColors.YellowGreen);
        }

        [Block("Gets a dictionary key by value (old <DICT{value}>)")]
        public static string GetKey(BotData data, [Variable] Dictionary<string, string> dictionary, string value)
        {
            var key = dictionary.FirstOrDefault(kvp => kvp.Value == value).Key;
            data.Logger.LogHeader();
            data.Logger.Log($"Got key: {key}", LogColors.YellowGreen);
            return key;
        }

        [Block("Gets a value from a dictionary by key, returns empty string if not found")]
        public static string DictGet(BotData data, [Variable] Dictionary<string, string> dict, string key)
        {
            var result = dict.TryGetValue(key, out var val) ? val : string.Empty;
            data.Logger.LogHeader();
            data.Logger.Log($"Dict[{key}] = {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Sets a value in a dictionary")]
        public static void DictSet(BotData data, [Variable] Dictionary<string, string> dict, string key, string value)
        {
            dict[key] = value;
            data.Logger.LogHeader();
            data.Logger.Log($"Set Dict[{key}] = {value}", LogColors.YellowGreen);
        }

        [Block("Checks if a dictionary contains a specific key")]
        public static bool DictContainsKey(BotData data, [Variable] Dictionary<string, string> dict, string key)
        {
            var result = dict.ContainsKey(key);
            data.Logger.LogHeader();
            data.Logger.Log($"Contains key '{key}': {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Checks if a dictionary contains a specific value")]
        public static bool DictContainsValue(BotData data, [Variable] Dictionary<string, string> dict, string value)
        {
            var result = dict.ContainsValue(value);
            data.Logger.LogHeader();
            data.Logger.Log($"Contains value '{value}': {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Gets all keys from a dictionary as a list")]
        public static List<string> DictKeys(BotData data, [Variable] Dictionary<string, string> dict)
        {
            var result = dict.Keys.ToList();
            data.Logger.LogHeader();
            data.Logger.Log($"Got {result.Count} keys", LogColors.YellowGreen);
            return result;
        }

        [Block("Gets all values from a dictionary as a list")]
        public static List<string> DictValues(BotData data, [Variable] Dictionary<string, string> dict)
        {
            var result = dict.Values.ToList();
            data.Logger.LogHeader();
            data.Logger.Log($"Got {result.Count} values", LogColors.YellowGreen);
            return result;
        }

        [Block("Gets the number of entries in a dictionary")]
        public static int DictCount(BotData data, [Variable] Dictionary<string, string> dict)
        {
            var result = dict.Count;
            data.Logger.LogHeader();
            data.Logger.Log($"Dictionary count: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Merges two dictionaries (second overwrites first on conflicts)")]
        public static Dictionary<string, string> DictMerge(BotData data, [Variable] Dictionary<string, string> dict1, [Variable] Dictionary<string, string> dict2)
        {
            var result = new Dictionary<string, string>(dict1);
            foreach (var kvp in dict2)
                result[kvp.Key] = kvp.Value;
            data.Logger.LogHeader();
            data.Logger.Log($"Merged dictionaries: {dict1.Count} + {dict2.Count} = {result.Count}", LogColors.YellowGreen);
            return result;
        }

        [Block("Creates a new empty dictionary")]
        public static Dictionary<string, string> DictCreate(BotData data)
        {
            var result = new Dictionary<string, string>();
            data.Logger.LogHeader();
            data.Logger.Log("Created empty dictionary", LogColors.YellowGreen);
            return result;
        }
    }
}
