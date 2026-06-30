namespace TimerApp.Desktop.ViewModels;

using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;
using SharpHook.Native;
using TimerApp.Desktop.Models;
using TimerApp.Desktop.Services;

public class VoiceSettingsViewModel : ReactiveObject
{
    private readonly MainViewModel       _main;
    private readonly GlobalHotkeyService _hotkeyService;
    private HotkeyBinding                _voiceToggleBinding;

    public ObservableCollection<AudioDevice> Devices { get; } = new();

    private AudioDevice? _selectedDevice;
    public AudioDevice? SelectedDevice
    {
        get => _selectedDevice;
        set => this.RaiseAndSetIfChanged(ref _selectedDevice, value);
    }

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    private bool _isCapturingHK;
    public bool IsCapturingHK
    {
        get => _isCapturingHK;
        set
        {
            this.RaiseAndSetIfChanged(ref _isCapturingHK, value);
            this.RaisePropertyChanged(nameof(CaptureButtonText));
        }
    }

    public string VoiceToggleDisplay => _voiceToggleBinding.Display;
    public string CaptureButtonText  => _isCapturingHK ? "Cancelar" : "Definir";
    public string CurrentStatus      => _main.VoiceStatus;

    public VoiceSettingsViewModel(MainViewModel main, GlobalHotkeyService hotkeyService)
    {
        _main          = main;
        _hotkeyService = hotkeyService;
        _isEnabled     = main.VoiceEnabled;

        _voiceToggleBinding = hotkeyService.Config.VoiceToggle.Clone();

        var appSettings = AppSettings.Load();

        foreach (var d in AudioDeviceHelper.GetInputDevices())
            Devices.Add(d);

        SelectedDevice = Devices.Count > 0
            ? (Devices.FirstOrDefault(d => d.Index == appSettings.VoiceDeviceIndex) ?? Devices[0])
            : null;
    }

    public void StartCapture()
    {
        IsCapturingHK          = true;
        _hotkeyService.Capturing = true;
    }

    public void StopCapture()
    {
        IsCapturingHK          = false;
        _hotkeyService.Capturing = false;
    }

    public void CaptureKey(KeyCode code, bool ctrl, bool alt, bool shift)
    {
        _voiceToggleBinding = new HotkeyBinding { Code = code, Ctrl = ctrl, Alt = alt, Shift = shift };
        this.RaisePropertyChanged(nameof(VoiceToggleDisplay));
        StopCapture();
    }

    public void ClearBinding()
    {
        _voiceToggleBinding = new HotkeyBinding();
        this.RaisePropertyChanged(nameof(VoiceToggleDisplay));
        StopCapture();
    }

    public void Save(System.Action closeWindow)
    {
        _main.ApplyVoiceSettings(_isEnabled, SelectedDevice?.Index ?? -1);

        var cfg = _hotkeyService.Config;
        cfg.VoiceToggle.Code  = _voiceToggleBinding.Code;
        cfg.VoiceToggle.Ctrl  = _voiceToggleBinding.Ctrl;
        cfg.VoiceToggle.Alt   = _voiceToggleBinding.Alt;
        cfg.VoiceToggle.Shift = _voiceToggleBinding.Shift;
        cfg.Save();
        _hotkeyService.ReloadConfig();

        closeWindow();
    }
}
