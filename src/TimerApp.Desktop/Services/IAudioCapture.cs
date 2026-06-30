namespace TimerApp.Desktop.Services;

using System;

public interface IAudioCapture : IDisposable
{
    void Start();
    void Stop();
    event Action<byte[], int>? DataAvailable; // buffer, bytesRecorded
}
