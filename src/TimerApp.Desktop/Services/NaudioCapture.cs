namespace TimerApp.Desktop.Services;

using System;
using NAudio.Wave;

public sealed class NaudioCapture : IAudioCapture
{
    private const int SampleRate = 16000;

    private WaveInEvent? _waveIn;

    public event Action<byte[], int>? DataAvailable;

    public void Start(int deviceIndex = -1)
    {
        // WaveIn.WAVE_MAPPER = -1 (padrão do sistema)
        _waveIn = new WaveInEvent
        {
            DeviceNumber      = deviceIndex,
            WaveFormat        = new WaveFormat(SampleRate, 16, 1),
            BufferMilliseconds = 250
        };
        _waveIn.DataAvailable += (_, e) => DataAvailable?.Invoke(e.Buffer, e.BytesRecorded);
        _waveIn.StartRecording();
    }

    public void Stop()
    {
        _waveIn?.StopRecording();
        _waveIn?.Dispose();
        _waveIn = null;
    }

    public void Dispose() => Stop();
}
