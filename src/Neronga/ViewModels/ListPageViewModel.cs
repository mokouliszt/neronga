using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Neronga.Localization;
using Neronga.Models;
using Neronga.Services;

namespace Neronga.ViewModels;

public partial class ListPageViewModel : ObservableObject
{
    public ObservableCollection<VendorGroupViewModel> Groups { get; } = new();

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isScanning;
    [ObservableProperty] private string statusText = Loc.S("list.status.idle");
    [ObservableProperty] private bool showWelcomeCard;
    [ObservableProperty] private int totalItemCount;
    [ObservableProperty] private bool hasNoWorkspaceRoots;

    private List<ScannedItem> _lastFullScan = new();

    public event EventHandler<EditTarget>? EditRequested;
    public event EventHandler? RequestOpenSettings;

    /// <summary>項目を透明化したとき。透明化済みページの件数を即時更新するために使う。</summary>
    public event EventHandler? ItemHidden;

    public ListPageViewModel()
    {
        ShowWelcomeCard = !AppServices.Settings.Settings.WelcomeDismissed;
        HasNoWorkspaceRoots = AppServices.Settings.Settings.WorkspaceRoots.Count == 0;
        _ = RescanAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task RescanAsync()
    {
        if (IsScanning) return;
        IsScanning = true;
        StatusText = Loc.S("list.status.scanning");
        try
        {
            var sw = Stopwatch.StartNew();
            var items = await AppServices.Scanner.ScanAsync(AppServices.Settings.Settings);
            sw.Stop();
            _lastFullScan = items;
            ApplyFilter();
            TotalItemCount = items.Count;
            HasNoWorkspaceRoots = AppServices.Settings.Settings.WorkspaceRoots.Count == 0;
            StatusText = Loc.F("list.status.scanned", items.Count, sw.Elapsed.TotalSeconds, DateTime.Now.ToString("HH:mm:ss"));
        }
        catch (Exception ex)
        {
            StatusText = Loc.F("list.status.failed", ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void ApplyFilter()
    {
        Groups.Clear();
        IEnumerable<ScannedItem> filtered = _lastFullScan;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            filtered = _lastFullScan.Where(i =>
                i.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.FullPath.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Vendor.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Label.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var vendorGroup in filtered.GroupBy(i => i.Vendor).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var vgvm = new VendorGroupViewModel(vendorGroup.Key);
            foreach (var item in vendorGroup
                         .OrderBy(i => i.Scope)
                         .ThenBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                var capturedItem = item;
                var ivm = new ScannedItemViewModel(capturedItem);
                ivm.HideRequestedSuccessfully += (s, _) =>
                {
                    if (s is ScannedItemViewModel self)
                    {
                        vgvm.Items.Remove(self);
                        vgvm.NotifyCountChanged();
                    }
                    _lastFullScan.Remove(capturedItem);
                    TotalItemCount = _lastFullScan.Count;
                    ItemHidden?.Invoke(this, EventArgs.Empty);
                };
                ivm.EditRequested += (_, vm) => EditRequested?.Invoke(this, vm);
                vgvm.Items.Add(ivm);
            }
            Groups.Add(vgvm);
        }
    }

    [RelayCommand]
    private void DismissWelcome()
    {
        ShowWelcomeCard = false;
        AppServices.Settings.Settings.WelcomeDismissed = true;
        AppServices.Settings.Save();
    }

    [RelayCommand]
    private void OpenSettings() => RequestOpenSettings?.Invoke(this, EventArgs.Empty);
}
