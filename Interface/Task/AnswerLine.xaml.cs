using System.Windows.Controls;
using Storage.Task;

namespace Interface.Task;

public partial class AnswerLine : UserControl
{
    private readonly Answer _answer;
    
    public AnswerLine(Answer answer)
    {
        InitializeComponent();
        _answer = answer;
    }
}