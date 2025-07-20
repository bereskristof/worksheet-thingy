using System.ComponentModel;
using System.IO;
using System.Windows;
using Storage;
using Storage.Sheet;
using WorkArgs = System.Tuple<string, Storage.Sheet.SelectorNode, byte, uint, string, string, string>;

namespace Interface.Export;

public partial class ExportingWindow
{
    private readonly BackgroundWorker _backgroundWorker = new();
    
    public ExportingWindow()
    {
        InitializeComponent();
        WindowStyle = WindowStyle.None;
    }
    
    public void ExportNPages(SelectorNode root, uint examCount, byte answerCount, string title, string author, string date)
    {
        var target = Environment.ExpandEnvironmentVariables("%homepath%/Desktop/final.pdf"); // TODO: Get from UI
        _backgroundWorker.WorkerReportsProgress = true;
        ExportProgressBar.Maximum = CalculateMaxProgress(examCount);
        Counter.Content = $"0 / {ExportProgressBar.Maximum}";
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        _backgroundWorker.DoWork += BackgroundLoader_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        var args = new WorkArgs(target, root, answerCount, examCount, title, author, date);
        _backgroundWorker.RunWorkerAsync(argument: args);
    }
    
    private int CalculateMaxProgress(uint examCount)
        => (int)examCount;

    private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        ExportProgressBar.Value = e.ProgressPercentage;
        Counter.Content = $"{e.ProgressPercentage} / {ExportProgressBar.Maximum}";
    }

    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var (target, root, answerCount, examCount, title, author, date) = (WorkArgs)e.Argument!;
        var multipleExportPage = new MultiExamBuilder(root, answerCount, title, author, date);
        
        // First always exports a byte array
        var firstPath = multipleExportPage.ExportNewPdf();
        byte[] combinedPdf = File.ReadAllBytes(firstPath);
        _backgroundWorker.ReportProgress(1);
        
        for (int i = 1; i < examCount; i++)
        {
            var path = multipleExportPage.ExportNewPdf();
            combinedPdf = multipleExportPage.MergePdfs(combinedPdf, path);
            _backgroundWorker.ReportProgress(i + 1);
        }
        
        File.WriteAllBytes(target, combinedPdf);
        multipleExportPage.CleanUp();
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        MessageBox.Show("Export completed successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }
}