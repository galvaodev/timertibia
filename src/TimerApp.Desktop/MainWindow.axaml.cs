namespace TimerApp.Desktop;

using Avalonia;
using Avalonia.ReactiveUI;
using TimerApp.Desktop.ViewModels;

public partial class MainWindow : ReactiveWindow<MainViewModel>
{
    private TimerWindow?        _overlay;
    private VoiceSettingsWindow? _voiceSettings;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => BindViewModel();
        BindViewModel();
        Closed += (_, _) => (DataContext as MainViewModel)?.Dispose();
    }

    private void BindViewModel()
    {
        if (DataContext is not MainViewModel vm) return;
        vm.OverlayRequested         -= OnOverlayRequested;
        vm.OverlayRequested         += OnOverlayRequested;
        vm.HotkeySettingsRequested  -= OnHotkeySettingsRequested;
        vm.HotkeySettingsRequested  += OnHotkeySettingsRequested;
        vm.VoiceSettingsRequested   -= OnVoiceSettingsRequested;
        vm.VoiceSettingsRequested   += OnVoiceSettingsRequested;
    }

    private void OnHotkeySettingsRequested(object? sender, System.EventArgs e)
    {
        if (App.HotkeyService is null) return;
        new HotkeySettingsWindow(App.HotkeyService).Show(this);
    }

    private void OnVoiceSettingsRequested(object? sender, System.EventArgs e)
    {
        if (_voiceSettings is { IsVisible: true })
        {
            _voiceSettings.Activate();
            return;
        }
        if (DataContext is not MainViewModel vm || App.HotkeyService is null) return;
        _voiceSettings = new VoiceSettingsWindow(vm, App.HotkeyService);
        _voiceSettings.Show(this);
    }

    private void OnOverlayRequested(object? sender, System.EventArgs e)
    {
        if (_overlay is { IsVisible: true })
        {
            _overlay.Activate();
            return;
        }

        if (DataContext is not MainViewModel vm) return;

        _overlay = new TimerWindow(vm);
        _overlay.Position = new PixelPoint(
            Position.X + (int)Width + 8,
            Position.Y + (int)(Height / 2) - 120);
        _overlay.Show();
    }
}
