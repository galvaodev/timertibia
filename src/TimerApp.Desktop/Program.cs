using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Velopack;

namespace TimerApp.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack DEVE ser a primeira linha antes de qualquer código Avalonia
        VelopackApp.Build().Run();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
