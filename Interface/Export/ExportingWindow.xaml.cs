using System.ComponentModel;
using System.IO;
using System.Windows;
using Storage;
using Storage.Sheet;
using WorkArgs = System.Tuple<string, Storage.Sheet.SelectorNode, byte, uint>;

namespace Interface.Export;

public partial class ExportingWindow
{
    private readonly BackgroundWorker _backgroundWorker = new();
    
    public ExportingWindow()
    {
        InitializeComponent();
        WindowStyle = WindowStyle.None;
    }
    
    public void ExportNPages(SelectorNode root, uint examCount, byte answerCount)
    {
        var target = Environment.ExpandEnvironmentVariables("%homepath%/Desktop/final.pdf");
        _backgroundWorker.WorkerReportsProgress = true;
        // backgroundWorker.WorkerSupportsCancellation = true;
        ExportProgressBar.Maximum = CalculateMaxProgress(examCount);
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        _backgroundWorker.DoWork += BackgroundLoader_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundLoader_RunWorkerCompleted;
        var args = new WorkArgs(target, root, answerCount, examCount);
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
        var (target, root, answerCount, examCount) = (WorkArgs)e.Argument!;
        var multipleExportPage = new MultiExamBuilder(root, answerCount, examCount);
        
        // First always exports a byte array
        var firstPath = multipleExportPage.ExportNewPdf();
        byte[] combinedPdf = File.ReadAllBytes(firstPath);
        _backgroundWorker.ReportProgress(1);
        
        for (int i = 0; i < examCount; i++)
        {
            var path = multipleExportPage.ExportNewPdf();
            combinedPdf = multipleExportPage.MergePdfs(combinedPdf, path);
            _backgroundWorker.ReportProgress(i + 1);
        }
        
        multipleExportPage.CleanUp();
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        MessageBox.Show("Export completed successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }
}