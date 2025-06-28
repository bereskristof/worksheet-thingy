using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

namespace Interface;

public partial class MainWindow
{
    private MainEditor? _mainEditor;
    public MainWindow()
    {
        InitializeComponent();
        string path = GetDefaultDbPath();
        bool wasEmpty = false;
        if (path == string.Empty) {
            path = "!"; // HACK, an empty path is valid, but it shouldn't be
            wasEmpty = true;
        }
        bool result = Storage.Manager.OpenDatabase(path);
        if (result) return;
        if (wasEmpty) // The Key was empty, most likely the first time the program was run
        {
            PasswordEntry.SwapToNewMode();
        }
        else // The Key was not empty, either the file was moved, or it no longer exists, or maybe the registry was tampered with
        {
            MessageBox.Show(Interface.Resources.Lang.PasswordResult_DefaultFileMissing, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ClearDefaultDbPath();
            PasswordEntry.SwapToImportMode();
        }
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
    
    private static string GetDefaultDbPath()
    {
        RegistryKey? path = Registry.CurrentUser.OpenSubKey(Interface.Resources.RegistryNames.KeyPath);
        string defaultPath = path?.GetValue(Interface.Resources.RegistryNames.ValueDbPath) as string ?? string.Empty;
        return defaultPath;
    }
    
    private static void ClearDefaultDbPath()
    {
        var key = Registry.CurrentUser.OpenSubKey(Interface.Resources.RegistryNames.KeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);
        key?.SetValue(Interface.Resources.RegistryNames.ValueDbPath, string.Empty);
    }
}