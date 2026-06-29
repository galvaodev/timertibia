namespace TimerApp.Core.Services;

// Detecta o sistema operacional e delega pro player correto
// Padrão Strategy — troca a implementação em runtime
public sealed class SoundService : ISoundService, IDisposable
{
    private readonly ISoundPlayer _player;
    private float _volume = 1.0f;

    public float Volume
    {
        get => _volume;
        set
        {
            // Math.Clamp = garante que fica entre 0 e 1
            // Equivalente ao: Math.min(1, Math.max(0, value)) do JS
            _volume = Math.Clamp(value, 0f, 1f);
            _player.SetVolume(_volume);
        }
    }

    public SoundService()
    {
        // OperatingSystem.IsXxx() é reconhecido pelo analisador CA1416 como guard de plataforma
        if (OperatingSystem.IsWindows())
            _player = new WindowsSoundPlayer();
        else if (OperatingSystem.IsMacOS())
            _player = new MacOSSoundPlayer();
        else if (OperatingSystem.IsLinux())
            _player = new LinuxSoundPlayer();
        else
            _player = new NullSoundPlayer();
    }

    public void Play(string soundFilePath)
    {
        var path = File.Exists(soundFilePath) ? soundFilePath : FallbackSoundPath();
        if (path is null) return;
        _player.Play(path, _volume);
    }

    private static string? FallbackSoundPath()
    {
        if (OperatingSystem.IsMacOS()) return "/System/Library/Sounds/Glass.aiff";
        return null;
    }

    public void Stop() => _player.Stop();

    public void Dispose()
    {
        if (_player is IDisposable d)
            d.Dispose();
    }
}