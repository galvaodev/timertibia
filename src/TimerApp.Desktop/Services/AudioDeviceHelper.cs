namespace TimerApp.Desktop.Services;

using System.Collections.Generic;
using PortAudioSharp;
using TimerApp.Desktop.Models;

public static class AudioDeviceHelper
{
    public static List<AudioDevice> GetInputDevices()
    {
        var devices = new List<AudioDevice> { new(-1, "Padrão do sistema") };
        try
        {
            PortAudio.LoadNativeLibrary();
            PortAudio.Initialize();
            try
            {
                for (int i = 0; i < PortAudio.DeviceCount; i++)
                {
                    var info = PortAudio.GetDeviceInfo(i);
                    if (info.maxInputChannels > 0)
                        devices.Add(new AudioDevice(i, info.name));
                }
            }
            finally { PortAudio.Terminate(); }
        }
        catch { }
        return devices;
    }
}
