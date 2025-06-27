using System.ComponentModel;
using System.Windows;

namespace Interface.Password;

public partial class PasswordPage
{
    public event EventHandler? PasswordUnlocked;
    private readonly BackgroundWorker _backgroundLoader = new();
    private bool _lastUnlockResult;
    
    public PasswordPage()
    {
        InitializeComponent();
        _backgroundLoader.DoWork += BackgroundLoader_DoWork;
        _backgroundLoader.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        MainPasswordBox.Focus();
    }

    private void MainPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        IncorrectLabel.Visibility = Visibility.Hidden;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        string password = MainPasswordBox.Password;
        LoadingBar.Visibility = Visibility.Visible;
        _backgroundLoader.RunWorkerAsync(password);
    }
    
    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        string password = e.Argument?.ToString() ?? string.Empty;
        _lastUnlockResult = Storage.Encryption.TryPassword(password);
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        LoadingBar.Visibility = Visibility.Hidden;
        if (_lastUnlockResult)
            PasswordUnlocked?.Invoke(this, EventArgs.Empty);
        else
            IncorrectLabel.Visibility = Visibility.Visible;
    }

    private void NewButton_OnClick(object sender, RoutedEventArgs e)
    {
        PasswordMode.Visibility = Visibility.Collapsed;
        ImportMode.Visibility = Visibility.Collapsed;
        CreateMode.Visibility = Visibility.Visible;
    }

    private void ImportButton_OnClick(object sender, RoutedEventArgs e)
    {
        PasswordMode.Visibility = Visibility.Collapsed;
        ImportMode.Visibility = Visibility.Visible;
        CreateMode.Visibility = Visibility.Collapsed;
    }
}