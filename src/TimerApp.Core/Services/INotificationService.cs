namespace TimerApp.Core.Services;

public interface INotificationService
{
    // Mostra uma notificação nativa do sistema operacional
    void Show(string title, string message);
}