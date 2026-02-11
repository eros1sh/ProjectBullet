using Avalonia;
using Avalonia.Media;

namespace ProjectBullet.Avalonia.Helpers
{
    public static class Brush
    {
        public static Color GetColor(string propertyName)
        {
            try
            {
                if (Application.Current.Resources.TryGetResource(propertyName, null, out var resource))
                    return ((SolidColorBrush)resource).Color;

                Application.Current.Resources.TryGetResource("ForegroundMain", null, out var fallback);
                return ((SolidColorBrush)fallback).Color;
            }
            catch
            {
                return Colors.Gainsboro;
            }
        }

        public static SolidColorBrush Get(string propertyName)
        {
            try
            {
                if (Application.Current.Resources.TryGetResource(propertyName, null, out var resource))
                    return (SolidColorBrush)resource;

                Application.Current.Resources.TryGetResource("ForegroundMain", null, out var fallback);
                return (SolidColorBrush)fallback;
            }
            catch
            {
                return new SolidColorBrush(Colors.Gainsboro);
            }
        }

        public static SolidColorBrush FromHex(string hex)
            => new(Color.Parse(hex));

        public static void SetAppColor(string resourceName, string color)
            => Application.Current.Resources[resourceName] = new SolidColorBrush(Color.Parse(color));
    }
}
