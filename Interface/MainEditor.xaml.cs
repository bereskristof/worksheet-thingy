using System.IO;
using System.Windows;

namespace Interface;

public partial class MainEditor
{
    public MainEditor() => InitializeComponent();

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var questions = Bindings.Instance.SheetRoot.GetQuestions();
        
        var taskSheet = new Storage.LatexBuilder();
        taskSheet.AutoHeader("Test Title", "Author Name", "Date");
        foreach (var question in questions)
            taskSheet.Question(
                question.Text,
                question.Points,
                question.Answers.GetRandomAnswers(5).Select(a => a.Text).ToArray()
                );
        taskSheet.AutoFooter();
        
        // Save the LaTeX document to a file or display it
        using var writer = new StreamWriter("C:/users/beres/Desktop/test.txt");
        writer.Write(taskSheet.Finish());
        writer.Close();
    }
}