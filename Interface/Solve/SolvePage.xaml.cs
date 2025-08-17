using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using Docnet.Core.Models;
using Interface.Password;
using Microsoft.Win32;

namespace Interface.Solve;

public partial class SolvePage
{
    private const int DimX = 1200;
    private const int DimY = 1700;
    
    private readonly BackgroundWorker _backgroundWorker = new();
    private string _csvBuffer = string.Empty;
    
    public SolvePage()
    {
        InitializeComponent();
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        _backgroundWorker.DoWork += BackgroundLoader_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
    }

    private void ImportPdf_OnClick(object sender, RoutedEventArgs e)
    {
        string path = GetLoadPath(Interface.Resources.Lang.Import_Title);
        if (string.IsNullOrEmpty(path)) 
            return;
        
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        ExportResultsButton.IsEnabled = false;
        _backgroundWorker.WorkerReportsProgress = true;
        ProgressBar.Maximum = GetPageCount(path);
        _backgroundWorker.RunWorkerAsync(argument: path);
    }

    private void ExportResults_OnClick(object sender, RoutedEventArgs e)
    {
        var exportPath = GetSavePath(Interface.Resources.Lang.Export_Title);
        if (string.IsNullOrEmpty(exportPath)) 
            return;
        try
        {
            File.WriteAllText(exportPath, _csvBuffer, Encoding.UTF8);
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
    
    private int GetPageCount(string path)
    {
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));
        return reader.GetPageCount();
    }

    private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        ProgressBar.Value = e.ProgressPercentage;
    }

    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var path = (string)(e.Argument ?? "");
        
        var results = new List<string>();
        results.Add("Oldal, Neptun kód, Pontok, Eredmény, Részpontok");
        using var doclib = Docnet.Core.DocLib.Instance;
        using var reader = doclib.GetDocReader(path, new PageDimensions(DimX, DimY));

        var pageCount = reader.GetPageCount();
    
        for (var i = 0; i < pageCount; i++)
        {
            ScannerHandler.ScanPdfPage(results, reader, i);
            _backgroundWorker.ReportProgress(i + 1);
        }
        var finalResult = string.Join("\n", results);
        e.Result = finalResult;
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        string result = (string)(e.Result ?? string.Empty);
        _csvBuffer = result;
        ExportResultsButton.IsEnabled = true;
        ProgressBar.Visibility = Visibility.Hidden;
    }
}