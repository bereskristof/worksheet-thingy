using System.IO;
using System.Windows;
using Interface.Password;
using Microsoft.Win32;

namespace Interface.Solve;

public partial class SolvePage
{
    string csvBuffer = string.Empty;
    public SolvePage()
    {
        InitializeComponent();
    }

    private void ImportPdf_OnClick(object sender, RoutedEventArgs e)
    {
        ProgressBar.Visibility = Visibility.Visible;
        string path = GetLoadPath(Interface.Resources.Lang.Import_Title); // TODO: Move to separate thread
        if (string.IsNullOrEmpty(path)) 
            return;            
        Console.WriteLine(path);
        csvBuffer = ScannerHandler.ScanMultiPagePdf(path);
        ExportResultsButton.IsEnabled = true;
        ProgressBar.Visibility = Visibility.Hidden;
    }

    private void ExportResults_OnClick(object sender, RoutedEventArgs e)
    {
        var exportPath = GetSavePath(Interface.Resources.Lang.Export_Title);
        if (string.IsNullOrEmpty(exportPath)) 
            return;
        try
        {
            File.WriteAllText(exportPath, csvBuffer);
            MessageBox.Show("Export saved", Interface.Resources.Lang.Export_Title, MessageBoxButton.OK, MessageBoxImage.Information); // TODO: Localize
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not export:\n{ex.Message}", Interface.Resources.Lang.Export_Title, MessageBoxButton.OK, MessageBoxImage.Error); // TODO: Localize
        }
    }

    private void ExportStats_OnClick(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
    
    private static string GetLoadPath(string title)
    {
        OpenFileDialog openDialog = new OpenFileDialog
        {
            Filter = "Portable Document Format|*.pdf|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }
    
    private static string GetSavePath(string title)
    {
        SaveFileDialog openDialog = new SaveFileDialog
        {
            Filter = "Comma Separated Values|*.csv|All files|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            Title = title
        };
        
        return openDialog.ShowDialog() == true ? Path.GetFullPath(openDialog.FileName) : string.Empty;
    }
}