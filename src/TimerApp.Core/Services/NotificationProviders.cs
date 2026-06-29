namespace TimerApp.Core.Services;

internal interface INotificationProvider
{
    void Show(string title, string message);
}

// ─── Windows ────────────────────────────────────────────────
internal sealed class WindowsNotificationProvider : INotificationProvider
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void Show(string title, string message)
    {
        // PowerShell é o jeito mais simples sem dependências extras
        // Mostra um balão na bandeja do sistema
        var script = $@"
            Add-Type -AssemblyName System.Windows.Forms
            $notify = New-Object System.Windows.Forms.NotifyIcon
            $notify.Icon = [System.Drawing.SystemIcons]::Information
            $notify.Visible = $true
            $notify.ShowBalloonTip(5000, '{title.Sanitize()}', '{message.Sanitize()}', [System.Windows.Forms.ToolTipIcon]::None)
            Start-Sleep -Seconds 6
            $notify.Dispose()
        ";

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName  = "powershell",
            Arguments = $"-NoProfile -NonInteractive -Command \"{script}\"",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true
        });
    }
}

// ─── macOS ──────────────────────────────────────────────────
internal sealed class MacOSNotificationProvider : INotificationProvider
{
    public void Show(string title, string message)
    {
        // osascript = AppleScript nativo do macOS
        // display notification é o comando pra notificação nativa
        var script = $"display notification \"{message.Sanitize()}\" with title \"{title.Sanitize()}\"";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "osascript",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true
        };
        psi.ArgumentList.Add("-e");
        psi.ArgumentList.Add(script);
        System.Diagnostics.Process.Start(psi);
    }
}

// ─── Linux ──────────────────────────────────────────────────
internal sealed class LinuxNotificationProvider : INotificationProvider
{
    public void Show(string title, string message)
    {
        // notify-send = ferramenta padrão do Linux pra notificações
        // vem instalado na maioria das distros com ambiente gráfico
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName  = "notify-send",
            Arguments = $"--urgency=normal \"{title.Sanitize()}\" \"{message.Sanitize()}\"",
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true
        });
    }
}

// ─── Null Object ────────────────────────────────────────────
internal sealed class NullNotificationProvider : INotificationProvider
{
    public void Show(string title, string message) { }
}

// ─── Utilitário compartilhado ───────────────────────────────
// Classe estática com método de extensão — explicação abaixo
internal static class NotificationHelpers
{
    // Remove aspas e quebras de linha pra não quebrar os comandos shell
    // Equivalente ao sanitize() que você faria em qualquer input JS
    public static string Sanitize(this string input) =>
        input.Replace("'", "").Replace("\"", "").Replace("\n", " ");
}