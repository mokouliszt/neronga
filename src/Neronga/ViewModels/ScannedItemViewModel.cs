using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Neronga.Localization;
using Neronga.Models;
using Neronga.Services;
using Wpf.Ui.Controls;

namespace Neronga.ViewModels;

public partial class ScannedItemViewModel : ObservableObject
{
    public ScannedItem Item { get; }

    public string DisplayName => Item.DisplayName;
    public string FullPath => Item.FullPath;
    public string KindLabel => DisplayHelpers.KindLabel(Item.Kind);
    public string ScopeLabel => Item.WorkspaceRoot is null
        ? DisplayHelpers.ScopeLabel(Item.Scope)
        : $"{DisplayHelpers.ScopeLabel(Item.Scope)} ・ {Path.GetFileName(Item.WorkspaceRoot.TrimEnd('\\', '/'))}";
    public string SizeText => DisplayHelpers.FormatSize(Item.SizeInfo, Item.IsDirectory);
    public string ModifiedText => DisplayHelpers.FormatRelativeOrDate(Item.LastModifiedUtc);
    public bool IsEditable => Item.IsEditable;
    public bool HasNote => !string.IsNullOrWhiteSpace(Item.Note);
    public string? Note => Item.Note;

    public SymbolRegular KindSymbol => Item.IsDirectory
        ? SymbolRegular.Toolbox24
        : Item.Kind == EntryKind.McpConfigFile
            ? SymbolRegular.PlugConnected24
            : SymbolRegular.DocumentText24;

    [ObservableProperty]
    private bool isBusy;

    public event EventHandler? HideRequestedSuccessfully;
    public event EventHandler<EditTarget>? EditRequested;

    public ScannedItemViewModel(ScannedItem item)
    {
        Item = item;
    }

    [RelayCommand]
    private void OpenContainingFolder()
    {
        try
        {
            var folder = Item.IsDirectory ? Item.FullPath : Path.GetDirectoryName(Item.FullPath);
            if (string.IsNullOrEmpty(folder)) return;
            if (Item.IsDirectory)
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{Item.FullPath}\"") { UseShellExecute = true });
        }
        catch { /* エクスプローラーが開けなくても致命的ではない */ }
    }

    [RelayCommand]
    private void CopyPath()
    {
        try { System.Windows.Clipboard.SetText(Item.FullPath); } catch { /* ignore */ }
    }

    [RelayCommand]
    private void Edit()
    {
        if (!IsEditable) return;
        EditRequested?.Invoke(this, new EditTarget(Item.FullPath, Item.Vendor, Item.Label, Item.Kind));
    }

    [RelayCommand]
    private async Task HideAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await Task.Run(() => AppServices.Vault.Hide(Item));
            HideRequestedSuccessfully?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(Loc.F("list.hide.failed", ex.Message), Loc.S("app.name"),
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
