namespace TimerApp.Core;

using TimerApp.Core.Services;

// Container de serviços — padrão simples antes de usar um DI container de verdade
// No front seria o seu Context Provider que envolve o app inteiro
public sealed class AppServices : IDisposable
{
    // Lazy<T> = inicializa só quando for usado pela primeira vez
    // Equivalente ao inicializar um singleton só no primeiro acesso
    private readonly Lazy<TimerService>       _timerService;
    private readonly Lazy<SoundService>       _soundService;
    private readonly Lazy<NotificationService> _notificationService;

    public ITimerService       Timer        => _timerService.Value;
    public ISoundService       Sound        => _soundService.Value;
    public INotificationService Notification => _notificationService.Value;

    public AppServices()
    {
        _timerService        = new Lazy<TimerService>();
        _soundService        = new Lazy<SoundService>();
        _notificationService = new Lazy<NotificationService>();

        // Quando qualquer timer acabar, toca o som e notifica
        // Isso conecta o TimerService ao SoundService e NotificationService
        // É o "fio" que liga os serviços entre si
        _timerService.Value.AnyTimerElapsed += OnTimerElapsed;
    }

    private void OnTimerElapsed(object? sender, Models.TimerInstance timer)
    {
        // Toca o som correspondente à categoria do timer
        var soundPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            timer.Config.SoundFile
        );
        Sound.Play(soundPath);

        // Mostra a notificação nativa
        var message = timer.Config.IsLooping
            ? $"{timer.Config.Duration.TotalMinutes:0} min — reiniciando..."
            : $"{timer.Config.Name} finalizado!";

        Notification.Show($"Timer: {timer.Config.Name}", message);
    }

    public void Dispose()
    {
        if (_timerService.IsValueCreated)
            _timerService.Value.Dispose();

        if (_soundService.IsValueCreated)
            _soundService.Value.Dispose();
    }
}