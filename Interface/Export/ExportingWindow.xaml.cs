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
        if (Math.Abs(e.ProgressPercentage - ExportProgressBar.Maximum) < 1e-6)
        {
            ExportProgressBar.IsIndeterminate = true;
        }
        ExportProgressBar.Value = e.ProgressPercentage;
        Counter.Content = $"{e.ProgressPercentage} / {ExportProgressBar.Maximum}";
    }

    private void BackgroundLoader_DoWork(object? sender, DoWorkEventArgs e)
    {
        var (target, root, answerCount, examCount, title, author, date) = (WorkArgs)e.Argument!;
        
        var latexBuilder = new LatexBuilder(title, author, date);
        MultiExamBuilder.BeginManualAdding(latexBuilder);
        for (uint i = 0; i < examCount - 1; i++)
        {
            try
            {
                MultiExamBuilder.AddExam(latexBuilder, root, answerCount);
            }
            catch (InvalidOperationException ex)
            {
                e.Result = ex.Message;
                return; // Stop the export if an error occurs
            }
            latexBuilder.Macro("newpage");
            _backgroundWorker.ReportProgress((int)(i + 1));
        }
        try
        {
            MultiExamBuilder.AddExam(latexBuilder, root, answerCount); // Add the last exam without a new page after it
        }
        catch (InvalidOperationException ex)
        {
            e.Result = ex.Message;
            return; // Stop the export if an error occurs
        }
        _backgroundWorker.ReportProgress((int)examCount);
        MultiExamBuilder.EndManualAdding(latexBuilder);
        
        var exportPath = MultiExamBuilder.TryExportPdf(latexBuilder, out var success, out var errorMessage);
        if (success)
        {
            File.Move(exportPath, Environment.ExpandEnvironmentVariables("%homepath%/Desktop/final.pdf"), true); // TODO: Get from UI
        }
        // TODO: Handle errorMessage properly
        MultiExamBuilder.CleanUp(exportPath);
        e.Result = success ? null : errorMessage;
    }

    private void BackgroundLoader_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        string? result = (string?)e.Result;
        if (result != null)
        {
            MessageBox.Show(result, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else
        {
            MessageBox.Show("Export completed successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information); // TODO: Localize
        }
        Close();
    }
}