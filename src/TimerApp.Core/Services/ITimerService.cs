namespace TimerApp.Core.Services;

using TimerApp.Core.Models;

// Interface = contrato TypeScript
// TS: interface ITimerService { start(config: TimerConfig): TimerInstance; ... }
//
// Por que interface? Porque no futuro podemos ter um FakeTimerService
// pra testes sem precisar mudar nada no resto do código
public interface ITimerService
{
    // Retorna todos os timers ativos no momento
    IReadOnlyList<TimerInstance> ActiveTimers { get; }

    // Inicia um timer e o retorna — pode ter vários rodando ao mesmo tempo
    TimerInstance Start(TimerConfig config);

    // Pausa um timer específico pelo Id
    void Pause(string timerId);

    // Retoma um timer pausado
    void Resume(string timerId);

    // Para e remove um timer
    void Stop(string timerId);

    // Reinicia o tempo sem remover o timer da lista
    void Reset(string timerId);

    // Para todos de uma vez
    void StopAll();

    // Evento global — qualquer timer que acabar dispara esse evento
    // A UI assina esse evento pra tocar som e mostrar notificação
    event EventHandler<TimerInstance> AnyTimerElapsed;
}