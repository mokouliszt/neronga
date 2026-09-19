using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Neronga.Localization;
using Neronga.Services;

namespace Neronga.ViewModels;

public partial class HiddenItemViewModel : ObservableObject
{
    public VaultEntry Entry { get; }
    public string OriginalPath => Entry.Meta.OriginalFullPath;
    public string Vendor => Entry.Meta.Vendor;
    public string Label => Entry.Meta.Label;
    public string KindLabel => DisplayHelpers.KindLabel(Entry.Meta.Kind);
    public string HiddenAtText => DisplayHelpers.FormatRelativeOrDate(Entry.Meta.HiddenAtUtc);

    [ObservableProperty] private bool isBusy;

    public event EventHandler? Restored;
    public event EventHandler? DeletedForever;

    public HiddenItemViewModel(VaultEntry entry) => Entry = entry;

    [RelayCommand]
    private void Restore()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var conflictAction = RestoreConflictAction.Cancel;
            if (AppServices.Vault.HasConflict(Entry))
            {
                var choice = MessageBox.Show(
                    Loc.S("hidden.conflict.message"),
                    Loc.S("hidden.conflict.title"),
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                conflictAction = choice switch
                {
                    MessageBoxResult.Yes => RestoreConflictAction.Overwrite,
                    MessageBoxResult.No => RestoreConflictAction.RenameNew,
                    _ => RestoreConflictAction.Cancel
                };
                if (conflictAction == RestoreConflictAction.Cancel) return;
            }

            AppServices.Vault.Restore(Entry, conflictAction);
            Restored?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.F("hidden.restore.failed", ex.Message), Loc.S("app.name"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void DeleteForever()
    {
        var choice = MessageBox.Show(
            Loc.F("hidden.deleteForever.message", Label, OriginalPath),
            Loc.S("hidden.deleteForever.title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (choice != MessageBoxResult.Yes) return;

        try
        {
            AppServices.Vault.DeleteForever(Entry);
            DeletedForever?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.F("hidden.delete.failed", ex.Message), Loc.S("app.name"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

public partial class HiddenPageViewModel : ObservableObject
{
    public ObservableCollection<HiddenItemViewModel> Items { get; } = new();

    [ObservableProperty] private bool isEmpty;

    /// <summary>透明化を解除したとき。アイテム一覧を再スキャンして件数を即時更新するために使う。</summary>
    public event EventHandler? ItemRestored;

    public HiddenPageViewModel()
    {
        Items.CollectionChanged += (_, _) => IsEmpty = Items.Count == 0;
        Reload();
    }

    [RelayCommand]
    public void Reload()
    {
        Items.Clear();
        foreach (var e in AppServices.Vault.ListHidden())
        {
            var vm = new HiddenItemViewModel(e);
            vm.Restored += (_, _) =>
            {
                Items.Remove(vm);
                ItemRestored?.Invoke(this, EventArgs.Empty);
            };
            vm.DeletedForever += (_, _) => Items.Remove(vm);
            Items.Add(vm);
        }
        IsEmpty = Items.Count == 0;
    }
}
