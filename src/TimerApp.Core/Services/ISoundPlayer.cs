namespace TimerApp.Core.Services;

// Interface interna — só usada dentro do SoundService
// Cada SO tem sua própria implementação
internal interface ISoundPlayer
{
    void Play(string filePath, float volume);
    void Stop();
    void SetVolume(float volume);
}