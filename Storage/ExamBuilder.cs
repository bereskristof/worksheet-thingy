using System.Diagnostics;
using Storage.Sheet;

namespace Storage;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class ExamBuilder
{
    private readonly Guid _id = Guid.NewGuid();
    
    private readonly Tuple<long, long[]>[] _skeleton = [];
    private readonly LatexBuilder _builder;

    private string? _exportPath;
    
    public ExamBuilder(SelectorNode rootNode, byte answerCount)
    {
        var questions = rootNode.GetQuestions();
        
        _builder = new LatexBuilder();
        
        var backgroundPath = Path.Combine(Path.GetTempPath(), "ExamBuilder", $"{_id}.png");
        var backgroundBuilder = new BackgroundBuilder(_id.ToByteArray());
        backgroundBuilder.Save(backgroundPath);
        var backgroundRelativePath = Path.GetRelativePath(Path.GetDirectoryName(backgroundPath)!, backgroundPath);
        _builder.AutoHeader("Test Title", "Author Name", "Date", backgroundRelativePath); // TODO: Replace with obtained values
        
        foreach (var question in questions)
        {
            var answers = question.Answers.GetRandomAnswers(answerCount);
            Tuple<long, long[]> skeleton = new Tuple<long, long[]>(question.Id, answers.Select(x => x.Id).ToArray()); // Store IDs to allow for reconstruction
            _skeleton = _skeleton.Append(skeleton).ToArray();
            _builder.Question(
                question.Text,
                question.Points, 
                answers.Select(a => a.Text).ToArray(),
                question.FetchImageStream()?.ToArray()
            );
        }
        _builder.AutoFooter();
    }

    public void Store()
    {
        foreach (var (question, answers) in _skeleton)
        {
            foreach (var answer in answers)
            {
                var storeCommand = Manager.Connection.CreateCommand();
                storeCommand.CommandText = "INSERT INTO Solutions (Uuid, QuestionNumber, AnswerNumber) VALUES (@Uuid, @QuestionNumber, @AnswerNumber)";
                storeCommand.Parameters.AddWithValue("@Uuid", _id.ToString());
                storeCommand.Parameters.AddWithValue("@QuestionNumber", question);
                storeCommand.Parameters.AddWithValue("@AnswerNumber", answer);
                storeCommand.ExecuteNonQuery();
            }
        }
    }

    public void ExportTex(string? filename = null)
    {
        if (_exportPath != null)
        {
            Log.Write("ExportTex: Export path is already set, cannot export again.", Log.Severity.Error);
            return;
        }
        filename ??= Path.Combine(Path.GetTempPath(), "ExamBuilder", $"{_id}.tex");
        Directory.CreateDirectory(Path.GetDirectoryName(filename) ?? throw new InvalidOperationException("Invalid directory name"));
        _exportPath = filename;
        using var writer = new StreamWriter(filename);
        writer.Write(_builder.Finish());
        writer.Close();
    }
    
    public void ExportPdf(string? filename = null)
    {
        ExportTex();
        if (_exportPath == null)
        {
            Log.Write("ExportPdf: Export path is not set, cannot export PDF.", Log.Severity.Error);
            return;
        }

        CallPdfLatex();
        CallPdfLatex();
        
        _exportPath = Path.ChangeExtension(_exportPath, ".pdf");
        Log.Write($"ExportPdf: Exported PDF to {_exportPath}");
    }
    
    // Piece of fucking dogshit latexmk does nothing other than NOT kill its fucking children,
    // So I have to call all these cunts manually
    // Because obviously if you call pdflatex, you actually just want to somewhat kinda create a PDF file. Sometimes.
    private void CallPdfLatex()
    {
        var process = new Process();
        var flags = $"-halt-on-error -output-directory=\"{Path.GetDirectoryName(_exportPath)}\" \"{_exportPath}\"";
        process.StartInfo = new ProcessStartInfo("pdflatex", flags)
        { CreateNoWindow = true };
        process.Start();
        var finished = process.WaitForExit(30_000); // Wait for god knows how many this will be seconds for the process to complete
        var outputPath = Path.ChangeExtension(_exportPath, ".pdf");
        if (finished && process.ExitCode == 0 && File.Exists(outputPath)) return;
        process.Kill(true);
        var killed = process.WaitForExit(5_000);
        if (!killed)
        {
            Log.Write($"ExportPdf: pdflatex failed to complete, child process has refused to be killed, ABANDONING!", Log.Severity.Error);
            Environment.Exit(-90); // <--- Process was force abandoned due to extreme complications
        }
        Log.Write($"ExportPdf: pdflatex failed to complete, child process was killed", Log.Severity.Warning);
        throw new TimeoutException();
    }
    
    public string GetExportPath()
    {
        if (_exportPath != null) return _exportPath;
        Log.Write("GetExportPath: Export path is not set.", Log.Severity.Error);
        return string.Empty;
    }

    public void CleanUp()
    {
        if (_exportPath == null)
        {
            Log.Write("CleanUp: Export path is empty.", Log.Severity.Error);
            return;
        }

        var auxFiles = new[] { ".aux", ".log", ".out", ".toc", ".pdf", ".tex", ".png" };
        foreach (var ext in auxFiles)
        {
            var auxFile = Path.ChangeExtension(_exportPath, ext);
            if (!File.Exists(auxFile)) continue;
            File.Delete(auxFile);
            Log.Write($"CleanUp: Cleaned up auxiliary file: {auxFile}");
            _exportPath = Path.ChangeExtension(_exportPath, ".tex");
        }
    }

    public void CleanUpError()
    {
        CleanUp();
    }
}