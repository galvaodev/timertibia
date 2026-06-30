namespace TimerApp.Desktop.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Input;
using SharpHook.Native;
using TimerApp.Core.Models;

public class HotkeyBinding
{
    public bool    Ctrl  { get; set; }
    public bool    Alt   { get; set; }
    public bool    Shift { get; set; }
    public KeyCode Code  { get; set; } = KeyCode.VcUndefined;

    [JsonIgnore] public bool   IsSet   => Code != KeyCode.VcUndefined;
    [JsonIgnore] public string Display => IsSet ? BuildDisplay() : "—";

    public HotkeyBinding Clone() => new() { Ctrl = Ctrl, Alt = Alt, Shift = Shift, Code = Code };

    public bool Matches(KeyCode code, bool ctrl, bool alt, bool shift)
        => IsSet && Code == code && Ctrl == ctrl && Alt == alt && Shift == shift;

    private string BuildDisplay()
    {
        var parts = new List<string>(4);
        if (Ctrl)  parts.Add("Ctrl");
        if (Alt)   parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        var name = Code.ToString();
        parts.Add(name.StartsWith("Vc") ? name[2..] : name);
        return string.Join("+", parts);
    }

    public static readonly Dictionary<Key, KeyCode> AvaloniaMap = new()
    {
        { Key.F1, KeyCode.VcF1 }, { Key.F2, KeyCode.VcF2 }, { Key.F3, KeyCode.VcF3 },
        { Key.F4, KeyCode.VcF4 }, { Key.F5, KeyCode.VcF5 }, { Key.F6, KeyCode.VcF6 },
        { Key.F7, KeyCode.VcF7 }, { Key.F8, KeyCode.VcF8 }, { Key.F9, KeyCode.VcF9 },
        { Key.F10, KeyCode.VcF10 }, { Key.F11, KeyCode.VcF11 }, { Key.F12, KeyCode.VcF12 },
        { Key.A, KeyCode.VcA }, { Key.B, KeyCode.VcB }, { Key.C, KeyCode.VcC },
        { Key.D, KeyCode.VcD }, { Key.E, KeyCode.VcE }, { Key.F, KeyCode.VcF },
        { Key.G, KeyCode.VcG }, { Key.H, KeyCode.VcH }, { Key.I, KeyCode.VcI },
        { Key.J, KeyCode.VcJ }, { Key.K, KeyCode.VcK }, { Key.L, KeyCode.VcL },
        { Key.M, KeyCode.VcM }, { Key.N, KeyCode.VcN }, { Key.O, KeyCode.VcO },
        { Key.P, KeyCode.VcP }, { Key.Q, KeyCode.VcQ }, { Key.R, KeyCode.VcR },
        { Key.S, KeyCode.VcS }, { Key.T, KeyCode.VcT }, { Key.U, KeyCode.VcU },
        { Key.V, KeyCode.VcV }, { Key.W, KeyCode.VcW }, { Key.X, KeyCode.VcX },
        { Key.Y, KeyCode.VcY }, { Key.Z, KeyCode.VcZ },
        { Key.D0, KeyCode.Vc0 }, { Key.D1, KeyCode.Vc1 }, { Key.D2, KeyCode.Vc2 },
        { Key.D3, KeyCode.Vc3 }, { Key.D4, KeyCode.Vc4 }, { Key.D5, KeyCode.Vc5 },
        { Key.D6, KeyCode.Vc6 }, { Key.D7, KeyCode.Vc7 }, { Key.D8, KeyCode.Vc8 },
        { Key.D9, KeyCode.Vc9 },
        { Key.NumPad0, KeyCode.VcNumPad0 }, { Key.NumPad1, KeyCode.VcNumPad1 },
        { Key.NumPad2, KeyCode.VcNumPad2 }, { Key.NumPad3, KeyCode.VcNumPad3 },
        { Key.NumPad4, KeyCode.VcNumPad4 }, { Key.NumPad5, KeyCode.VcNumPad5 },
        { Key.NumPad6, KeyCode.VcNumPad6 }, { Key.NumPad7, KeyCode.VcNumPad7 },
        { Key.NumPad8, KeyCode.VcNumPad8 }, { Key.NumPad9, KeyCode.VcNumPad9 },
        { Key.Space,    KeyCode.VcSpace    },
        { Key.Delete,   KeyCode.VcDelete   },
        { Key.Insert,   KeyCode.VcInsert   },
        { Key.Home,     KeyCode.VcHome     },
        { Key.End,      KeyCode.VcEnd      },
        { Key.PageUp,   KeyCode.VcPageUp   },
        { Key.PageDown, KeyCode.VcPageDown },
        { Key.Left,     KeyCode.VcLeft     },
        { Key.Right,    KeyCode.VcRight    },
        { Key.Up,       KeyCode.VcUp       },
        { Key.Down,     KeyCode.VcDown     },
    };
}

public class TimerHotkeys
{
    public HotkeyBinding Start        { get; set; } = new();
    public HotkeyBinding PauseResume  { get; set; } = new();
    public HotkeyBinding Reset        { get; set; } = new();
    public HotkeyBinding ToggleRepeat { get; set; } = new();
}

public class HotkeyConfig
{
    public TimerHotkeys  Food         { get; set; } = new();
    public TimerHotkeys  Potion       { get; set; } = new();
    public TimerHotkeys  Boost        { get; set; } = new();
    public TimerHotkeys  Custom       { get; set; } = new();
    public HotkeyBinding VoiceToggle  { get; set; } = new();

    private static readonly string ConfigFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TibiaTimer", "hotkeys.json");

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Converters    = { new JsonStringEnumConverter() }
    };

    public static HotkeyConfig Load()
    {
        if (!File.Exists(ConfigFile)) return new();
        try { return JsonSerializer.Deserialize<HotkeyConfig>(File.ReadAllText(ConfigFile), Opts) ?? new(); }
        catch { return new(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile)!);
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(this, Opts));
    }

    public TimerHotkeys For(TimerCategory cat) => cat switch
    {
        TimerCategory.Food   => Food,
        TimerCategory.Boost  => Boost,
        TimerCategory.Potion => Potion,
        _                    => Custom
    };
}
