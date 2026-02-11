namespace ProjectBullet.Avalonia.Platforms
{
    public interface IScreenshotService
    {
        void Take(int width, int height, int top, int left);
    }

    public class ScreenshotService : IScreenshotService
    {
        public void Take(int width, int height, int top, int left)
        {
            Utils.Screenshot.Take(width, height, top, left);
        }
    }
}
