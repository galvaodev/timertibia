namespace TimerApp.Core.Services;

public interface ISoundService
{
    // Toca um arquivo de som pelo caminho
    void Play(string soundFilePath);

    // Para qualquer som tocando
    void Stop();

    // Volume de 0.0 a 1.0
    float Volume { get; set; }
}