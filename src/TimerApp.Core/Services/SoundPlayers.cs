namespace TimerApp.Core.Services;

// ─── Windows ────────────────────────────────────────────────
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class WindowsSoundPlayer : ISoundPlayer, IDisposable
{
    // WMPLib só existe no Windows — usamos dynamic pra não
    // quebrar a compilação em Mac/Linux
    private dynamic? _player;

    public void Play(string filePath, float volume)
    {
        Stop();

        // Cria o Windows Media Player via COM (interop nativo do Windows)
        // "dynamic" = tipo resolvido em runtime, sem checagem estática
        // Equivalente ao require() de uma lib nativa no Node
        var type = Type.GetTypeFromProgID("WMPlayer.OCX");
        if (type is null) return;

        _player = Activator.CreateInstance(type);
        _player!.settings.volume = (int)(volume * 100);
        _player.URL = filePath;
        _player.controls.play();
    }

    public void Stop()
    {
        _player?.controls.stop();
        _player = null;
    }

    public void SetVolume(float volume)
    {
        if (_player is not null)
            _player.settings.volume = (int)(volume * 100);
    }

    public void Dispose() => Stop();
}

// ─── macOS ──────────────────────────────────────────────────
internal sealed class MacOSSoundPlayer : ISoundPlayer
{
    private System.Diagnostics.Process? _process;

    public void Play(string filePath, float volume)
    {
        Stop();

        // afplay = comando nativo do macOS pra tocar áudio
        // Equivalente a exec("afplay arquivo.mp3") no Node
        _process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName  = "afplay",
            Arguments = $"-v {volume.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} \"{filePath}\"",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true
        });
    }

    public void Stop()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill();
            _process.Dispose();
        }
        _process = null;
    }

    public void SetVolume(float volume)
    {
        // afplay não suporta mudança de volume em runtime
        // precisaria reiniciar o processo — simplificamos por ora
    }
}

// ─── Linux ──────────────────────────────────────────────────
internal sealed class LinuxSoundPlayer : ISoundPlayer
{
    private System.Diagnostics.Process? _process;

    public void Play(string filePath, float volume)
    {
        Stop();

        // paplay = PulseAudio player, disponível na maioria das distros
        _process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName  = "paplay",
            Arguments = $"--volume={(int)(volume * 65536)} \"{filePath}\"",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true
        });
    }

    public void Stop()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill();
            _process.Dispose();
        }
        _process = null;
    }

    public void SetVolume(float volume) { }
}

// ─── Fallback silencioso ─────────────────────────────────────
// Padrão Null Object — evita null checks espalhados pelo código
// Em vez de if (soundService != null) soundService.Play(...)
// simplesmente sempre chamamos Play() e esse cara não faz nada
internal sealed class NullSoundPlayer : ISoundPlayer
{
    public void Play(string filePath, float volume) { }
    public void Stop() { }
    public void SetVolume(float volume) { }
}