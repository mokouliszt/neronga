using System.Windows;
using System.Windows.Controls;
using Neronga.ViewModels;

namespace Neronga.Views;

public partial class HistoryPageView : UserControl
{
    public HistoryPageView()
    {
        InitializeComponent();
    }

    private void TrackedFileRow_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 行内のボタン（削除など）を押したときに、行クリック＝エディタを開く動作が
        // 同時に走らないようにする。
        if (IsWithinButton(e.OriginalSource as DependencyObject)) return;

        if (sender is FrameworkElement { DataContext: TrackedFileRowViewModel row } && row.OpenCommand.CanExecute(null))
        {
            row.OpenCommand.Execute(null);
        }
    }

    private static bool IsWithinButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is System.Windows.Controls.Primitives.ButtonBase) return true;
            source = System.Windows.Media.VisualTreeHelper.GetParent(source)
                     ?? (source as FrameworkElement)?.Parent;
        }
        return false;
    }
}
