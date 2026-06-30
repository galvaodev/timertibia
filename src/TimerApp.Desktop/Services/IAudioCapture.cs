namespace TimerApp.Desktop.Services;

using System;

public interface IAudioCapture : IDisposable
{
    void Start(int deviceIndex = -1);
    void Stop();
    event Action<byte[], int>? DataAvailable; // buffer, bytesRecorded
}
