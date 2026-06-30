namespace TimerApp.Desktop.Services;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vosk;
using TimerApp.Core.Models;
using TimerApp.Desktop.ViewModels;

public sealed class VoiceCommandService : IDisposable
{
    private static readonly Regex MinutesDigitRegex = new(
        @"(\d{1,3})\s*minuto", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Vocabulário restrito às palavras PT que o modelo conhece.
    // Isso força o modelo a escolher a palavra mais próxima desta lista,
    // melhorando muito o reconhecimento de palavras curtas/específicas.
    private const string Grammar = """
        ["tempo",
         "comida", "poção", "experiência",
         "ativar", "pausar", "deletar", "reiniciar", "continuar", "voltar",
         "tudo", "todos",
         "um", "dois", "tres", "quatro", "cinco", "seis", "sete", "oito", "nove", "dez",
         "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove",
         "vinte", "trinta", "quarenta", "cinquenta", "sessenta",
         "minuto", "minutos", "[unk]"]
        """;

    private const int    SampleRate      = 16000;
    private const double CooldownSeconds = 3.0;

    private readonly MainViewModel _vm;
    private VoskRecognizer?        _recognizer;
    private IAudioCapture?         _capture;
    private DateTime               _lastCommand = DateTime.MinValue;
    private bool                   _disposed;

    public event Action<string>?         StatusChanged;
    public event Action<string>?         PartialReceived;
    public event Action<string, string>? TranscriptReceived;

    public static bool IsSupported => true;

    public VoiceCommandService(MainViewModel vm) => _vm = vm;

    // ── Lifecycle ─────────────────────────────────────────────────

    public bool Start(string modelPath, int deviceIndex = -1)
    {
        try
        {
            Vosk.SetLogLevel(0); // mostra warnings do grammar no console — útil para debug
            var model = new Model(modelPath);
            _recognizer = new VoskRecognizer(model, SampleRate, Grammar);
            _recognizer.SetMaxAlternatives(0);
            _recognizer.SetWords(false);

            _capture = new PortAudioCapture();

            _capture.DataAvailable += OnAudioData;
            _capture.Start(deviceIndex);

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
        _capture?.Stop();
        _capture?.Dispose();
        _capture = null;

        _recognizer?.Dispose();
        _recognizer = null;

        NotifyStatus("desativado");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // ── Audio ─────────────────────────────────────────────────────

    private void OnAudioData(byte[] buffer, int bytesRecorded)
    {
        if (_recognizer is null) return;

        if (_recognizer.AcceptWaveform(buffer, bytesRecorded))
        {
            var text = ExtractText(_recognizer.Result());
            if (!string.IsNullOrWhiteSpace(text))
            {
                var cmd = text.ToLowerInvariant().Trim();
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var result = TryExecuteDebug(cmd);
                    TranscriptReceived?.Invoke(text, result);
                });
            }
        }
        else
        {
            var partial = ExtractText(_recognizer.PartialResult());
            if (!string.IsNullOrWhiteSpace(partial))
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => PartialReceived?.Invoke(partial));
        }
    }

    // ── Gate ─────────────────────────────────────────────────────
    // A palavra de ativação deve ser a PRIMEIRA do comando.
    // "pausar", "voltar", "deletar" funcionam sozinhos.
    // "tempo" é obrigatório para ativar, reiniciar e timer customizado.

    private static bool StartsWithTrigger(string t) =>
        t.StartsWith("tempo")    ||
        t.StartsWith("pausar")   ||
        t.StartsWith("voltar")   ||
        t.StartsWith("continuar")||
        t.StartsWith("deletar");

    private string TryExecuteDebug(string transcript)
    {
        if (!StartsWithTrigger(transcript))
            return "❌ sem gatilho";

        var now = DateTime.UtcNow;
        if ((now - _lastCommand).TotalSeconds < CooldownSeconds)
            return "⏳ cooldown";
        _lastCommand = now;

        return ExecuteCommand(transcript);
    }

    // ── Matching 100% português ───────────────────────────────────

    private static bool IsComida(string c)       => c.Contains("comida");
    private static bool IsPoção(string c)        => c.Contains("pocao") || c.Contains("poção") || c.Contains("pocões");
    private static bool IsExperiencia(string c)  => c.Contains("experiencia") || c.Contains("experiência");
    private static bool IsAll(string c)          => c.Contains("tudo") || c.Contains("todos");

    private static bool IsPauseVerb(string c)    => c.Contains("pausar");
    private static bool IsDeleteVerb(string c)   => c.Contains("deletar");
    private static bool IsResetVerb(string c)    => c.Contains("reiniciar");
    private static bool IsResumeVerb(string c)   => c.Contains("voltar") || c.Contains("continuar");
    private static bool IsActivateVerb(string c) => c.Contains("ativar");

    // ── Dispatch ──────────────────────────────────────────────────

