using System.ComponentModel;

namespace Interface;

public partial class MainWindow
{
    private MainEditor? _mainEditor;
    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        _mainEditor?.OnExit();
    }

    private void PasswordEntry_OnPasswordUnlocked(object? sender, EventArgs e)
    {
        _mainEditor = new MainEditor();
        Main.Children.Remove(PasswordEntry);
        Main.Children.Add(_mainEditor);
    }
}