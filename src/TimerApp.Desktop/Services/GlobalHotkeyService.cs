namespace TimerApp.Desktop.Services;

using System;
using Avalonia.Threading;
using SharpHook;
using SharpHook.Native;
using TimerApp.Core.Models;
using TimerApp.Desktop.Models;
using TimerApp.Desktop.ViewModels;

public sealed class GlobalHotkeyService : IDisposable
{
    private readonly MainViewModel _vm;
    private TaskPoolGlobalHook?   _hook;
    private HotkeyConfig          _config;

    public bool         Capturing { get; set; }
    public HotkeyConfig Config    => _config;

    public GlobalHotkeyService(MainViewModel vm)
    {
        _vm     = vm;
        _config = HotkeyConfig.Load();
        StartHook();
    }

    public void ReloadConfig() => _config = HotkeyConfig.Load();

    private void StartHook()
    {
        _hook              = new TaskPoolGlobalHook();
        _hook.KeyPressed  += OnKeyPressed;
        _hook.RunAsync();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (Capturing) return;

        var code  = e.Data.KeyCode;
        var mask  = e.RawEvent.Mask;
        var ctrl  = mask.HasFlag(ModifierMask.LeftCtrl)  || mask.HasFlag(ModifierMask.RightCtrl);
        var alt   = mask.HasFlag(ModifierMask.LeftAlt)   || mask.HasFlag(ModifierMask.RightAlt);
        var shift = mask.HasFlag(ModifierMask.LeftShift) || mask.HasFlag(ModifierMask.RightShift);

        Dispatcher.UIThread.Post(() => Dispatch(code, ctrl, alt, shift));
    }

    private void Dispatch(KeyCode code, bool ctrl, bool alt, bool shift)
    {
        var entries = new[]
        {
            (TimerCategory.Food,   _config.Food),
            (TimerCategory.Potion, _config.Potion),
            (TimerCategory.Boost,  _config.Boost),
            (TimerCategory.Custom, _config.Custom),
        };

        foreach (var (cat, hk) in entries)
        {
            if      (hk.Start.Matches(code, ctrl, alt, shift))        { _vm.HotkeyStart(cat);        return; }
            else if (hk.PauseResume.Matches(code, ctrl, alt, shift))  { _vm.HotkeyPauseResume(cat);  return; }
            else if (hk.Reset.Matches(code, ctrl, alt, shift))        { _vm.HotkeyReset(cat);        return; }
            else if (hk.ToggleRepeat.Matches(code, ctrl, alt, shift)) { _vm.HotkeyToggleRepeat(cat); return; }
        }

        if (_config.VoiceToggle.Matches(code, ctrl, alt, shift)) { _vm.ToggleVoice(); return; }
    }

    public void Dispose()
    {
        _hook?.Dispose();
        _hook = null;
    }
}
