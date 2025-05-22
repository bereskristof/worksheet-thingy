using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Storage.Task;

namespace Interface.Task;

public partial class TaskPage
{
    private Question? _currentQuestion;
    
    public TaskPage()
    {
        InitializeComponent();
        TaskList.DataContext = Question.LoadAll();
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
        var answers = question.LoadAllAnswers();
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
        var question = Question.New();
        _currentQuestion = question;
        _currentQuestion?.Store();
        TaskList.DataContext = Question.LoadAll();
        TaskList.ScrollIntoView(TaskList.Items[^1]!);
    }

    private void ButtonAddAnswer_OnClick(object sender, RoutedEventArgs e)
    {
        _currentQuestion?.AddAnswer();
    }
}