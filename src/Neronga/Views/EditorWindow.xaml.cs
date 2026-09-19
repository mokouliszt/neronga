using Neronga.ViewModels;
using Wpf.Ui.Controls;

namespace Neronga.Views;

public partial class EditorWindow : FluentWindow
{
    public EditorWindow(EditorWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
