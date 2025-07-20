using Docnet.Core;
using Storage.Sheet;

namespace Storage;

// TODO: Make this use a single TeX file with multiple pages instead of multiple TeX files.
// Would increase performance and reduce disk usage.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class MultiExamBuilder(SelectorNode rootNode, byte answerCount, uint examCount)
{
    private readonly List<ExamBuilder> _workingExamBuilders = [];
    
    [Obsolete]
    public void ExportCombinedPdf(string targetPath)
    {
        SaveAndExportAllPdfs();
        var pdfPaths = _workingExamBuilders.Select(x => x.GetExportPath()).ToArray();
        MergePdfs(pdfPaths, targetPath);
        CleanUp();
    }
    
    [Obsolete]
    private void SaveAndExportAllPdfs()
    {
        for (int i = 0; i < examCount; i++)
        {
        }
    }
    
    [Obsolete]
    private static void MergePdfs(string[] pdfPaths, string exportPath)
    {
        if (pdfPaths.Length < 2)
        {
            File.Copy(pdfPaths[0], exportPath);
        }

        var mergedPdf = DocLib.Instance.Merge(pdfPaths[0], pdfPaths[1]);
        foreach (var path in pdfPaths[2..])
        {
        }
        
        File.WriteAllBytes(exportPath, mergedPdf);
    }

    // A state variable would certainly be more elegant, but this should eventually get completely replaced anyway. (TODO)
    /// Just read the implementation, it's shorter than explaining it.
    public string ExportNewPdf()
    {
        var examBuilder = new ExamBuilder(rootNode, answerCount);
        // examBuilder.Store();
        examBuilder.ExportPdf();
        var exportPath = examBuilder.GetExportPath();
        _workingExamBuilders.Add(examBuilder);
        return examBuilder.GetExportPath();
    }

    /// Appends a PDF to an existing PDF byte array.
    public byte[] MergePdfs(byte[] existingPdf, string pathToPdfToAppend)
    {
        var pdfBytes = File.ReadAllBytes(pathToPdfToAppend);
        existingPdf = DocLib.Instance.Merge(existingPdf, pdfBytes);
        return existingPdf;
    }

    /// Cleans up all exam builders, removing their temporary files.
    public void CleanUp()
    {
        var i = 0;
        foreach (var examBuilder in _workingExamBuilders)
        {
            examBuilder.CleanUp();
            Console.WriteLine("Cleaned up: {0}", ++i);
        }
    }
}