using System.Windows;

namespace Interface;

public partial class MainEditor
{
    public MainEditor() => InitializeComponent();

    public void OnExit() => TaskPage.OnExit();

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var list = Bindings.Instance.SheetRoot.GetQuestions();
        foreach (var item in list)
        {
            Console.WriteLine(item.Text);
        }

        Console.WriteLine();
    }
}