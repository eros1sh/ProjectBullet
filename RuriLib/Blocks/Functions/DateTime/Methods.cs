using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Globalization;

namespace RuriLib.Blocks.Functions.DateTimeFunctions
{
    [BlockCategory("DateTime Functions", "Blocks for working with dates and times", "#ff9966")]
    public static class Methods
    {
        [Block("Gets the current date and time as a formatted string")]
        public static string DateTimeNow(BotData data, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var result = System.DateTime.Now.ToString(format);
            data.Logger.LogHeader();
            data.Logger.Log($"Current datetime: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Gets the current UTC date and time as a formatted string")]
        public static string DateTimeUtcNow(BotData data, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var result = System.DateTime.UtcNow.ToString(format);
            data.Logger.LogHeader();
            data.Logger.Log($"Current UTC datetime: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Gets the current Unix timestamp in seconds")]
        public static int UnixTimeNow(BotData data)
        {
            var result = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data.Logger.LogHeader();
            data.Logger.Log($"Unix timestamp: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Gets the current Unix timestamp in milliseconds as a string")]
        public static string UnixTimeMillisNow(BotData data)
        {
            var result = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            data.Logger.LogHeader();
            data.Logger.Log($"Unix timestamp (ms): {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Converts a Unix timestamp (seconds) to a formatted datetime string")]
        public static string UnixToDateTime(BotData data, int unixTimestamp, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var dt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).LocalDateTime;
            var result = dt.ToString(format);
            data.Logger.LogHeader();
            data.Logger.Log($"Converted {unixTimestamp} to {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Converts a datetime string to a Unix timestamp in seconds")]
        public static int DateTimeToUnix(BotData data, [Variable] string dateTimeString, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var dt = System.DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture);
            var result = (int)new DateTimeOffset(dt).ToUnixTimeSeconds();
            data.Logger.LogHeader();
            data.Logger.Log($"Converted {dateTimeString} to {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Parses a datetime string and reformats it to a different format")]
        public static string DateTimeReformat(BotData data, [Variable] string dateTimeString,
            string inputFormat = "yyyy-MM-dd", string outputFormat = "dd/MM/yyyy")
        {
            var dt = System.DateTime.ParseExact(dateTimeString, inputFormat, CultureInfo.InvariantCulture);
            var result = dt.ToString(outputFormat);
            data.Logger.LogHeader();
            data.Logger.Log($"Reformatted: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Adds days to a datetime string and returns the result")]
        public static string DateTimeAddDays(BotData data, [Variable] string dateTimeString,
            int days, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var dt = System.DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture);
            var result = dt.AddDays(days).ToString(format);
            data.Logger.LogHeader();
            data.Logger.Log($"Added {days} days: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Adds hours to a datetime string and returns the result")]
        public static string DateTimeAddHours(BotData data, [Variable] string dateTimeString,
            int hours, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var dt = System.DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture);
            var result = dt.AddHours(hours).ToString(format);
            data.Logger.LogHeader();
            data.Logger.Log($"Added {hours} hours: {result}", LogColors.YellowGreen);
            return result;
        }

        [Block("Calculates the difference in days between two datetime strings")]
        public static int DateTimeDiffDays(BotData data, [Variable] string dateTime1, [Variable] string dateTime2,
            string format = "yyyy-MM-dd HH:mm:ss")
        {
            var dt1 = System.DateTime.ParseExact(dateTime1, format, CultureInfo.InvariantCulture);
            var dt2 = System.DateTime.ParseExact(dateTime2, format, CultureInfo.InvariantCulture);
            var result = (int)(dt2 - dt1).TotalDays;
            data.Logger.LogHeader();
            data.Logger.Log($"Difference: {result} days", LogColors.YellowGreen);
            return result;
        }

        [Block("Gets the ISO 8601 representation of the current UTC time")]
        public static string DateTimeIso8601(BotData data)
        {
            var result = System.DateTime.UtcNow.ToString("o");
            data.Logger.LogHeader();
            data.Logger.Log($"ISO 8601: {result}", LogColors.YellowGreen);
            return result;
        }
    }
}
