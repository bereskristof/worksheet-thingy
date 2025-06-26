using System.Windows;

namespace Interface;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Storage.Manager.OpenDatabase("demo.wstkdb");
        base.OnStartup(e);
#if DEBUG
        SetDebugLocale();
#endif
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        Storage.Manager.CloseDatabase();
    }

#if DEBUG
    private static void SetDebugLocale()
    {
        string debugCulture = Environment.GetEnvironmentVariable("WPF_DEBUG_CULTURE") ?? "en-US";
        Console.WriteLine(debugCulture);
        var culture = new System.Globalization.CultureInfo(debugCulture);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
#endif
}