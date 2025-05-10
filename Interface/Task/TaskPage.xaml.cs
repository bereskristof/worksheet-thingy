using System.Windows.Controls;

namespace Interface.Task;

public partial class TaskPage
{
    public TaskPage()
    {
        InitializeComponent();
        TaskList.DataContext = new List<Storage.Task.Question>
        {
            Storage.Task.Question.New(),
            Storage.Task.Question.New(),
            Storage.Task.Question.New(),
        };
    }

    private void TaskList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid grid) Console.WriteLine(grid.SelectedIndex);
    }
}