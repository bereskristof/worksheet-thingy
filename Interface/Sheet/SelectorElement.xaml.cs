using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Interface.Sheet;

public partial class SelectorElement : ICanRequestDeletion
{
    public event EventHandler? RequestsDeletion;
    
    public event EventHandler? RequestsTaskSelection;
    
    private readonly Tuple<byte, byte, byte>[] _indentColors =
    [
        new(236, 236, 236),
        new(244, 244, 244),
    ];
    
    private readonly uint _depth;
    
    public SelectorElement() : this(0) { }

    private SelectorElement(uint depth = 0)
    {
        _depth = depth;
        InitializeComponent();
        FormatDesignByIndent(_depth);
    }

    private void OptionListOrdered_OnSelected(object sender, RoutedEventArgs e)
    {
    }

    private void OptionListShuffled_OnSelected(object sender, RoutedEventArgs e)
    {
    }

    private void OptionPickShuffled_OnSelected(object sender, RoutedEventArgs e)
    {
    }

    private void ButtonHide_OnClick(object sender, RoutedEventArgs e) 
        => ToggleHide();

    private void ButtonAddTask_OnClick(object sender, RoutedEventArgs e)
        => AddChildTask();

    private void ButtonAddContainer_OnClick(object sender, RoutedEventArgs e)
        => AddChildSelector();

    private void ButtonDelete_OnClick(object sender, RoutedEventArgs e)
        => Delete(true);
    
    private void FormatDesignByIndent(uint indentLevel)
    {
        Tuple<byte, byte, byte> indCol = _indentColors.ElementAt((int)(indentLevel % 2));
        var color = new SolidColorBrush(Color.FromRgb(indCol.Item1, indCol.Item2, indCol.Item3));
        MainBorder.Background = color;
        if (indentLevel == 0) 
            FormatRoot();
    }

    private void FormatRoot()
    {
        MainBorder.Background = Brushes.Transparent;
        MainBorder.BorderBrush = Brushes.Transparent;
        MainBorder.BorderThickness = new Thickness(0);
        MainBorder.Padding = new Thickness(0);
        MainBorder.Margin = new Thickness(3, 3, 5, 3);
        TitleTextBlock.Text = Interface.Resources.Lang.Sheet_RootTitle;
        DeleteButton.Visibility = Visibility.Collapsed;
        HideButton.Visibility = Visibility.Collapsed;
    }

    private void ToggleHide()
    {
        ContentList.Visibility = ContentList.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        ManageButtons.Visibility = ManageButtons.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        BehaviourBox.IsEnabled = !BehaviourBox.IsEnabled;
        HideButton.Content = ContentList.Visibility == Visibility.Collapsed ? Interface.Resources.Lang.Sheet_Show : Interface.Resources.Lang.Sheet_Hide;
    }
    
    private void AddChildTask()
    {
        var task = new TaskElement(_depth + 1);
        ContentList.Children.Add(task);
        task.RequestsDeletion += DeleteChild;
        task.RequestsTaskSelection += BubbleTaskSelection;
    }

    private void AddChildSelector()
    {
        var selector = new SelectorElement(_depth + 1);
        ContentList.Children.Add(selector);
        selector.RequestsDeletion += DeleteChild;
        selector.RequestsTaskSelection += BubbleTaskSelection;
    }

    private void BubbleTaskSelection(object? sender, EventArgs e) 
        => RequestsTaskSelection?.Invoke(sender, e);

    private void DeleteChild(object? sender, EventArgs e)
    {
        ContentList.Children.Remove((Control)sender!);
    }

    public void Delete(bool root = false)
    {
        foreach (var child in ContentList.Children)
        {
            switch (child)
            {
                case SelectorElement selectorElement:
                    selectorElement.Delete();
                    break;
                case TaskElement taskElement:
                    taskElement.Delete();
                    break;
            }
        }

        if (!root) return;
        RequestsDeletion?.Invoke(this, EventArgs.Empty);
    }
}