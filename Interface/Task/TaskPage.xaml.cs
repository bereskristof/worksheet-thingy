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

    public void OnExit()
    {
        _currentQuestion?.Store();
    }

    private void TaskList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;
        if (grid.SelectedItem is not Question question)
            return;
        ChangeSelectedTask(question);
    }

    private void ChangeSelectedTask(Question question)
    {
        _currentQuestion?.Store();
        _currentQuestion = question;
        AnswerListPanel.ItemsSource = _currentQuestion.Answers;
        // Update bindings
        Binding questionBinding = new()
        {
            Source = question,
            Path = new PropertyPath("Text"), 
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay,
        };
        BindingOperations.SetBinding(QuestionBox, TextBox.TextProperty, questionBinding);
        Binding scoreBinding = new()
        {
            Source = question,
            Path = new PropertyPath("Points"), 
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay,
        };
        BindingOperations.SetBinding(ScoreBox, TextBox.TextProperty, scoreBinding);
        TaskList.Items.Refresh(); // HACK
        TaskList.SelectedItem = question;
    }

    private void ButtonAdd_OnClick(object sender, RoutedEventArgs e)
    {
        var question = Questions.Add();
        _currentQuestion = question;
        _currentQuestion?.Store();
        ChangeSelectedTask(question);
        TaskList.ScrollIntoView(TaskList.Items[^1]!);
    }

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
    {
        if (_currentQuestion == null) return; // Does nothing if no question is selected
        Questions.Remove(_currentQuestion);
    }

    private void ButtonAddAnswer_OnClick(object sender, RoutedEventArgs e)
    {
        _currentQuestion?.Answers.Add(_currentQuestion);
    }

    private void ButtonDeleteAnswer_OnClick(object sender, RoutedEventArgs e)
    {
        if (AnswerListPanel.SelectedItem is not Answer answer)
            return;
        _currentQuestion?.Answers.Remove(answer);
    }
}