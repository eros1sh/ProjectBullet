using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Blocks.Functions.Json
{
    [BlockCategory("JSON", "Blocks for advanced JSON operations", "#f59e0b")]
    public static class Methods
    {
        [Block("Queries a JSON string with a JSONPath expression and returns the first match")]
        public static string JsonPathQuery(BotData data, [Variable] string json, string jsonPath)
        {
            var token = JToken.Parse(json);
            var result = token.SelectToken(jsonPath)?.ToString() ?? string.Empty;

            data.Logger.LogHeader();
            data.Logger.Log($"JSONPath query '{jsonPath}' result: {result}", LogColors.Yellow);
            return result;
        }

        [Block("Queries a JSON string with a JSONPath expression and returns all matches")]
        public static List<string> JsonPathQueryAll(BotData data, [Variable] string json, string jsonPath)
        {
            var token = JToken.Parse(json);
            var results = token.SelectTokens(jsonPath).Select(t => t.ToString()).ToList();

            data.Logger.LogHeader();
            data.Logger.Log($"JSONPath query '{jsonPath}' returned {results.Count} results:", LogColors.Yellow);
            data.Logger.Log(results, LogColors.Yellow);
            return results;
        }

        [Block("Flattens a JSON object into a dictionary with dot-notation keys")]
        public static Dictionary<string, string> JsonFlatten(BotData data, [Variable] string json)
        {
            var token = JToken.Parse(json);
            var result = new Dictionary<string, string>();
            FlattenToken(token, string.Empty, result);

            data.Logger.LogHeader();
            data.Logger.Log($"Flattened JSON into {result.Count} keys:", LogColors.Yellow);
            foreach (var kvp in result)
            {
                data.Logger.Log($"  {kvp.Key} = {kvp.Value}", LogColors.Yellow);
            }
            return result;
        }

        [Block("Counts elements in a JSON array or properties in a JSON object at the given path")]
        public static int JsonCount(BotData data, [Variable] string json, string jsonPath = "$")
        {
            var token = JToken.Parse(json);
            var selected = token.SelectToken(jsonPath);
            var count = selected switch
            {
                JArray arr => arr.Count,
                JObject obj => obj.Properties().Count(),
                _ => selected != null ? 1 : 0
            };

            data.Logger.LogHeader();
            data.Logger.Log($"JSON count at '{jsonPath}': {count}", LogColors.Yellow);
            return count;
        }

        [Block("Gets all property names/keys from a JSON object at the given path")]
        public static List<string> JsonGetKeys(BotData data, [Variable] string json, string jsonPath = "$")
        {
            var token = JToken.Parse(json);
            var selected = token.SelectToken(jsonPath);
            var keys = new List<string>();

            if (selected is JObject obj)
            {
                keys = obj.Properties().Select(p => p.Name).ToList();
            }

            data.Logger.LogHeader();
            data.Logger.Log($"JSON keys at '{jsonPath}': {keys.Count} found", LogColors.Yellow);
            data.Logger.Log(keys, LogColors.Yellow);
            return keys;
        }

        [Block("Gets the type of the JSON value at the given path")]
        public static string JsonGetType(BotData data, [Variable] string json, string jsonPath = "$")
        {
            var token = JToken.Parse(json);
            var selected = token.SelectToken(jsonPath);
            var typeName = selected?.Type switch
            {
                JTokenType.Object => "object",
                JTokenType.Array => "array",
                JTokenType.String => "string",
                JTokenType.Integer => "number",
                JTokenType.Float => "number",
                JTokenType.Boolean => "boolean",
                JTokenType.Null => "null",
                _ => "unknown"
            };

            data.Logger.LogHeader();
            data.Logger.Log($"JSON type at '{jsonPath}': {typeName}", LogColors.Yellow);
            return typeName;
        }

        [Block("Beautifies or minifies a JSON string")]
        public static string JsonBeautify(BotData data, [Variable] string json, bool minify = false)
        {
            var token = JToken.Parse(json);
            var formatting = minify ? Formatting.None : Formatting.Indented;
            var result = token.ToString(formatting);

            data.Logger.LogHeader();
            data.Logger.Log(minify ? "Minified JSON" : "Beautified JSON", LogColors.Yellow);
            return result;
        }

        private static void FlattenToken(JToken token, string prefix, Dictionary<string, string> result)
        {
            switch (token)
            {
                case JObject obj:
                    foreach (var property in obj.Properties())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        FlattenToken(property.Value, key, result);
                    }
                    break;

                case JArray arr:
                    for (var i = 0; i < arr.Count; i++)
                    {
                        var key = $"{prefix}[{i}]";
                        FlattenToken(arr[i], key, result);
                    }
                    break;

                default:
                    result[prefix] = token.ToString();
                    break;
            }
        }
    }
}
