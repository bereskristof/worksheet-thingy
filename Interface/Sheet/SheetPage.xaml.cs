using System.Windows.Controls;
using Storage.Task;

namespace Interface.Sheet;

public partial class SheetPage
{
    TaskElement? _questionSelectionTarget;
    
    private static QuestionList Questions => Bindings.Instance.Questions;
    
    public SheetPage()
    {
        InitializeComponent();
        TaskList.DataContext = Questions;
    }

    private void TaskList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;
        if (grid.SelectedItem is not Question question)
            return;
        _questionSelectionTarget?.UpdateQuestion(question);
        SwapToSheetEditor();
    }

    private void SheetEditor_OnRequestsTaskSelection(object? sender, EventArgs e)
    {
        if (sender is not TaskElement taskElement)
            return;
        SwapToQuestionSelection(taskElement);
    }

    private void SwapToQuestionSelection(TaskElement target)
    {
        TaskList.IsEnabled = true;
        SheetTreeView.IsEnabled = false;
        _questionSelectionTarget = target;
    }

    private void SwapToSheetEditor()
    {
        TaskList.IsEnabled = false;
        SheetTreeView.IsEnabled = true;
        _questionSelectionTarget = null;
    }
}