using System.ComponentModel;
using Interface.Task;

namespace Interface;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        TaskPage.OnExit();
    }
}