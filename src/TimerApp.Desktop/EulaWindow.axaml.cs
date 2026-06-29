namespace TimerApp.Desktop;

using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

public partial class EulaWindow : Window
{
    private static readonly string EulaFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TibiaTimer", "eula_accepted");

    public static bool IsAccepted => File.Exists(EulaFile);

    public EulaWindow()
    {
        InitializeComponent();
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(EulaFile)!);
        File.WriteAllText(EulaFile, DateTime.UtcNow.ToString("O"));
        Close();
    }

    private void Decline_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
