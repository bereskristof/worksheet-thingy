using System.Diagnostics;
using System.Windows.Navigation;

namespace Interface.About;

public partial class About
{
    public About()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start("explorer", e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}