    private string ExecuteCommand(string cmd)
    {
        // Deletar tudo
        if (IsDeleteVerb(cmd) && IsAll(cmd))
        { _vm.StopAll(); Speak("Todos os timers deletados."); return "✅ deletar tudo"; }

        // Deletar específico
        if (IsDeleteVerb(cmd))
        {
            if (IsComida(cmd))
            { _vm.HotkeyStop(TimerCategory.Food);   Speak("Comida deletada."); return "✅ deletar comida"; }
            if (IsPoção(cmd))
            { _vm.HotkeyStop(TimerCategory.Potion); Speak("Poção deletada."); return "✅ deletar poção"; }
            if (IsExperiencia(cmd))
            { _vm.HotkeyStop(TimerCategory.Boost);  Speak("Experiência deletada."); return "✅ deletar experiencia"; }
            return "⚠️ deletar — qual timer?";
        }

        // Pausar tudo
        if (IsPauseVerb(cmd) && IsAll(cmd))
        { _vm.PauseAll(); Speak("Todos os timers pausados."); return "✅ pausar tudo"; }

        // Pausar específico
        if (IsPauseVerb(cmd))
        {
            if (IsComida(cmd))
            { _vm.HotkeyPauseResume(TimerCategory.Food);   Speak("Comida pausada."); return "✅ pausar comida"; }
            if (IsPoção(cmd))
            { _vm.HotkeyPauseResume(TimerCategory.Potion); Speak("Poção pausada."); return "✅ pausar poção"; }
            if (IsExperiencia(cmd))
            { _vm.HotkeyPauseResume(TimerCategory.Boost);  Speak("Experiência pausada."); return "✅ pausar experiencia"; }
            return "⚠️ pausar — qual timer?";
        }

        // Voltar tudo
        if (IsResumeVerb(cmd) && IsAll(cmd))
        { _vm.ResumeAll(); Speak("Todos os timers retomados."); return "✅ voltar tudo"; }

        // Voltar específico
        if (IsResumeVerb(cmd))
        {
            if (IsComida(cmd))
            { _vm.HotkeyPauseResume(TimerCategory.Food);   Speak("Comida retomada."); return "✅ voltar comida"; }
            if (IsPoção(cmd))
            { _vm.HotkeyPauseResume(TimerCategory.Potion); Speak("Poção retomada."); return "✅ voltar poção"; }
            if (IsExperiencia(cmd))
            { _vm.HotkeyPauseResume(TimerCategory.Boost);  Speak("Experiência retomada."); return "✅ voltar experiencia"; }
        }

        // Reiniciar tudo
        if (IsResetVerb(cmd) && IsAll(cmd))
        { _vm.StopAll(); Speak("Todos os timers reiniciados."); return "✅ reiniciar tudo"; }

        // Reiniciar específico
        if (IsResetVerb(cmd))
        {
            if (IsComida(cmd))
            { _vm.HotkeyReset(TimerCategory.Food);   Speak("Comida reiniciada."); return "✅ reiniciar comida"; }
            if (IsPoção(cmd))
            { _vm.HotkeyReset(TimerCategory.Potion); Speak("Poção reiniciada."); return "✅ reiniciar poção"; }
            if (IsExperiencia(cmd))
            { _vm.HotkeyReset(TimerCategory.Boost);  Speak("Experiência reiniciada."); return "✅ reiniciar experiencia"; }
            return "⚠️ reiniciar — qual timer?";
        }

        // Ativar tudo
        if (IsActivateVerb(cmd) && IsAll(cmd))
        { _vm.StartAll(); Speak("Comida, poção e experiência ativados!"); return "✅ ativar tudo"; }

        // Ativar preset (com ou sem "ativar")
        if (IsComida(cmd))
        { _vm.StartFood();   Speak("Comida ativada!"); return "✅ ativar comida"; }
        if (IsPoção(cmd))
        { _vm.StartPotion(); Speak("Poção ativada!"); return "✅ ativar poção"; }
        if (IsExperiencia(cmd))
        { _vm.StartBoost();  Speak("Experiência ativada!"); return "✅ ativar experiencia"; }

        // "Tempo X minutos"
        var minutes = ParseMinutes(cmd);
        if (minutes.HasValue)
        {
            _vm.CustomHours   = 0;
            _vm.CustomMinutes = minutes.Value;
            _vm.CustomName    = $"{minutes.Value} minutos";
            _vm.StartCustomFromForm();
            Speak($"Timer de {minutes.Value} minutos iniciado!");
            return $"✅ tempo {minutes.Value} min";
        }

        return "❓ sem comando";
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static void Speak(string text) => VoiceSynthesizer.Speak(text);

    private static int? ParseMinutes(string text)
    {
        var m = MinutesDigitRegex.Match(text);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n is >= 1 and <= 180)
            return n;

        if (!text.Contains("minuto")) return null;

        var words = new Dictionary<string, int>
        {
            ["um"]=1,   ["dois"]=2,  ["tres"]=3,  ["quatro"]=4, ["cinco"]=5,
            ["seis"]=6, ["sete"]=7,  ["oito"]=8,  ["nove"]=9,   ["dez"]=10,
            ["onze"]=11,["doze"]=12, ["treze"]=13,["quatorze"]=14,["catorze"]=14,["quinze"]=15,
            ["dezesseis"]=16,["dezasseis"]=16,["dezessete"]=17,["dezoito"]=18,["dezenove"]=19,
            ["vinte"]=20,["trinta"]=30,["quarenta"]=40,["cinquenta"]=50,["sessenta"]=60,
        };

        foreach (var (word, num) in words)
            if (text.Contains(word)) return num;

        return null;
    }

    private static string ExtractText(string json)
    {
        var match = Regex.Match(json, @"""(?:text|partial)""\s*:\s*""([^""]*)""");
        return match.Success ? match.Groups[1].Value : "";
    }

    private void NotifyStatus(string status) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusChanged?.Invoke(status));
}
