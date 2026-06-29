namespace TimerApp.Desktop;

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TimerApp.Core.Models;
using TimerApp.Desktop.Models;
using TimerApp.Desktop.Services;
using TimerApp.Desktop.ViewModels;

public partial class HotkeySettingsWindow : Window
{
    private readonly GlobalHotkeyService _service;
    private HotkeySlotVM?               _capturingSlot;

    public List<HotkeyGroupVM> Groups { get; }

    public HotkeySettingsWindow() : this(App.HotkeyService!) { }

    public HotkeySettingsWindow(GlobalHotkeyService service)
    {
        _service = service;
        var cfg  = service.Config;

        Groups = new List<HotkeyGroupVM>
        {
            new("Food",   "🍗", TimerCategory.Food,   cfg.Food),
            new("Potion", "💊", TimerCategory.Potion, cfg.Potion),
            new("Boost",  "⚡", TimerCategory.Boost,  cfg.Boost),
            new("Custom", "⏱", TimerCategory.Custom, cfg.Custom),
        };

        DataContext = this;
        InitializeComponent();
        KeyDown += OnWindowKeyDown;
    }

    // ── Capture flow ─────────────────────────────────────────────

    private void OnDefine_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not HotkeySlotVM slot) return;

        if (_capturingSlot == slot)
        {
            CancelCapture();
            return;
        }

        if (_capturingSlot != null) CancelCapture();

        _capturingSlot       = slot;
        slot.IsCapturing     = true;
        _service.Capturing   = true;
        Focus();
    }

    private void OnClear_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not HotkeySlotVM slot) return;
        if (_capturingSlot == slot) CancelCapture();
        slot.ClearBinding();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_capturingSlot == null) return;

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                  or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        if (e.Key == Key.Escape)
        {
            CancelCapture();
            e.Handled = true;
            return;
        }

        if (!HotkeyBinding.AvaloniaMap.TryGetValue(e.Key, out var code))
            return;

        var ctrl  = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var alt   = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        _capturingSlot.CaptureKey(code, ctrl, alt, shift);
        _capturingSlot     = null;
        _service.Capturing = false;
        e.Handled          = true;
    }

    private void CancelCapture()
    {
        if (_capturingSlot == null) return;
        _capturingSlot.IsCapturing = false;
        _capturingSlot             = null;
        _service.Capturing         = false;
    }

    // ── Save / Cancel ─────────────────────────────────────────────

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        CancelCapture();
        var cfg = _service.Config;
        Groups[0].ApplyTo(cfg.Food);
        Groups[1].ApplyTo(cfg.Potion);
        Groups[2].ApplyTo(cfg.Boost);
        Groups[3].ApplyTo(cfg.Custom);
        cfg.Save();
        _service.ReloadConfig();
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        CancelCapture();
        Close();
    }
}
