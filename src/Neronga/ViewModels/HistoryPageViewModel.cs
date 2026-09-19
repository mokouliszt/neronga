using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Neronga.Localization;
using Neronga.Services;

namespace Neronga.ViewModels;

public partial class TrackedFileRowViewModel : ObservableObject
{
    public TrackedFileInfo Info { get; }
    public string FileId => Info.FileId;
    public string Vendor => Info.Vendor;
    public string Label => Info.Label;
    public string FullPath => Info.OriginalPath;
    public string KindLabel => DisplayHelpers.KindLabel(Info.Kind);
    public int CommitCount => Info.CommitCount;
    public string EditsText => Loc.F("history.edits", Info.CommitCount);
    public string LastEditedText => Info.LastEditedAt is { } dt
        ? DisplayHelpers.FormatRelativeOrDate(dt.UtcDateTime)
        : "-";

    public event EventHandler<EditTarget>? OpenRequested;
    public event EventHandler<TrackedFileRowViewModel>? DeleteRequested;

    public TrackedFileRowViewModel(TrackedFileInfo info) => Info = info;

    [RelayCommand]
    private void Open() =>
        OpenRequested?.Invoke(this, new EditTarget(Info.OriginalPath, Info.Vendor, Info.Label, Info.Kind));

    [RelayCommand]
    private void Delete() => DeleteRequested?.Invoke(this, this);
}

public partial class HistoryPageViewModel : ObservableObject
{
    public ObservableCollection<TrackedFileRowViewModel> Rows { get; } = new();

    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string sizeText = string.Empty;
    [ObservableProperty] private string noticeText = string.Empty;
    [ObservableProperty] private bool hasNotice;

    public event EventHandler<EditTarget>? EditRequested;

    public HistoryPageViewModel()
    {
        Reload();
    }

    [RelayCommand]
    public void Reload()
    {
        Rows.Clear();

        List<TrackedFileInfo> tracked;
        try
        {
            tracked = AppServices.History.ListTrackedFiles();
        }
        catch (Exception ex)
        {
            IsEmpty = true;
            SizeText = string.Empty;
            ShowNotice(Loc.F("history.load.failed", ex.Message));
            return;
        }

        foreach (var info in tracked)
        {
            var row = new TrackedFileRowViewModel(info);
            row.OpenRequested += (_, target) => EditRequested?.Invoke(this, target);
            row.DeleteRequested += (_, r) => DeleteRow(r);
            Rows.Add(row);
        }
        IsEmpty = Rows.Count == 0;
        UpdateSize();
    }

    private void UpdateSize()
    {
        var bytes = AppServices.History.GetRepositorySizeBytes();
        SizeText = Loc.F("history.size", DisplayHelpers.FormatSize(bytes, isDirectory: false));
    }

    private void DeleteRow(TrackedFileRowViewModel row)
    {
        var choice = MessageBox.Show(
            Loc.F("history.delete.message", row.Label, row.CommitCount, row.FullPath),
            Loc.S("history.delete.title"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (choice != MessageBoxResult.OK) return;

        try
        {
            AppServices.History.DeleteHistory(row.FileId);
            Rows.Remove(row);
            IsEmpty = Rows.Count == 0;
            UpdateSize();
            ShowNotice(Loc.F("history.deleted", row.Label));
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.F("history.delete.failed", ex.Message), Loc.S("app.name"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void DeleteAll()
    {
        if (Rows.Count == 0) return;

        var choice = MessageBox.Show(
            Loc.F("history.deleteAll.message", Rows.Count),
            Loc.S("history.deleteAll.title"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (choice != MessageBoxResult.OK) return;

        try
        {
            AppServices.History.DeleteAllHistory();
            Reload();
            ShowNotice(Loc.S("history.deletedAll"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.F("history.delete.failed", ex.Message), Loc.S("app.name"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowNotice(string message)
    {
        NoticeText = message;
        HasNotice = true;
    }

    [RelayCommand]
    private void DismissNotice() => HasNotice = false;
}
