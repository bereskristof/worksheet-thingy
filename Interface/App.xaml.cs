using System.Globalization;
using System.Windows;
using Microsoft.Win32;

namespace Interface;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SetLocale();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Storage.Manager.CloseDatabase();
    }

    private static void SetLocale()
    {
        string systemCulture = CultureInfo.InstalledUICulture.ToString();
        var key = Registry.CurrentUser.OpenSubKey(Interface.Resources.RegistryNames.KeyPath);
        string targetCulture = key?.GetValue(Interface.Resources.RegistryNames.ValueLocale, systemCulture) as string ?? systemCulture;
        var culture = new CultureInfo(targetCulture);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}