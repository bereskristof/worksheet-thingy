namespace Interface;

public partial class MainEditor
{
    public MainEditor() => InitializeComponent();

    public void OnExit() => TaskPage.OnExit();
}