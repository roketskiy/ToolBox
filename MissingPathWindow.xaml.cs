using System.Windows;

namespace ToolBox;

public partial class MissingPathWindow : Window
{
    public MissingPathWindow(string path)
    {
        InitializeComponent();
        PathText.Text = path;
    }

    void Relocate_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
