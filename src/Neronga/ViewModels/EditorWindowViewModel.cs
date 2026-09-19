using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Neronga.Localization;
using Neronga.Services;

namespace Neronga.ViewModels;

public partial class HistoryEntryViewModel : ObservableObject
{
    public HistoryCommitInfo Info { get; }
    public string Sha => Info.Sha;
    public string ShortSha => Info.Sha.Length >= 7 ? Info.Sha[..7] : Info.Sha;
    public string Message => Info.Message;
    public string WhenText => Info.When.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

    public HistoryEntryViewModel(HistoryCommitInfo info) => Info = info;
}

public partial class EditorWindowViewModel : ObservableObject
{
    public EditTarget Target { get; }
    private readonly string _fileId;

    public ObservableCollection<HistoryEntryViewModel> History { get; } = new();

    [ObservableProperty] private string content = string.Empty;
    [ObservableProperty] private string originalContent = string.Empty;
    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private HistoryEntryViewModel? selectedHistoryEntry;
    [ObservableProperty] private string diffText = string.Empty;
    [ObservableProperty] private bool isDiffVisible;

    public string TitleText => $"{Target.Vendor} ・ {Target.Label}";
    public string PathText => Target.FullPath;

    public event EventHandler? RequestClose;

    public EditorWindowViewModel(EditTarget target)
    {
        Target = target;
        _fileId = HistoryService.ComputeFileId(target.FullPath);
        LoadCurrent();

        // 履歴機能に問題があっても、ファイルの閲覧・編集自体は行えるようにする。
        try
        {
            AppServices.History.RegisterFile(target.FullPath, target.Vendor, target.Label, target.Kind);
            AppServices.History.EnsureBaseline(_fileId, target.FullPath);
            ReloadHistory();
        }
        catch (Exception ex)
        {
            StatusText = Loc.F("history.load.failed", ex.Message);
        }
    }

    private bool _suppressDirtyCheck;

    private void LoadCurrent()
    {
        try
        {
            var text = File.ReadAllText(Target.FullPath);
            _suppressDirtyCheck = true;
            Content = text;
            OriginalContent = text;
            _suppressDirtyCheck = false;
            IsDirty = false;
            StatusText = Loc.F("editor.status.loaded", Target.FullPath);
        }
        catch (Exception ex)
        {
            StatusText = Loc.F("editor.status.loadFailed", ex.Message);
        }
    }

    partial void OnContentChanged(string value)
    {
        if (_suppressDirtyCheck) return;
        IsDirty = value != OriginalContent;
    }

    [RelayCommand]
    private void Save()
    {
        // 1) 実ファイルへの書き込み。ここが失敗したときだけ「保存に失敗」とする。
        try
        {
            File.WriteAllText(Target.FullPath, Content);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.F("editor.save.failed", ex.Message), Loc.S("app.name"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        OriginalContent = Content;
        IsDirty = false;
        StatusText = Loc.F("editor.status.saved", DateTime.Now.ToString("HH:mm:ss"));

        // 2) 履歴の記録。失敗してもファイル自体は保存済みなので、保存成功は取り消さない。
        try
        {
            // 履歴を削除したあとに再度編集された場合でも追跡対象へ戻す。
            AppServices.History.RegisterFile(Target.FullPath, Target.Vendor, Target.Label, Target.Kind);
            AppServices.History.CommitEdit(_fileId, Target.FullPath, Content, Loc.F("editor.commit.message", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            ReloadHistory();
        }
        catch (Exception ex)
        {
            StatusText = Loc.F("editor.status.savedNoHistory", ex.Message);
        }
    }

    [RelayCommand]
    private void ReloadHistory()
    {
        History.Clear();
        foreach (var h in AppServices.History.GetHistory(_fileId))
            History.Add(new HistoryEntryViewModel(h));
    }

    partial void OnSelectedHistoryEntryChanged(HistoryEntryViewModel? value)
    {
        if (value is null) { IsDiffVisible = false; return; }
        var idx = History.IndexOf(value);
        if (idx < 0) { IsDiffVisible = false; return; }

        if (idx == History.Count - 1)
        {
            DiffText = Loc.S("editor.diff.first");
        }
        else
        {
            var older = History[idx + 1];
            var diff = AppServices.History.GetDiffText(_fileId, older.Sha, value.Sha);
            DiffText = string.IsNullOrWhiteSpace(diff) ? Loc.S("editor.diff.noChange") : diff;
        }
        IsDiffVisible = true;
    }

    [RelayCommand]
    private void RestoreSelectedVersion()
    {
        if (SelectedHistoryEntry is null) return;
        var restored = AppServices.History.GetContentAtCommit(_fileId, SelectedHistoryEntry.Sha);
        Content = restored;
        StatusText = Loc.F("editor.status.versionLoaded", SelectedHistoryEntry.WhenText);
    }

    [RelayCommand]
    private void Close()
    {
        if (IsDirty)
        {
            var choice = MessageBox.Show(
                Loc.S("editor.unsaved.message"),
                Loc.S("editor.unsaved.title"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (choice != MessageBoxResult.Yes) return;
        }
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
