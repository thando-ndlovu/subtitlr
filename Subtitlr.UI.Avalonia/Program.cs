using Avalonia;

namespace Subtitlr.UI.Avalonia;

class Program
{
    public static void Main(string[] args)
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .StartWithClassicDesktopLifetime(args);
    }
}
