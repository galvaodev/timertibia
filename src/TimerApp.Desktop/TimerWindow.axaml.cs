namespace TimerApp.Desktop;

using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using TimerApp.Desktop.ViewModels;

public partial class TimerWindow : Window
{
    public TimerWindow() : this(new MainViewModel()) { }

    public TimerWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Pin_Click(object? sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        var color = Topmost ? "#d8b06a" : "#3a414c";
        var brush = new SolidColorBrush(Color.Parse(color));
        PinIcon.Foreground    = brush;
        PinButton.BorderBrush = brush;
    }

    private void Opacity50_Click(object? sender, RoutedEventArgs e)  => SetOpacity(0.50);
    private void Opacity60_Click(object? sender, RoutedEventArgs e)  => SetOpacity(0.60);
    private void Opacity70_Click(object? sender, RoutedEventArgs e)  => SetOpacity(0.70);
    private void Opacity90_Click(object? sender, RoutedEventArgs e)  => SetOpacity(0.90);
    private void Opacity100_Click(object? sender, RoutedEventArgs e) => SetOpacity(1.00);

    private void SetOpacity(double value)
    {
        Opacity = value;

        var active = new SolidColorBrush(Color.Parse("#d8b06a"));
        var normal = new SolidColorBrush(Color.Parse("#2a313c"));

        MarkButton(Btn50,  value, 0.50, active, normal);
        MarkButton(Btn60,  value, 0.60, active, normal);
        MarkButton(Btn70,  value, 0.70, active, normal);
        MarkButton(Btn90,  value, 0.90, active, normal);
        MarkButton(Btn100, value, 1.00, active, normal);

        // Update active CSS class on all buttons
        foreach (var (btn, target) in new[] {
            (Btn50, 0.50), (Btn60, 0.60), (Btn70, 0.70),
            (Btn90, 0.90), (Btn100, 1.00) })
        {
            if (Math.Abs(value - target) < 0.01)
                btn.Classes.Add("active");
            else
                btn.Classes.Remove("active");
        }
    }

    private void OpenMain_Click(object? sender, RoutedEventArgs e)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime
            as IClassicDesktopStyleApplicationLifetime;

        if (lifetime?.MainWindow is not MainWindow mainWindow) return;

        if (DataContext is MainViewModel vm)
            vm.GoToNewTab();

        mainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
        mainWindow.Show();
        mainWindow.Activate();
    }

    private static void MarkButton(
        Button btn, double current, double target,
        IBrush active, IBrush normal)
    {
        var isActive = Math.Abs(current - target) < 0.01;
        btn.BorderBrush = isActive ? active : normal;
        btn.Foreground  = isActive ? active : normal;
    }
}
