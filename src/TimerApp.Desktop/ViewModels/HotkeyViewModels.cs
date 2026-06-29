namespace TimerApp.Desktop.ViewModels;

using System.Collections.Generic;
using ReactiveUI;
using SharpHook.Native;
using TimerApp.Core.Models;
using TimerApp.Desktop.Models;

public class HotkeySlotVM : ReactiveObject
{
    private HotkeyBinding _binding;
    private bool _isCapturing;

    public string Label { get; }

    public HotkeyBinding Binding => _binding;

    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            this.RaiseAndSetIfChanged(ref _isCapturing, value);
            this.RaisePropertyChanged(nameof(Display));
            this.RaisePropertyChanged(nameof(SetButtonText));
            this.RaisePropertyChanged(nameof(DisplayBg));
            this.RaisePropertyChanged(nameof(DisplayFg));
        }
    }

    public string Display       => IsCapturing ? "Pressione uma tecla..." : _binding.Display;
    public string SetButtonText => IsCapturing ? "Cancelar" : "Definir";
    public string DisplayBg     => IsCapturing ? "#1e2a3a" : "#0f1218";
    public string DisplayFg     => IsCapturing ? "#d8b06a" : (_binding.IsSet ? "#e8eaed" : "#3a414c");
    public string DisplayBorder => IsCapturing ? "#4a6080" : "#20262f";

    public HotkeySlotVM(string label, HotkeyBinding binding)
    {
        Label    = label;
        _binding = binding.Clone();
    }

    public void ClearBinding()
    {
        _binding    = new HotkeyBinding();
        IsCapturing = false;
        this.RaisePropertyChanged(nameof(Display));
        this.RaisePropertyChanged(nameof(DisplayFg));
    }

    public void CaptureKey(KeyCode code, bool ctrl, bool alt, bool shift)
    {
        _binding    = new HotkeyBinding { Ctrl = ctrl, Alt = alt, Shift = shift, Code = code };
        IsCapturing = false;
        this.RaisePropertyChanged(nameof(Display));
        this.RaisePropertyChanged(nameof(DisplayFg));
    }
}

public class HotkeyGroupVM
{
    public string               CategoryLabel { get; }
    public string               Icon          { get; }
    public List<HotkeySlotVM>  Slots         { get; }
    public TimerCategory        Category      { get; }

    public HotkeyGroupVM(string label, string icon, TimerCategory cat, TimerHotkeys hk)
    {
        CategoryLabel = label;
        Icon          = icon;
        Category      = cat;
        Slots = new List<HotkeySlotVM>
        {
            new("Iniciar",          hk.Start),
            new("Pausar/Continuar", hk.PauseResume),
            new("Resetar",          hk.Reset),
            new("Repetir",          hk.ToggleRepeat),
        };
    }

    public void ApplyTo(TimerHotkeys hk)
    {
        hk.Start        = Slots[0].Binding;
        hk.PauseResume  = Slots[1].Binding;
        hk.Reset        = Slots[2].Binding;
        hk.ToggleRepeat = Slots[3].Binding;
    }
}
