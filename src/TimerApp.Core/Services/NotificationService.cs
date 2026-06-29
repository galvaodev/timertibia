namespace TimerApp.Core.Services;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationProvider _provider;

    public NotificationService()
    {
        _provider = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => new WindowsNotificationProvider(),
            PlatformID.Unix    => OperatingSystem.IsMacOS()
                                    ? new MacOSNotificationProvider()
                                    : new LinuxNotificationProvider(),
            _                  => new NullNotificationProvider()
        };
    }

    public void Show(string title, string message) =>
        _provider.Show(title, message);
}