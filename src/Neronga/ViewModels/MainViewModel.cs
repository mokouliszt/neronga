using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Neronga.Localization;

namespace Neronga.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ListPageViewModel ListPage { get; }
    public HiddenPageViewModel HiddenPage { get; }
    public HistoryPageViewModel HistoryPage { get; }
    public SettingsPageViewModel SettingsPage { get; }

    [ObservableProperty] private object? currentPage;
    [ObservableProperty] private string selectedNav = "list";

    public event EventHandler<EditTarget>? EditRequested;

    public MainViewModel()
    {
        ListPage = new ListPageViewModel();
        HiddenPage = new HiddenPageViewModel();
        HistoryPage = new HistoryPageViewModel();
        SettingsPage = new SettingsPageViewModel();

        ListPage.EditRequested += (_, target) => EditRequested?.Invoke(this, target);
        ListPage.RequestOpenSettings += (_, _) => NavigateTo("settings");
        HistoryPage.EditRequested += (_, target) => EditRequested?.Invoke(this, target);
        SettingsPage.SettingsChangedRequiresRescan += async (_, _) => await ListPage.RescanAsync();

        // 透明化・復元の結果を、ページを開いていなくても互いの件数へ即座に反映する。
        ListPage.ItemHidden += (_, _) =>
        {
            try { HiddenPage.Reload(); }
            catch { /* 透明化済みページを開いたときに再読み込みされる */ }
        };
        HiddenPage.ItemRestored += async (_, _) => await ListPage.RescanAsync();

        // 言語を切り替えたとき、VM側で組み立て済みの文言（件数表示や相対時刻など）も作り直す。
        Loc.LanguageChanged += OnLanguageChanged;

        currentPage = ListPage;
    }

    private async void OnLanguageChanged(object? sender, EventArgs e)
    {
        try
        {
            HiddenPage.Reload();
            HistoryPage.Reload();
            await ListPage.RescanAsync();
        }
        catch { /* 各ページを開いた時点で再読み込みされる */ }
    }

    [RelayCommand]
    private void NavigateTo(string tag)
    {
        SelectedNav = tag;
        CurrentPage = tag switch
        {
            "list" => ListPage,
            "hidden" => HiddenPage,
            "history" => HistoryPage,
            "settings" => SettingsPage,
            _ => ListPage
        };

        switch (tag)
        {
            case "hidden": HiddenPage.Reload(); break;
            case "history": HistoryPage.Reload(); break;
        }
    }
}
