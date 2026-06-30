namespace TimerApp.Desktop.Services;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Vosk;
using TimerApp.Core.Models;
using TimerApp.Desktop.ViewModels;

public sealed class VoiceCommandService : IDisposable
{
    private const string WakeWord          = "oi timer";
    private const int    SampleRate        = 16000;
    private const double CommandTimeoutSec = 6.0;

    private readonly MainViewModel _vm;
    private VoskRecognizer?        _recognizer;
    private IAudioCapture?         _capture;
    private Timer?                 _commandTimeout;
    private bool                   _awaitingCommand;
    private bool                   _disposed;

    public event Action<string>? StatusChanged;

    public static bool IsSupported => true; // Win + Mac + Linux via PortAudio

    public VoiceCommandService(MainViewModel vm) => _vm = vm;

    public bool Start(string modelPath)
    {
        try
        {
            Vosk.SetLogLevel(-1);
            var model = new Model(modelPath);
            _recognizer = new VoskRecognizer(model, SampleRate);
            _recognizer.SetMaxAlternatives(0);
            _recognizer.SetWords(false);

            _capture = CreateCapture();
            _capture.DataAvailable += OnAudioData;
            _capture.Start();

            NotifyStatus("escutando...");
            return true;
        }
        catch (Exception ex)
        {
            Stop();
            NotifyStatus($"erro: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        _commandTimeout?.Dispose();
        _commandTimeout  = null;
        _awaitingCommand = false;

        _capture?.Stop();
        _capture?.Dispose();
        _capture = null;

        _recognizer?.Dispose();
        _recognizer = null;

        NotifyStatus("desativado");
    }

    private static IAudioCapture CreateCapture() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new NaudioCapture()
            : new PortAudioCapture();

    private void OnAudioData(byte[] buffer, int bytesRecorded)
    {
        if (_recognizer is null) return;

        if (_recognizer.AcceptWaveform(buffer, bytesRecorded))
        {
            var text = ExtractField(_recognizer.Result(), "text");
            if (!string.IsNullOrWhiteSpace(text))
                HandleTranscript(text);
        }
        else
        {
            var partial = ExtractField(_recognizer.PartialResult(), "partial");
            if (!_awaitingCommand && partial.Contains(WakeWord, StringComparison.OrdinalIgnoreCase))
                EnterCommandMode();
        }
    }

    private void HandleTranscript(string text)
    {
        var lower = text.ToLowerInvariant().Trim();

        if (!_awaitingCommand)
        {
            if (!lower.Contains(WakeWord)) return;

            var afterWake = lower[(lower.IndexOf(WakeWord, StringComparison.Ordinal) + WakeWord.Length)..].Trim();
            if (afterWake.Length > 0)
            {
                // "oi timer food" — comando junto com wake word
                Avalonia.Threading.Dispatcher.UIThread.Post(() => ExecuteCommand(afterWake));
            }
            else
            {
                EnterCommandMode();
            }
            return;
        }

        _awaitingCommand = false;
        _commandTimeout?.Dispose();
        NotifyStatus("escutando...");

        var cmd = lower.Contains(WakeWord)
            ? lower[(lower.IndexOf(WakeWord, StringComparison.Ordinal) + WakeWord.Length)..].Trim()
            : lower;

        if (!string.IsNullOrWhiteSpace(cmd))
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ExecuteCommand(cmd));
        else
            VoiceSynthesizer.Speak("Ok, quando precisar é só chamar.");
    }

    private void EnterCommandMode()
    {
        _awaitingCommand = true;
        NotifyStatus("ouvindo comando...");
        VoiceSynthesizer.Speak("Olá, o que posso ajudar?");

        _commandTimeout?.Dispose();
        _commandTimeout = new Timer(_ =>
        {
            _awaitingCommand = false;
            NotifyStatus("escutando...");
            VoiceSynthesizer.Speak("Ok, quando precisar é só chamar.");
        }, null, TimeSpan.FromSeconds(CommandTimeoutSec), Timeout.InfiniteTimeSpan);
    }

    private void ExecuteCommand(string cmd)
    {
        // ── Presets ────────────────────────────────────────────────
        if (cmd.Contains("food"))
        { _vm.StartFood();   VoiceSynthesizer.Speak("Food ativado!"); return; }

        if (cmd.Contains("boost"))
        { _vm.StartBoost();  VoiceSynthesizer.Speak("Boost ativado!"); return; }

        if (cmd.Contains("poç") || cmd.Contains("potion"))
        { _vm.StartPotion(); VoiceSynthesizer.Speak("Potion ativado!"); return; }

        // ── Pausar ────────────────────────────────────────────────
        if (cmd.StartsWith("pausar") || cmd.StartsWith("pausa"))
        {
            if (cmd.Contains("food"))
            { _vm.HotkeyPauseResume(TimerCategory.Food);   VoiceSynthesizer.Speak("Food pausado."); return; }
            if (cmd.Contains("boost"))
            { _vm.HotkeyPauseResume(TimerCategory.Boost);  VoiceSynthesizer.Speak("Boost pausado."); return; }
            if (cmd.Contains("poç") || cmd.Contains("potion"))
            { _vm.HotkeyPauseResume(TimerCategory.Potion); VoiceSynthesizer.Speak("Potion pausado."); return; }
        }

        // ── Continuar ─────────────────────────────────────────────
        if (cmd.StartsWith("continuar") || cmd.StartsWith("retomar"))
        {
            if (cmd.Contains("food"))
            { _vm.HotkeyPauseResume(TimerCategory.Food);   VoiceSynthesizer.Speak("Food continuado."); return; }
            if (cmd.Contains("boost"))
            { _vm.HotkeyPauseResume(TimerCategory.Boost);  VoiceSynthesizer.Speak("Boost continuado."); return; }
            if (cmd.Contains("poç") || cmd.Contains("potion"))
            { _vm.HotkeyPauseResume(TimerCategory.Potion); VoiceSynthesizer.Speak("Potion continuado."); return; }
        }

        // ── Resetar ───────────────────────────────────────────────
        if (cmd.StartsWith("resetar") || cmd.StartsWith("reiniciar"))
        {
            if (cmd.Contains("tudo") || cmd.Contains("todos"))
            { _vm.StopAll(); VoiceSynthesizer.Speak("Todos os timers parados."); return; }
            if (cmd.Contains("food"))
            { _vm.HotkeyReset(TimerCategory.Food);   VoiceSynthesizer.Speak("Food resetado."); return; }
            if (cmd.Contains("boost"))
            { _vm.HotkeyReset(TimerCategory.Boost);  VoiceSynthesizer.Speak("Boost resetado."); return; }
            if (cmd.Contains("poç") || cmd.Contains("potion"))
            { _vm.HotkeyReset(TimerCategory.Potion); VoiceSynthesizer.Speak("Potion resetado."); return; }
        }

        // ── Parar tudo ────────────────────────────────────────────
        if (cmd.Contains("parar") && (cmd.Contains("tudo") || cmd.Contains("todos")))
        { _vm.StopAll(); VoiceSynthesizer.Speak("Todos os timers parados."); return; }

        // ── Tempo de X minutos ────────────────────────────────────
        var minutes = ParseMinutes(cmd);
        if (minutes.HasValue)
        {
            _vm.CustomHours   = 0;
            _vm.CustomMinutes = minutes.Value;
            _vm.CustomName    = $"{minutes.Value} minutos";
            _vm.StartCustomFromForm();
            VoiceSynthesizer.Speak($"Timer de {minutes.Value} minutos iniciado!");
            return;
        }

        VoiceSynthesizer.Speak("Não entendi. Tente novamente.");
    }

    private static int? ParseMinutes(string text)
    {
        var m = Regex.Match(text, @"(\d{1,3})\s*min");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n is >= 1 and <= 180)
            return n;

        var words = new Dictionary<string, int>
        {
            ["um"] = 1,    ["dois"] = 2,    ["três"] = 3,   ["quatro"] = 4,
            ["cinco"] = 5, ["seis"] = 6,    ["sete"] = 7,   ["oito"] = 8,
            ["nove"] = 9,  ["dez"] = 10,    ["onze"] = 11,  ["doze"] = 12,
            ["treze"] = 13, ["quatorze"] = 14, ["quinze"] = 15,
            ["dezesseis"] = 16, ["dezasseis"] = 16,
            ["dezessete"] = 17, ["dezoito"] = 18, ["dezenove"] = 19,
            ["vinte"] = 20, ["trinta"] = 30, ["quarenta"] = 40,
            ["cinquenta"] = 50, ["sessenta"] = 60
        };

        if (!text.Contains("min")) return null;

        foreach (var (word, num) in words)
            if (text.Contains(word)) return num;

        return null;
    }

    private static string ExtractField(string json, string field)
    {
        var match = Regex.Match(json, $"\"{field}\"\\s*:\\s*\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : "";
    }

    private void NotifyStatus(string status) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusChanged?.Invoke(status));

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
