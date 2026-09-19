using System.Windows;
using Neronga.Services;
using Neronga.ViewModels;
using Neronga.Views;
using Wpf.Ui.Appearance;

namespace Neronga;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        // RadioButton の IsChecked="True" は InitializeComponent 中に Checked イベントを
        // 同期的に発火させるため、ハンドラが参照する _viewModel を先に用意しておく。
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        InitializeComponent();

        _viewModel.EditRequested += OnEditRequested;

        if (AppServices.Settings.Settings.Theme == "System")
        {
            // updateAccents: false — アクセント色はブランド色で固定し、Windowsの既定色に戻させない。
            SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica, updateAccents: false);
        }
    }

    private void NavList_Checked(object sender, RoutedEventArgs e) => _viewModel.NavigateToCommand.Execute("list");
    private void NavHidden_Checked(object sender, RoutedEventArgs e) => _viewModel.NavigateToCommand.Execute("hidden");
    private void NavHistory_Checked(object sender, RoutedEventArgs e) => _viewModel.NavigateToCommand.Execute("history");
    private void NavSettings_Checked(object sender, RoutedEventArgs e) => _viewModel.NavigateToCommand.Execute("settings");

    private void OnEditRequested(object? sender, EditTarget target)
    {
        var editorVm = new EditorWindowViewModel(target);
        var window = new EditorWindow(editorVm) { Owner = this };
        editorVm.RequestClose += (_, _) => window.Close();
        window.Closed += (_, _) =>
        {
            // 編集履歴ページを開いていれば最新状態に更新する。
            // 履歴の読み取りに失敗してもウィンドウを閉じる操作自体は成功させる。
            try { _viewModel.HistoryPage.Reload(); }
            catch { /* 履歴ページ側で再読み込みできる */ }
        };
        window.Show();
    }
}
