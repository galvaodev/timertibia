namespace TimerApp.Desktop.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using ReactiveUI;
using TimerApp.Core;
using TimerApp.Core.Models;
using TimerApp.Core.Services;

public class MainViewModel : ReactiveObject
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
        _services = new AppServices();
        _services.Timer.AnyTimerElapsed += OnTimerElapsed;
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
