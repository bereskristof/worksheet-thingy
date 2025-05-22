using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Storage.Task;

namespace Interface.Task;

public partial class TaskPage
{
    private Question? _currentQuestion;

    public QuestionList Questions { get; set; } = [];
    
    public TaskPage()
    {
        InitializeComponent();
        Questions.LoadAll();
        TaskList.DataContext = Questions;
    }

    private void TaskList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;
        if (grid.SelectedItem is not Question question)
            return;
        _currentQuestion?.Store();
        _currentQuestion = question;
        AnswerListPanel.Children.Clear();
        var answers = question.GetAnswers();
        foreach (var answerItem in answers.Select(answer => new AnswerLine(answer)))
        {
            AnswerListPanel.Children.Add(answerItem);
        }
        // Update bindings
        Binding freshAfBinding = new()
        {
            Source = question,
            Path = new PropertyPath("Text"), 
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay,
        };
        BindingOperations.SetBinding(QuestionBox, TextBox.TextProperty, freshAfBinding);
        grid.Items.Refresh(); // HACK, If you fix this, change the selection color too!
    }

    public void OnExit()
    {
        _currentQuestion?.Store();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var question = Questions.Add();
        _currentQuestion = question;
        _currentQuestion?.Store();
        TaskList.ScrollIntoView(TaskList.Items[^1]!);
    }

    private void ButtonAddAnswer_OnClick(object sender, RoutedEventArgs e)
    {
        _currentQuestion?.AddAnswer();
    }
}