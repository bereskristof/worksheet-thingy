using Storage.Sheet;

namespace Storage;

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
        _builder.AutoHeader("Test Title", "Author Name", "Date"); // TODO: Replace with obtained values
        foreach (var question in questions)
        {
            var answers = question.Answers.GetRandomAnswers(answerCount);
            Tuple<long, long[]> skeleton = new Tuple<long, long[]>(question.Id, answers.Select(x => x.Id).ToArray()); // Store IDs to allow for reconstruction
            _skeleton = _skeleton.Append(skeleton).ToArray();
            _builder.Question(
                question.Text,
                question.Points, 
                answers.Select(a => a.Text).ToArray()
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

    public void Export(string? filename = null)
    {
        if (_exportPath != null)
        {
            Log.Write("Export: Export path is already set, cannot export again.", Log.Severity.Error);
            return;
        }
        filename ??= Path.Combine(Path.GetTempPath(), "ExamBuilder", $"{_id}.tex");
        Directory.CreateDirectory(Path.GetDirectoryName(filename) ?? throw new InvalidOperationException("Invalid directory name"));
        _exportPath = filename;
        using var writer = new StreamWriter(filename);
        writer.Write(_builder.Finish());
        writer.Close();
    }

    public void CleanUp()
    {
        if (_exportPath == null)
        {
            Log.Write("CleanUp: Export path is empty.", Log.Severity.Error);
            return;
        }
        if (File.Exists(_exportPath))
        {
            File.Delete(_exportPath);
            Log.Write($"CleanUp: Cleaned up export file: {_exportPath}");
        }
        else
        {
            Log.Write($"CleanUp: Export file does not exist: {_exportPath}", Log.Severity.Warning);
        }
    }
}