using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Neronga.Localization;
using Neronga.Models;
using Neronga.Services;

namespace Neronga.ViewModels;

public partial class PathEntryRowViewModel : ObservableObject
{
    public PathEntryDefinition Definition { get; }
    public string Vendor => Definition.DisplayVendor;
    public string Label => Definition.DisplayLabel;
    public string KindLabel => DisplayHelpers.KindLabel(Definition.Kind);
    public string ScopeLabel => DisplayHelpers.ScopeLabel(Definition.Scope);
    public string PathTemplate => Definition.PathTemplate;
    public bool IsBuiltIn => Definition.IsBuiltIn;
    public string? Note => Definition.DisplayNote;
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    [ObservableProperty] private bool enabled;

    public event EventHandler? Removed;
    public event Action<PathEntryRowViewModel>? EnabledToggled;

    public PathEntryRowViewModel(PathEntryDefinition def)
    {
        Definition = def;
        enabled = def.Enabled;
    }

    partial void OnEnabledChanged(bool value)
    {
        Definition.Enabled = value;
        EnabledToggled?.Invoke(this);
    }

    [RelayCommand]
    private void Remove() => Removed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenInExplorer()
    {
        try
        {
            if (Definition.Scope != ScopeType.Global) return;
            var resolved = PathTokenResolver.Resolve(Definition.PathTemplate);
            var target = File.Exists(resolved) ? Path.GetDirectoryName(resolved) : resolved;
            if (string.IsNullOrEmpty(target) || !Directory.Exists(target)) return;
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{target}\"") { UseShellExecute = true });
        }
        catch { /* ignore */ }
    }
}

public partial class SettingsPageViewModel : ObservableObject
{
    public ObservableCollection<PathEntryRowViewModel> Entries { get; } = new();
    public ObservableCollection<string> WorkspaceRoots { get; } = new();

    [ObservableProperty] private double workspaceScanDepth;
    [ObservableProperty] private string excludedFolderNamesText = string.Empty;
    [ObservableProperty] private string themeSelection = "System";
    [ObservableProperty] private string languageSelection = "日本語";

    [ObservableProperty] private string newVendor = string.Empty;
    [ObservableProperty] private string newLabel = string.Empty;
    [ObservableProperty] private string newPath = string.Empty;
    [ObservableProperty] private EntryKind newKind = EntryKind.InstructionFile;
    [ObservableProperty] private ScopeType newScope = ScopeType.Global;

    public EntryKind[] KindOptions { get; } = Enum.GetValues<EntryKind>();
    public ScopeType[] ScopeOptions { get; } = Enum.GetValues<ScopeType>();
    public string[] ThemeOptions { get; } = { "System", "Light", "Dark" };
    public string[] LanguageOptions { get; } = { "日本語", "English" };

    public event EventHandler? SettingsChangedRequiresRescan;

    private bool _loading;

    public SettingsPageViewModel()
    {
        LoadFromStore();
        // 言語を切り替えると組み込みエントリのラベル表記が変わるため、一覧を作り直す。
        Loc.LanguageChanged += (_, _) => LoadFromStore();
    }

    private void LoadFromStore()
    {
        _loading = true;
        var s = AppServices.Settings.Settings;
        Entries.Clear();
        foreach (var e in s.Entries.OrderBy(x => x.Vendor, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.Scope)
                     .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase))
        {
            Entries.Add(WrapEntry(e));
        }

        WorkspaceRoots.Clear();
        foreach (var w in s.WorkspaceRoots) WorkspaceRoots.Add(w);

        WorkspaceScanDepth = s.WorkspaceScanDepth;
        ExcludedFolderNamesText = string.Join(", ", s.ExcludedFolderNames);
        ThemeSelection = s.Theme;
        LanguageSelection = s.Language == Loc.English ? "English" : "日本語";
        _loading = false;
    }

    private PathEntryRowViewModel WrapEntry(PathEntryDefinition def)
    {
        var row = new PathEntryRowViewModel(def);
        row.Removed += (_, _) => RemoveEntry(row);
        row.EnabledToggled += _ => { AppServices.Settings.Save(); SettingsChangedRequiresRescan?.Invoke(this, EventArgs.Empty); };
        return row;
    }

    private void RemoveEntry(PathEntryRowViewModel row)
    {
        AppServices.Settings.RemoveEntry(row.Definition.Id);
        Entries.Remove(row);
        SettingsChangedRequiresRescan?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void AddEntry()
    {
        if (string.IsNullOrWhiteSpace(NewVendor) || string.IsNullOrWhiteSpace(NewLabel) || string.IsNullOrWhiteSpace(NewPath))
        {
            MessageBox.Show(Loc.S("settings.add.incomplete"), Loc.S("app.name"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var def = new PathEntryDefinition
        {
            Vendor = NewVendor.Trim(),
            Label = NewLabel.Trim(),
            Kind = NewKind,
            Scope = NewScope,
            PathTemplate = NewPath.Trim(),
            Enabled = true
        };
        AppServices.Settings.AddCustomEntry(def);
        Entries.Add(WrapEntry(def));

        NewVendor = string.Empty;
        NewLabel = string.Empty;
        NewPath = string.Empty;
        SettingsChangedRequiresRescan?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void BrowseNewPath()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = Loc.S("settings.add.browse.dialog") };
        if (dlg.ShowDialog() == true)
        {
            NewPath = dlg.FolderName;
        }
    }

    [RelayCommand]
    private void AddWorkspaceRoot()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = Loc.S("settings.roots.dialog") };
        if (dlg.ShowDialog() != true) return;

        var path = dlg.FolderName;
        if (WorkspaceRoots.Any(w => string.Equals(w, path, StringComparison.OrdinalIgnoreCase))) return;

        WorkspaceRoots.Add(path);
        AppServices.Settings.Settings.WorkspaceRoots.Add(path);
        AppServices.Settings.Save();
        SettingsChangedRequiresRescan?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void RemoveWorkspaceRoot(string path)
    {
        WorkspaceRoots.Remove(path);
        AppServices.Settings.Settings.WorkspaceRoots.RemoveAll(w => string.Equals(w, path, StringComparison.OrdinalIgnoreCase));
        AppServices.Settings.Save();
        SettingsChangedRequiresRescan?.Invoke(this, EventArgs.Empty);
    }

    partial void OnWorkspaceScanDepthChanged(double value)
    {
        if (_loading) return;
        AppServices.Settings.Settings.WorkspaceScanDepth = Math.Max(1, (int)value);
        AppServices.Settings.Save();
    }

    partial void OnExcludedFolderNamesTextChanged(string value)
    {
        if (_loading) return;
        var list = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        AppServices.Settings.Settings.ExcludedFolderNames = list;
        AppServices.Settings.Save();
    }

    partial void OnThemeSelectionChanged(string value)
    {
        if (_loading) return;
        AppServices.Settings.Settings.Theme = value;
        AppServices.Settings.Save();
        App.ApplyTheme(value);
    }

    partial void OnLanguageSelectionChanged(string value)
    {
        if (_loading) return;
        var code = value == "English" ? Loc.English : Loc.Japanese;
        AppServices.Settings.Settings.Language = code;
        AppServices.Settings.Save();
        Loc.Instance.SetLanguage(code);
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        var choice = MessageBox.Show(
            Loc.S("settings.entries.reset.confirm"),
            Loc.S("app.name"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (choice != MessageBoxResult.Yes) return;

        AppServices.Settings.ResetToDefaults();
        LoadFromStore();
        SettingsChangedRequiresRescan?.Invoke(this, EventArgs.Empty);
    }
}
