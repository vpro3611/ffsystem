using System;
using System.Windows;
using FileSystemP.Core.Services;

namespace FileSystemP.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplyTheme();
        new KeyBindingSettingsService().EnsureSettingsFileIsValid();
    }

    private static void ApplyTheme()
    {
        bool isLight = true;
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        if (key?.GetValue("AppsUseLightTheme") is int value)
            isLight = value != 0;

        string theme = isLight ? "Light" : "Dark";
        var dict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{theme}.xaml", UriKind.Absolute)
        };
        Application.Current.Resources.MergedDictionaries[0] = dict;
    }
}
