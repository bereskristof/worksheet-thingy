using System.Windows;

namespace Interface;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Storage.Manager.CreateDatabase("demo.wstkdb");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Storage.Manager.CloseDatabase();
        base.OnExit(e);
    }
}