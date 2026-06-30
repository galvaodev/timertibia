namespace TimerApp.Desktop.Services;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public static class VoiceSynthesizer
{
    private static Process? _current;

    public static void Speak(string text)
    {
        // Cancela fala anterior para não acumular
        try { _current?.Kill(); } catch { /* ignore */ }

        Task.Run(() =>
        {
            try
            {
                ProcessStartInfo psi;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Luciana = voz pt-BR do macOS, ótima qualidade
                    psi = new ProcessStartInfo("say")
                    {
                        Arguments        = $"-v Luciana \"{Escape(text)}\"",
                        UseShellExecute  = false,
                        CreateNoWindow   = true,
                    };
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // PowerShell com System.Speech (built-in no Windows)
                    var script = $"Add-Type -AssemblyName System.Speech; " +
                                 $"$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                                 $"$s.Rate = 1; $s.Speak('{Escape(text)}')";
                    psi = new ProcessStartInfo("powershell")
                    {
                        Arguments       = $"-NoProfile -WindowStyle Hidden -Command \"{script}\"",
                        UseShellExecute = false,
                        CreateNoWindow  = true,
                    };
                }
                else
                {
                    // Linux — requer espeak-ng instalado
                    psi = new ProcessStartInfo("espeak-ng")
                    {
                        Arguments       = $"-v pt-br \"{Escape(text)}\"",
                        UseShellExecute = false,
                        CreateNoWindow  = true,
                    };
                }

                _current = Process.Start(psi);
                _current?.WaitForExit();
            }
            catch { /* TTS opcional — falha silenciosa */ }
        });
    }

    private static string Escape(string text) =>
        text.Replace("\"", "").Replace("'", "").Replace("`", "");
}
