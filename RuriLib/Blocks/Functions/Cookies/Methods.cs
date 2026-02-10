using RuriLib.Attributes;
using RuriLib.Functions.Files;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RuriLib.Blocks.Functions.Cookies
{
    [BlockCategory("Cookies", "Blocks for managing HTTP cookies", "#8B4513")]
    public static class Methods
    {
        [Block("Saves all cookies to a file in JSON format")]
        public static async Task SaveCookies(BotData data, string path,
            RuriLib.Blocks.Utility.Files.FileEncoding encoding = RuriLib.Blocks.Utility.Files.FileEncoding.UTF8)
        {
            if (data.Providers.Security.RestrictBlocksToCWD)
                FileUtils.ThrowIfNotInCWD(path);

            FileUtils.CreatePath(path);

            var json = JsonSerializer.Serialize(data.COOKIES, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json, MapEncoding(encoding), data.CancellationToken).ConfigureAwait(false);

            data.Logger.LogHeader();
            data.Logger.Log($"Saved {data.COOKIES.Count} cookies to {path}", LogColors.Flavescent);
        }

        [Block("Loads cookies from a JSON file")]
        public static async Task LoadCookies(BotData data, string path,
            RuriLib.Blocks.Utility.Files.FileEncoding encoding = RuriLib.Blocks.Utility.Files.FileEncoding.UTF8)
        {
            if (data.Providers.Security.RestrictBlocksToCWD)
                FileUtils.ThrowIfNotInCWD(path);

            var json = await File.ReadAllTextAsync(path, MapEncoding(encoding), data.CancellationToken).ConfigureAwait(false);
            var cookies = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (cookies != null)
            {
                foreach (var kvp in cookies)
                {
                    data.COOKIES[kvp.Key] = kvp.Value;
                }
            }

            data.Logger.LogHeader();
            data.Logger.Log($"Loaded cookies from {path}", LogColors.Flavescent);
        }

        [Block("Clears all cookies")]
        public static void ClearAllCookies(BotData data)
        {
            data.COOKIES.Clear();

            data.Logger.LogHeader();
            data.Logger.Log("Cleared all cookies", LogColors.Flavescent);
        }

        [Block("Gets the value of a specific cookie")]
        public static string GetCookie(BotData data, string name, string defaultValue = "")
        {
            var value = data.COOKIES.TryGetValue(name, out var v) ? v : defaultValue;

            data.Logger.LogHeader();
            data.Logger.Log($"Cookie '{name}' = '{value}'", LogColors.Flavescent);
            return value;
        }

        [Block("Sets a specific cookie")]
        public static void SetCookie(BotData data, string name, string value)
        {
            data.COOKIES[name] = value;

            data.Logger.LogHeader();
            data.Logger.Log($"Set cookie '{name}' = '{value}'", LogColors.Flavescent);
        }

        [Block("Deletes a specific cookie")]
        public static void DeleteCookie(BotData data, string name)
        {
            data.COOKIES.Remove(name);

            data.Logger.LogHeader();
            data.Logger.Log($"Deleted cookie '{name}'", LogColors.Flavescent);
        }

        [Block("Gets the number of cookies")]
        public static int CookieCount(BotData data)
        {
            var count = data.COOKIES.Count;

            data.Logger.LogHeader();
            data.Logger.Log($"Cookie count: {count}", LogColors.Flavescent);
            return count;
        }

        [Block("Exports cookies in Netscape/Mozilla format")]
        public static async Task ExportCookiesNetscape(BotData data, string path, string domain = ".example.com")
        {
            if (data.Providers.Security.RestrictBlocksToCWD)
                FileUtils.ThrowIfNotInCWD(path);

            FileUtils.CreatePath(path);

            var sb = new StringBuilder();
            sb.AppendLine("# Netscape HTTP Cookie File");
            foreach (var cookie in data.COOKIES)
            {
                // domain \t TRUE \t / \t FALSE \t 0 \t name \t value
                sb.AppendLine($"{domain}\tTRUE\t/\tFALSE\t0\t{cookie.Key}\t{cookie.Value}");
            }

            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, data.CancellationToken).ConfigureAwait(false);

            data.Logger.LogHeader();
            data.Logger.Log($"Exported {data.COOKIES.Count} cookies in Netscape format to {path}", LogColors.Flavescent);
        }

        [Block("Imports cookies from a Netscape/Mozilla format file")]
        public static async Task ImportCookiesNetscape(BotData data, string path)
        {
            if (data.Providers.Security.RestrictBlocksToCWD)
                FileUtils.ThrowIfNotInCWD(path);

            var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, data.CancellationToken).ConfigureAwait(false);
            var imported = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split('\t');
                if (parts.Length >= 7)
                {
                    data.COOKIES[parts[5]] = parts[6];
                    imported++;
                }
            }

            data.Logger.LogHeader();
            data.Logger.Log($"Imported {imported} cookies from Netscape format file {path}", LogColors.Flavescent);
        }

        private static Encoding MapEncoding(RuriLib.Blocks.Utility.Files.FileEncoding encoding)
            => encoding switch
            {
                RuriLib.Blocks.Utility.Files.FileEncoding.UTF8 => Encoding.UTF8,
                RuriLib.Blocks.Utility.Files.FileEncoding.ASCII => Encoding.ASCII,
                RuriLib.Blocks.Utility.Files.FileEncoding.Unicode => Encoding.Unicode,
                RuriLib.Blocks.Utility.Files.FileEncoding.BigEndianUnicode => Encoding.BigEndianUnicode,
                RuriLib.Blocks.Utility.Files.FileEncoding.UTF32 => Encoding.UTF32,
                RuriLib.Blocks.Utility.Files.FileEncoding.Latin1 => Encoding.Latin1,
                _ => Encoding.UTF8
            };
    }
}
