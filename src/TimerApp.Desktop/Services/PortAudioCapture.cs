namespace TimerApp.Desktop.Services;

using System;
using System.Runtime.InteropServices;
using PortAudioSharp;

public sealed class PortAudioCapture : IAudioCapture
{
    private const double SampleRate      = 16000.0;
    private const uint   FramesPerBuffer = 4000; // ~250ms

    private Stream?          _stream;
    private Stream.Callback? _callback; // held to prevent GC

    public event Action<byte[], int>? DataAvailable;

    public void Start()
    {
        PortAudio.LoadNativeLibrary();
        PortAudio.Initialize();

        int device = PortAudio.DefaultInputDevice;
        if (device < 0) throw new InvalidOperationException("Nenhum dispositivo de entrada encontrado.");

        var info = PortAudio.GetDeviceInfo(device);

        var inputParams = new StreamParameters
        {
            device                    = device,
            channelCount              = 1,
            sampleFormat              = SampleFormat.Int16,
            suggestedLatency          = info.defaultLowInputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero
        };

        _callback = (input, _, frameCount, ref _, _, _) =>
        {
            var bytes = new byte[frameCount * 2];
            Marshal.Copy(input, bytes, 0, bytes.Length);
            DataAvailable?.Invoke(bytes, bytes.Length);
            return StreamCallbackResult.Continue;
        };

        _stream = new Stream(
            inParams:        inputParams,
            outParams:       null,
            sampleRate:      SampleRate,
            framesPerBuffer: FramesPerBuffer,
            streamFlags:     StreamFlags.NoFlag,
            callback:        _callback,
            userData:        null);

        _stream.Start();
    }

    public void Stop()
    {
        try
        {
            _stream?.Stop();
            _stream?.Close();
            _stream?.Dispose();
        }
        catch { /* safe */ }
        _stream   = null;
        _callback = null;

        try { PortAudio.Terminate(); } catch { /* safe */ }
    }

    public void Dispose() => Stop();
}
