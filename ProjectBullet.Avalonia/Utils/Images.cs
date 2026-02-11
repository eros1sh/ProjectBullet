using System;
using System.IO;

namespace ProjectBullet.Avalonia.Utils
{
    public static class Images
    {
        public static global::Avalonia.Media.Imaging.Bitmap Base64ToBitmap(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return null;

            try
            {
                var bytes = Convert.FromBase64String(base64);
                using var ms = new MemoryStream(bytes);
                return new global::Avalonia.Media.Imaging.Bitmap(ms);
            }
            catch
            {
                return null;
            }
        }

        public static global::Avalonia.Media.Imaging.Bitmap BytesToBitmap(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            try
            {
                using var ms = new MemoryStream(bytes);
                return new global::Avalonia.Media.Imaging.Bitmap(ms);
            }
            catch
            {
                return null;
            }
        }

        public static string BitmapToBase64(global::Avalonia.Media.Imaging.Bitmap bitmap)
        {
            if (bitmap == null)
                return null;

            using var ms = new MemoryStream();
            bitmap.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
