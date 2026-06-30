namespace TimerApp.Desktop;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TimerApp.Desktop.Models;
using TimerApp.Desktop.Services;
using TimerApp.Desktop.ViewModels;

public partial class VoiceSettingsWindow : Window
{
    private VoiceSettingsViewModel _vm = null!;

    public VoiceSettingsWindow() => InitializeComponent();

    public VoiceSettingsWindow(MainViewModel mainVm, GlobalHotkeyService hotkeyService) : this()
    {
        _vm         = new VoiceSettingsViewModel(mainVm, hotkeyService);
        DataContext = _vm;
        KeyDown    += OnWindowKeyDown;
    }

    // ── HK capture ───────────────────────────────────────────────

    private void DefineHK_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm.IsCapturingHK) { _vm.StopCapture(); return; }
        _vm.StartCapture();
        Focus();
    }

    private void ClearHK_Click(object? sender, RoutedEventArgs e)
        => _vm.ClearBinding();

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_vm.IsCapturingHK) return;

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                  or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        if (e.Key == Key.Escape) { _vm.StopCapture(); e.Handled = true; return; }

        if (!HotkeyBinding.AvaloniaMap.TryGetValue(e.Key, out var code)) return;

        _vm.CaptureKey(code,
            e.KeyModifiers.HasFlag(KeyModifiers.Control),
            e.KeyModifiers.HasFlag(KeyModifiers.Alt),
            e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        e.Handled = true;
    }

    // ── Save / Cancel ─────────────────────────────────────────────

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _vm.StopCapture();
        _vm.Save(Close);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        _vm.StopCapture();
        Close();
    }
}
