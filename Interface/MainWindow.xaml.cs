using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Interface;

public partial class MainWindow
{
    private MainEditor? _mainEditor;
    public MainWindow()
    {
        Application.Current.DispatcherUnhandledException += DispatcherUnhandledException_Raised;
        InitializeComponent();
        HandleLoginUiDefaultPage();
    }

    private void HandleLoginUiDefaultPage()
    {
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
            PasswordEntry.SwapToNewMode(true);
        }
        else // The Key was not empty, either the file was moved, or it no longer exists, or maybe the registry was tampered with
        {
            MessageBox.Show(Interface.Resources.Lang.PasswordResult_DefaultFileMissing, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ClearDefaultDbPath();
            PasswordEntry.SwapToImportMode(true);
        }
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
    
    private void DispatcherUnhandledException_Raised(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Exception ex = e.Exception;
        while (ex.InnerException != null) 
            ex = ex.InnerException;
        string errorMessage = $"{Interface.Resources.Lang.Crash_Message}\n\n- Error message -\n{ex.Message}\n\n- Error stacktrace -\n{ex.StackTrace}";
        MessageBox.Show(errorMessage, Interface.Resources.Lang.Crash_Title, MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Close();
    }
}