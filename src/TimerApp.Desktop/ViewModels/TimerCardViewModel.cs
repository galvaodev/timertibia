namespace TimerApp.Desktop.ViewModels;

using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using TimerApp.Core.Models;

public class TimerCardViewModel : ReactiveObject
{
    private readonly Action<string> _onStop;
    private readonly Action<string> _onPause;
    private readonly Action<string> _onResume;
    private readonly Action<string> _onReset;
    private readonly TimerInstance  _instance;

    // ── Identity ─────────────────────────────────────────────────
    public string        TimerId   => _instance.Config.Id;
    public string        TimerName => _instance.Config.Name;
    public bool          IsLooping => _instance.Config.IsLooping;
    public TimerCategory Category  => _instance.Config.Category;

    // ── Category ─────────────────────────────────────────────────
    public string TypeLabel => _instance.Config.Category switch
    {
        TimerCategory.Food   => "Food",
        TimerCategory.Boost  => "Boost",
        TimerCategory.Potion => "Potion",
        _                    => "Custom"
    };

    public IBrush AccentBrush       { get; }
    public IBrush SoftBrush         { get; }
    public IBrush AccentBorderBrush { get; }

    // ── Image ────────────────────────────────────────────────────
    private Bitmap? _categoryImage;
    public Bitmap? CategoryImage => _categoryImage ??= LoadImage();

    private Bitmap LoadImage()
    {
        var path = _instance.Config.Category switch
        {
            TimerCategory.Food   => "avares://TimerApp.Desktop/Assets/images/food.gif",
            TimerCategory.Boost  => "avares://TimerApp.Desktop/Assets/images/boost.gif",
            TimerCategory.Potion => "avares://TimerApp.Desktop/Assets/images/potion.gif",
            _                    => "avares://TimerApp.Desktop/Assets/images/default.gif"
        };
        return new Bitmap(AssetLoader.Open(new Uri(path)));
    }

    // ── Dynamic state ────────────────────────────────────────────
    private string _displayTime = "00:00";
    public string DisplayTime
    {
        get => _displayTime;
        private set => this.RaiseAndSetIfChanged(ref _displayTime, value);
    }

    private double _progressPercent = 100;
    public double ProgressPercent
    {
        get => _progressPercent;
        private set => this.RaiseAndSetIfChanged(ref _progressPercent, value);
    }

    private bool _isPaused;
    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isPaused, value);
            this.RaisePropertyChanged(nameof(ToggleLabel));
            this.RaisePropertyChanged(nameof(StatusLine));
            this.RaisePropertyChanged(nameof(PauseResumeIcon));
            this.RaisePropertyChanged(nameof(CountdownBrush));
        }
    }

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        private set
        {
            this.RaiseAndSetIfChanged(ref _isCompleted, value);
            this.RaisePropertyChanged(nameof(ToggleLabel));
            this.RaisePropertyChanged(nameof(StatusLine));
            this.RaisePropertyChanged(nameof(CountdownBrush));
            this.RaisePropertyChanged(nameof(ProgressBrush));
        }
    }

    public string ToggleLabel    => IsCompleted ? "Reiniciar" : (IsPaused ? "Continuar" : "Pausar");
    public string StatusLine     => IsCompleted ? "Concluído" : (IsPaused ? "Pausado" : "Em andamento");
    public string PauseResumeIcon => IsPaused ? "▶" : "⏸";

    public IBrush CountdownBrush => IsCompleted
        ? MakeBrush("#ff5d6c")
        : IsPaused
            ? MakeBrush("#8b929e")
            : MakeBrush("#f1f3f5");

    public IBrush ProgressBrush => IsCompleted ? MakeBrush("#f0556b") : AccentBrush;

    // ── Repeat toggle ────────────────────────────────────────────
    private bool _repeatEnabled;
    public bool RepeatEnabled
    {
        get => _repeatEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _repeatEnabled, value);
            this.RaisePropertyChanged(nameof(RepeatCheckBrush));
            this.RaisePropertyChanged(nameof(RepeatCheckBorderBrush));
            _instance.IsLooping = value;
        }
    }

    public IBrush RepeatCheckBrush       => RepeatEnabled ? AccentBrush : Brushes.Transparent;
    public IBrush RepeatCheckBorderBrush => RepeatEnabled ? AccentBrush : MakeBrush("#3a414c");

    // ── Constructor ──────────────────────────────────────────────
    public TimerCardViewModel(
        TimerInstance instance,
        Action<string> onStop,
        Action<string> onPause,
        Action<string> onResume,
        Action<string> onReset)
    {
        _instance = instance;
        _onStop   = onStop;
        _onPause  = onPause;
        _onResume = onResume;
        _onReset  = onReset;

        (AccentBrush, SoftBrush, AccentBorderBrush) = instance.Config.Category switch
        {
            TimerCategory.Food   => (MakeBrush("#f0913e"), MakeBrush("#24F0913E"), MakeBrush("#61F0913E")),
            TimerCategory.Boost  => (MakeBrush("#7c8cf8"), MakeBrush("#247C8CF8"), MakeBrush("#617C8CF8")),
            TimerCategory.Potion => (MakeBrush("#f0556b"), MakeBrush("#24F0556B"), MakeBrush("#61F0556B")),
            _                    => (MakeBrush("#d8b06a"), MakeBrush("#24D8B06A"), MakeBrush("#61D8B06A")),
        };

        _repeatEnabled = instance.IsLooping;

        _instance.Ticked += (_, _) => UpdateDisplay();
        UpdateDisplay();
    }

    private static IBrush MakeBrush(string hex) => new SolidColorBrush(Color.Parse(hex));

    // ── Commands ─────────────────────────────────────────────────
    public void Stop() => _onStop(TimerId);

    public void TogglePause()
    {
        if (IsCompleted)
        {
            _onReset(TimerId);
            IsCompleted = false;
            IsPaused    = false;
            UpdateDisplay();
            return;
        }

        if (IsPaused)
        {
            _onResume(TimerId);
            IsPaused = false;
        }
        else
        {
            _onPause(TimerId);
            IsPaused = true;
        }
    }

    public void ResetTimer()
    {
        var wasPaused = IsPaused;
        _onReset(TimerId);
        IsPaused    = wasPaused;
        IsCompleted = false;
        UpdateDisplay();
    }

    public void ToggleRepeat() => RepeatEnabled = !RepeatEnabled;

    public void NotifyElapsed()
    {
        if (_instance.IsLooping)
        {
            UpdateDisplay();
            return;
        }
        IsCompleted = true;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var remaining = _instance.Remaining;
        var total     = _instance.Config.Duration;

        var formatted = remaining.TotalHours >= 1
            ? remaining.ToString(@"hh\:mm\:ss")
            : remaining.ToString(@"mm\:ss");

        var pct = total.TotalSeconds > 0
            ? Math.Clamp(remaining.TotalSeconds / total.TotalSeconds * 100, 0, 100)
            : 0;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            DisplayTime     = formatted;
            ProgressPercent = pct;
        });
    }
}
