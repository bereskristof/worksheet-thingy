using Docnet.Core;
using Storage.Sheet;

namespace Storage;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class MultiExamBuilder(SelectorNode rootNode, byte answerCount, uint examCount)
{
    private readonly List<ExamBuilder> _workingExamBuilders = [];
    
    public void ExportCombinedPdf()
    {
        SaveAndExportAllPdfs();
        var pdfPaths = _workingExamBuilders.Select(x => x.GetExportPath()).ToArray();
        MergePdfs(pdfPaths, Environment.ExpandEnvironmentVariables("%homepath%/Desktop/final.pdf")); // TODO: Make this configurable
        CleanUp();
    }
    
    private void SaveAndExportAllPdfs()
    {
        for (int i = 0; i < examCount; i++)
        {
            var examBuilder = new ExamBuilder(rootNode, answerCount);
            // examBuilder.Store();
            examBuilder.ExportPdf();
            var exportPath = examBuilder.GetExportPath();
            Console.WriteLine("Exported {0} of {1}", i + 1, examCount);
            _workingExamBuilders.Add(examBuilder);
        }
    }
    
    private static void MergePdfs(string[] pdfPaths, string exportPath)
    {
        if (pdfPaths.Length < 2)
        {
            File.Copy(pdfPaths[0], exportPath);
        }

        var mergedPdf = DocLib.Instance.Merge(pdfPaths[0], pdfPaths[1]);
        foreach (var path in pdfPaths[2..])
        {
            var pdfBytes = File.ReadAllBytes(path);
            mergedPdf = DocLib.Instance.Merge(mergedPdf, pdfBytes);
        }
        
        File.WriteAllBytes(exportPath, mergedPdf);
    }

    private void CleanUp()
    {
        var i = 0;
        foreach (var examBuilder in _workingExamBuilders)
        {
            examBuilder.CleanUp();
            Console.WriteLine("Cleaned up: {0}", ++i);
        }
    }
}