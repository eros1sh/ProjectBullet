using QRCoder;
using RuriLib.Attributes;
using RuriLib.Logging;
using RuriLib.Models.Bots;
using System;
using System.IO;

namespace RuriLib.Blocks.Functions.QrCodeFunctions
{
    [BlockCategory("QR Code", "Blocks for QR code generation", "#7c4dff")]
    public static class Methods
    {
        [Block("Generates a QR code as PNG image bytes from the given content")]
        public static byte[] QrCodeGenerate(BotData data, string content, int pixelsPerModule = 10)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var pngBytes = qrCode.GetGraphic(pixelsPerModule);

            data.Logger.LogHeader();
            data.Logger.Log($"Generated QR code PNG ({pngBytes.Length} bytes) for: {content}", LogColors.YellowGreen);

            return pngBytes;
        }

        [Block("Generates a QR code and returns it as a Base64-encoded PNG string")]
        public static string QrCodeToBase64(BotData data, string content, int pixelsPerModule = 10)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var pngBytes = qrCode.GetGraphic(pixelsPerModule);
            var base64 = Convert.ToBase64String(pngBytes);

            data.Logger.LogHeader();
            data.Logger.Log($"Generated QR code Base64 ({base64.Length} chars) for: {content}", LogColors.YellowGreen);

            return base64;
        }

        [Block("Generates a QR code and saves it as a PNG file")]
        public static void QrCodeToFile(BotData data, string content, string filePath, int pixelsPerModule = 10)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var pngBytes = qrCode.GetGraphic(pixelsPerModule);
            File.WriteAllBytes(filePath, pngBytes);

            data.Logger.LogHeader();
            data.Logger.Log($"Saved QR code to {filePath} for: {content}", LogColors.YellowGreen);
        }
    }
}
