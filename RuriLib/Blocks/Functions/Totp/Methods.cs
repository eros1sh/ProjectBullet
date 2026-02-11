using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.Security.Cryptography;

namespace RuriLib.Blocks.Functions.TotpFunctions
{
    [BlockCategory("TOTP/HOTP", "Blocks for 2FA code generation", "#ff7043")]
    public static class Methods
    {
        [Block("Generates a TOTP code from a Base32 secret (RFC 6238)")]
        public static string TotpGenerate(BotData data, string secret, int digits = 6, int period = 30)
        {
            var key = Base32Decode(secret);
            var timeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / period;
            var code = ComputeHotp(key, timeStep, digits);

            data.Logger.LogHeader();
            data.Logger.Log($"Generated TOTP code: {code}", LogColors.YellowGreen);

            return code;
        }

        [Block("Generates an HOTP code from a Base32 secret and counter (RFC 4226)")]
        public static string HotpGenerate(BotData data, string secret, int counter, int digits = 6)
        {
            var key = Base32Decode(secret);
            var code = ComputeHotp(key, counter, digits);

            data.Logger.LogHeader();
            data.Logger.Log($"Generated HOTP code for counter {counter}: {code}", LogColors.YellowGreen);

            return code;
        }

        [Block("Validates a TOTP code against the current time with a configurable window")]
        public static bool TotpValidate(BotData data, string secret, string code, int digits = 6,
            int period = 30, int window = 1)
        {
            var key = Base32Decode(secret);
            var currentTimeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / period;
            var isValid = false;

            for (var i = -window; i <= window; i++)
            {
                var candidate = ComputeHotp(key, currentTimeStep + i, digits);
                if (candidate == code)
                {
                    isValid = true;
                    break;
                }
            }

            data.Logger.LogHeader();
            data.Logger.Log($"TOTP validation result: {isValid}", LogColors.YellowGreen);

            return isValid;
        }

        private static string ComputeHotp(byte[] key, long counter, int digits)
        {
            // Convert counter to big-endian 8-byte array
            var counterBytes = new byte[8];
            for (var i = 7; i >= 0; i--)
            {
                counterBytes[i] = (byte)(counter & 0xff);
                counter >>= 8;
            }

            // HMAC-SHA1
            using var hmac = new HMACSHA1(key);
            var hash = hmac.ComputeHash(counterBytes);

            // Dynamic truncation
            var offset = hash[hash.Length - 1] & 0x0f;
            var binaryCode =
                ((hash[offset] & 0x7f) << 24) |
                ((hash[offset + 1] & 0xff) << 16) |
                ((hash[offset + 2] & 0xff) << 8) |
                (hash[offset + 3] & 0xff);

            var mod = (int)Math.Pow(10, digits);
            var otp = binaryCode % mod;

            return otp.ToString().PadLeft(digits, '0');
        }

        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            input = input.TrimEnd('=').ToUpperInvariant();
            var output = new byte[input.Length * 5 / 8];
            int bitIndex = 0, inputIndex = 0, outputBits = 0, outputIndex = 0;
            while (inputIndex < input.Length)
            {
                int byteIndex = alphabet.IndexOf(input[inputIndex]);
                if (byteIndex < 0) { inputIndex++; continue; }
                outputBits = (outputBits << 5) | byteIndex;
                bitIndex += 5;
                if (bitIndex >= 8)
                {
                    output[outputIndex++] = (byte)(outputBits >> (bitIndex - 8));
                    bitIndex -= 8;
                }
                inputIndex++;
            }
            return output;
        }
    }
}
