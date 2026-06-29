using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using TimerApp.Desktop.ViewModels;
using Velopack;
using Velopack.Sources;

namespace TimerApp.Desktop;

public partial class App : Application
{
    private static UpdateManager? _updateManager;
    private static UpdateInfo?    _pendingUpdate;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!EulaWindow.IsAccepted)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                var eula = new EulaWindow();
                desktop.MainWindow = eula;
                eula.Closed += (_, _) =>
                {
                    if (!EulaWindow.IsAccepted)
                    {
                        desktop.Shutdown();
                        return;
                    }
                    var main = new MainWindow();
                    desktop.MainWindow = main;
                    desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnLastWindowClose;
                    main.Show();
                    _ = CheckForUpdatesAsync(main);
                };
            }
            else
            {
                desktop.MainWindow = new MainWindow();
                _ = CheckForUpdatesAsync(desktop.MainWindow);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Chamado pelo MainViewModel quando o usuário clica "Atualizar agora"
    public static void StartUpdate(Action<int> onProgress)
    {
        if (_pendingUpdate is null || _updateManager is null) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await _updateManager.DownloadUpdatesAsync(_pendingUpdate, onProgress);
                _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
            }
            catch { }
        });
    }

    private static async Task CheckForUpdatesAsync(Window mainWindow)
    {
        try
        {
            var source = new GithubSource(AppConfig.GitHubRepoUrl, null, false);
            _updateManager = new UpdateManager(source);

            if (!_updateManager.IsInstalled) return; // rodando em dev, sem update

            _pendingUpdate = await _updateManager.CheckForUpdatesAsync();
            if (_pendingUpdate is null) return;

            var version = _pendingUpdate.TargetFullRelease.Version.ToString();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (mainWindow.DataContext is MainViewModel vm)
                {
                    vm.UpdateVersion   = version;
                    vm.UpdateAvailable = true;
                }
            });
        }
        catch { }
    }
}
