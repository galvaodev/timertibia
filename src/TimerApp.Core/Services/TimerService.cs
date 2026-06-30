namespace TimerApp.Core.Services;

using System.Timers;
using TimerApp.Core.Models;

// "sealed" = não pode ser herdada — como um componente React final
// Implementa a interface ITimerService
public sealed class TimerService : ITimerService, IDisposable
{
    // Dictionary = Map do JavaScript
    // chave: Id do timer | valor: instância do timer
    private readonly Dictionary<string, TimerInstance> _timers = new();

    // O timer do sistema que vai "bater" a cada 1 segundo
    // É UMA instância só que gerencia TODOS os nossos timers
    // Pensa como um setInterval global
    private readonly Timer _systemTimer;

    // Lock = proteção contra condição de corrida
    // Quando múltiplos timers rodam ao mesmo tempo, podem tentar
    // acessar o Dictionary simultaneamente — o lock evita isso
    // Não tem equivalente direto no JS (JS é single-thread)
    private readonly object _lock = new();

    public event EventHandler<TimerInstance>? AnyTimerElapsed;

    // IReadOnlyList = array read-only — a UI pode ler mas não modificar
    public IReadOnlyList<TimerInstance> ActiveTimers
    {
        get
        {
            lock (_lock)
                return _timers.Values.ToList();
        }
    }

    public TimerService()
    {
        // Cria o timer do sistema que vai chamar OnSystemTick a cada 1s
        _systemTimer = new Timer(1000); // 1000ms = 1 segundo
        _systemTimer.Elapsed  += OnSystemTick;
        _systemTimer.AutoReset = true;  // repete automaticamente
        _systemTimer.Start();
    }

    public TimerInstance Start(TimerConfig config)
    {
        var instance = new TimerInstance(config);

        // Assina o evento Elapsed de cada timer individual
        // Quando um timer acabar, repassamos pro evento global
        instance.Elapsed += (sender, timer) =>
            AnyTimerElapsed?.Invoke(this, timer);

        lock (_lock)
            _timers[config.Id] = instance;

        instance.Start();
        return instance;
    }

    public void Pause(string timerId)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(timerId, out var timer))
                timer.Pause();
        }
    }

    public void Resume(string timerId)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(timerId, out var timer))
                timer.Start();
        }
    }

    public void Stop(string timerId)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(timerId, out var timer))
            {
                timer.Reset();
                _timers.Remove(timerId);
            }
        }
    }

    public void Reset(string timerId)
    {
        lock (_lock)
        {
            if (_timers.TryGetValue(timerId, out var timer))
            {
                var wasPaused = timer.State == TimerState.Paused;
                timer.Reset();
                if (!wasPaused)
                    timer.Start();
            }
        }
    }

    public void StopAll()
    {
        lock (_lock)
        {
            foreach (var timer in _timers.Values)
                timer.Reset();

            _timers.Clear();
        }
    }

    // Esse método roda a cada 1 segundo (chamado pelo _systemTimer)
    // É o "setInterval" do nosso app — avança todos os timers de uma vez
    private void OnSystemTick(object? sender, ElapsedEventArgs e)
    {
        List<TimerInstance> snapshot;

        // Copia a lista com lock, depois processa fora do lock
        // Boa prática: manter o lock o menor tempo possível
        lock (_lock)
            snapshot = _timers.Values.ToList();

        foreach (var timer in snapshot)
            timer.Advance(TimeSpan.FromSeconds(1));
    }

    // IDisposable = interface que permite limpeza de recursos
    // Equivalente ao cleanup do useEffect do React:
    // useEffect(() => { ... return () => cleanup() }, [])
    public void Dispose()
    {
        _systemTimer.Stop();
        _systemTimer.Dispose();
        StopAll();
    }
}
