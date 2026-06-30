namespace TimerApp.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using ReactiveUI;
using TimerApp.Core;
using TimerApp.Core.Models;
using TimerApp.Core.Services;
using TimerApp.Desktop.Models;
using TimerApp.Desktop.Services;

public class MainViewModel : ReactiveObject, IDisposable
{
    private readonly AppServices _services;

    public ObservableCollection<TimerCardViewModel> ActiveTimers { get; } = new();

    private bool _hasActiveTimers;
    public bool HasActiveTimers
    {
        get => _hasActiveTimers;
        private set => this.RaiseAndSetIfChanged(ref _hasActiveTimers, value);
    }

    private int _activeTimerCount;
    public int ActiveTimerCount
    {
        get => _activeTimerCount;
        private set => this.RaiseAndSetIfChanged(ref _activeTimerCount, value);
    }

    // ── Auto-update ──────────────────────────────────────────────

    private bool _updateAvailable;
    public bool UpdateAvailable
    {
        get => _updateAvailable;
        internal set => this.RaiseAndSetIfChanged(ref _updateAvailable, value);
    }

    private string _updateVersion = "";
    public string UpdateVersion
    {
        get => _updateVersion;
        internal set => this.RaiseAndSetIfChanged(ref _updateVersion, value);
    }

    private bool _isUpdating;
    public bool IsUpdating
    {
        get => _isUpdating;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isUpdating, value);
            this.RaisePropertyChanged(nameof(IsNotUpdating));
        }
    }

    public bool IsNotUpdating => !_isUpdating;

    private int _updateProgress;
    public int UpdateProgress
    {
        get => _updateProgress;
        private set => this.RaiseAndSetIfChanged(ref _updateProgress, value);
    }

    public void ApplyUpdate()
    {
        IsUpdating = true;
        App.StartUpdate(p => Dispatcher.UIThread.Post(() => UpdateProgress = p));
    }

    // ── Overlay window ───────────────────────────────────────────

    public event EventHandler? OverlayRequested;
    public void OpenOverlay() => OverlayRequested?.Invoke(this, EventArgs.Empty);

    // ── Hotkey settings ──────────────────────────────────────────

    public event EventHandler? HotkeySettingsRequested;
    public void OpenHotkeySettings() => HotkeySettingsRequested?.Invoke(this, EventArgs.Empty);

    // ── Hotkey dispatch ──────────────────────────────────────────

    public void HotkeyStart(TimerCategory cat)
    {
        switch (cat)
        {
            case TimerCategory.Food:   StartFood();           break;
            case TimerCategory.Boost:  StartBoost();          break;
            case TimerCategory.Potion: StartPotion();         break;
            default:                   StartCustomFromForm(); break;
        }
    }

    public void HotkeyPauseResume(TimerCategory cat)
        => ActiveTimers.FirstOrDefault(t => t.Category == cat)?.TogglePause();

    public void HotkeyReset(TimerCategory cat)
        => ActiveTimers.FirstOrDefault(t => t.Category == cat)?.ResetTimer();

    public void HotkeyToggleRepeat(TimerCategory cat)
        => ActiveTimers.FirstOrDefault(t => t.Category == cat)?.ToggleRepeat();

    // ── Tab navigation ───────────────────────────────────────────

    private string _currentTab = "new";
    public string CurrentTab
    {
        get => _currentTab;
        private set
        {
            this.RaiseAndSetIfChanged(ref _currentTab, value);
            this.RaisePropertyChanged(nameof(IsNewTab));
            this.RaisePropertyChanged(nameof(IsActiveTab));
        }
    }

    public bool IsNewTab    => CurrentTab == "new";
    public bool IsActiveTab => CurrentTab == "active";

    public void GoToNewTab()    => CurrentTab = "new";
    public void GoToActiveTab() => CurrentTab = "active";

    // ── Custom form ──────────────────────────────────────────────

    private string _customName = "";
    public string CustomName
    {
        get => _customName;
        set => this.RaiseAndSetIfChanged(ref _customName, value);
    }

    // ── Volume ───────────────────────────────────────────────────

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set
        {
            this.RaiseAndSetIfChanged(ref _volume, value);
            this.RaisePropertyChanged(nameof(VolumeLabel));
            _services.Sound.Volume = (float)value;
        }
    }

    public string VolumeLabel => $"{(int)(_volume * 100)}%";

    private decimal _customHours;
    public decimal CustomHours
    {
        get => _customHours;
        set => this.RaiseAndSetIfChanged(ref _customHours, value);
    }

    private decimal _customMinutes = 5;
    public decimal CustomMinutes
    {
        get => _customMinutes;
        set => this.RaiseAndSetIfChanged(ref _customMinutes, value);
    }

    public void StartCustomFromForm()
    {
        var totalMinutes = (int)CustomHours * 60 + (int)CustomMinutes;
        if (totalMinutes <= 0) return;

        var config = new TimerConfig
        {
            Name      = string.IsNullOrWhiteSpace(CustomName) ? "Custom" : CustomName,
            Category  = TimerCategory.Custom,
            Duration  = TimeSpan.FromMinutes(totalMinutes),
            IsLooping = true
        };

        StartTimer(config);
    }

    // ── Constructor ──────────────────────────────────────────────

    public MainViewModel()
    {
        _services    = new AppServices();
        _appSettings = AppSettings.Load();
        _services.Timer.AnyTimerElapsed += OnTimerElapsed;

        if (_appSettings.VoiceEnabled)
        {
            _voiceEnabled = true;
            _ = StartVoiceAsync();
        }
    }

    // ── Presets ──────────────────────────────────────────────────

    public void StartFood()   => StartTimer(TimerConfig.CreateFood());
    public void StartBoost()  => StartTimer(TimerConfig.CreateBoost());
    public void StartPotion() => StartTimer(TimerConfig.CreatePotion());

    // ── Controls ─────────────────────────────────────────────────

    public void PauseTimer(string timerId)     => _services.Timer.Pause(timerId);
    public void ResumeTimer(string timerId)    => _services.Timer.Resume(timerId);
    public void ResetTimerCard(string timerId) => _services.Timer.Reset(timerId);

    public void StopTimer(string timerId)
    {
        _services.Timer.Stop(timerId);
        var card = ActiveTimers.FirstOrDefault(t => t.TimerId == timerId);
        if (card is not null) ActiveTimers.Remove(card);
        SyncCounts();
    }

    public void StopAll()
    {
        _services.Timer.StopAll();
        ActiveTimers.Clear();
        SyncCounts();
    }

    public void Dispose()
    {
        _voiceService?.Dispose();
        _services.Dispose();
    }

    // ── Voice commands ───────────────────────────────────────────

    private readonly AppSettings       _appSettings;
    private VoiceCommandService?       _voiceService;

    public static bool IsVoiceSupported => VoiceCommandService.IsSupported;

    public void ToggleVoice() => VoiceEnabled = !VoiceEnabled;

    public string VoiceTooltip => IsVoiceDownloading
        ? $"Baixando modelo... {VoiceDownloadProgress}%"
        : $"Voz: {VoiceStatus}";

    public double VoiceMicOpacity => VoiceEnabled ? 1.0 : 0.35;

    private bool _voiceEnabled;
    public bool VoiceEnabled
    {
        get => _voiceEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _voiceEnabled, value);
            this.RaisePropertyChanged(nameof(VoiceMicOpacity));
            _appSettings.VoiceEnabled = value;
            _appSettings.Save();
            if (value) _ = StartVoiceAsync();
            else       StopVoice();
        }
    }

    private string _voiceStatus = "desativado";
    public string VoiceStatus
    {
        get => _voiceStatus;
        private set
        {
            this.RaiseAndSetIfChanged(ref _voiceStatus, value);
            this.RaisePropertyChanged(nameof(VoiceTooltip));
        }
    }

    private bool _isVoiceListening;
    public bool IsVoiceListening
    {
        get => _isVoiceListening;
        private set => this.RaiseAndSetIfChanged(ref _isVoiceListening, value);
    }

    private bool _isVoiceDownloading;
    public bool IsVoiceDownloading
    {
        get => _isVoiceDownloading;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isVoiceDownloading, value);
            this.RaisePropertyChanged(nameof(IsNotVoiceDownloading));
        }
    }

    public bool IsNotVoiceDownloading => !_isVoiceDownloading;

    private int _voiceDownloadProgress;
    public int VoiceDownloadProgress
    {
        get => _voiceDownloadProgress;
        private set
        {
            this.RaiseAndSetIfChanged(ref _voiceDownloadProgress, value);
            this.RaisePropertyChanged(nameof(VoiceTooltip));
        }
    }

    private async System.Threading.Tasks.Task StartVoiceAsync()
    {
        if (!IsVoiceSupported)
        {
            VoiceStatus = "não disponível no Mac/Linux ainda";
            _voiceEnabled = false;
            this.RaisePropertyChanged(nameof(VoiceEnabled));
            return;
        }

        if (!VoiceModelManager.IsDownloaded)
        {
            IsVoiceDownloading = true;
            VoiceStatus = "baixando modelo...";
            try
            {
                var progress = new Progress<int>(p =>
                    Dispatcher.UIThread.Post(() => VoiceDownloadProgress = p));
                await VoiceModelManager.DownloadAsync(progress);
            }
            catch
            {
                VoiceStatus        = "erro ao baixar modelo";
                IsVoiceDownloading = false;
                _voiceEnabled      = false;
                this.RaisePropertyChanged(nameof(VoiceEnabled));
                return;
            }
            IsVoiceDownloading = false;
        }

        _voiceService = new VoiceCommandService(this);
        _voiceService.StatusChanged += s => Dispatcher.UIThread.Post(() =>
        {
            VoiceStatus      = s;
            IsVoiceListening = s != "desativado";
        });

        if (!_voiceService.Start(VoiceModelManager.ModelPath))
        {
            _voiceEnabled = false;
            this.RaisePropertyChanged(nameof(VoiceEnabled));
            _voiceService.Dispose();
            _voiceService = null;
        }
    }

    private void StopVoice()
    {
        _voiceService?.Stop();
        _voiceService?.Dispose();
        _voiceService    = null;
        IsVoiceListening = false;
        VoiceStatus      = "desativado";
    }

    // ── Private ──────────────────────────────────────────────────

    private void StartTimer(TimerConfig config)
    {
        var instance = _services.Timer.Start(config);
        var card = new TimerCardViewModel(
            instance, StopTimer, PauseTimer, ResumeTimer, ResetTimerCard);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ActiveTimers.Add(card);
            SyncCounts();
            CurrentTab = "active";
        });
    }

    private void SyncCounts()
    {
        HasActiveTimers  = ActiveTimers.Count > 0;
        ActiveTimerCount = ActiveTimers.Count;
    }

    private void OnTimerElapsed(object? sender, TimerInstance timer)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var card = ActiveTimers.FirstOrDefault(t => t.TimerId == timer.Config.Id);
            card?.NotifyElapsed();
        });
    }
}
