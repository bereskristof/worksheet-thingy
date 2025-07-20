using Docnet.Core;
using Storage.Sheet;

namespace Storage;

// TODO: Make this use a single TeX file with multiple pages instead of multiple TeX files.
// Would increase performance and reduce disk usage.
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class MultiExamBuilder(SelectorNode rootNode, byte answerCount, string title, string author, string date)
{
    private readonly List<ExamBuilder> _workingExamBuilders = [];

    // A state variable would certainly be more elegant, but this should eventually get completely replaced anyway. (TODO)
    /// Just read the implementation, it's shorter than explaining it.
    public string ExportNewPdf()
    {
        var examBuilder = new ExamBuilder(rootNode, answerCount, title, author, date);
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