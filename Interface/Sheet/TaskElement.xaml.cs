using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Storage.Task;

namespace Interface.Sheet;

public partial class TaskElement : ICanRequestDeletion
{
    public event EventHandler? RequestsDeletion;
    
    public event EventHandler? RequestsTaskSelection;
    
    private readonly Tuple<byte, byte, byte>[] _indentColors =
    [
        new(236, 236, 236),
        new(244, 244, 244),
    ];

    public TaskElement() : this(0) { }

    internal TaskElement(uint depth = 0)
    {
        InitializeComponent();
        FormatDesignByIndent(depth);
    }

    private void ButtonSelect_OnClick(object sender, RoutedEventArgs e)
        => SelectQuestion();

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e) 
        => Delete(true);

    private void FormatDesignByIndent(uint indentLevel)
    {
        Tuple<byte, byte, byte> indCol = _indentColors.ElementAt((int)(indentLevel % 2));
        var color = new SolidColorBrush(Color.FromRgb(indCol.Item1, indCol.Item2, indCol.Item3));
        MainBorder.Background = color;
    }
    
    private void SelectQuestion() 
        => RequestsTaskSelection?.Invoke(this, EventArgs.Empty);

    public void Delete(bool root = false)
    {
        if (!root) return;
        RequestsDeletion?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateQuestion(Question question)
    {
        Binding questionBinding = new()
        {
            Source = question,
            Path = new PropertyPath("Text"), 
            UpdateSourceTrigger = UpdateSourceTrigger.Default,
            Mode = BindingMode.OneWay,
        };
        BindingOperations.SetBinding(QuestionPreview, TextBlock.TextProperty, questionBinding);
        // TODO: Update not yet added node
    }
}