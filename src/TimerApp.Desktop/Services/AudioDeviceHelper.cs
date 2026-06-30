namespace TimerApp.Desktop.Services;

using System.Collections.Generic;
using PortAudioSharp;
using TimerApp.Desktop.Models;

#if WINDOWS
using NAudio.Wave;
#endif

public static class AudioDeviceHelper
{
    public static List<AudioDevice> GetInputDevices()
    {
        var devices = new List<AudioDevice>
        {
            new(-1, "Padrão do sistema")
        };

        try
        {
#if WINDOWS
            for (int i = 0; i < WaveIn.DeviceCount; i++)
                devices.Add(new AudioDevice(i, WaveIn.GetCapabilities(i).ProductName));
#else
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
#endif
        }
        catch { /* se falhar, retorna só o padrão */ }

        return devices;
    }
}
