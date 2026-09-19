using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neronga.Models;

namespace Neronga.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _lock = new();
    public AppSettings Settings { get; private set; }

    public SettingsStore()
    {
        Settings = LoadOrCreate();
    }

    private static AppSettings LoadOrCreate()
    {
        Directory.CreateDirectory(PathTokenResolver.NerongaDataRoot);
        var path = PathTokenResolver.ConfigFilePath;

        AppSettings settings;
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                // 壊れた設定ファイルは初期化してやり直す（元ファイルは .bak として残す）。
                try { File.Copy(path, path + ".bak", overwrite: true); } catch { /* ignore */ }
                settings = new AppSettings();
            }
        }
        else
        {
            settings = new AppSettings();
        }

        var isFirstRun = !settings.HasRunBefore;
        MergeBuiltInDefaults(settings);

        if (isFirstRun)
        {
            foreach (var candidate in DefaultCatalog.WellKnownWorkspaceRootCandidates())
            {
                try
                {
                    if (Directory.Exists(candidate) &&
                        !settings.WorkspaceRoots.Any(w => string.Equals(w, candidate, StringComparison.OrdinalIgnoreCase)))
                    {
                        settings.WorkspaceRoots.Add(candidate);
                    }
                }
                catch { /* アクセス不可などは無視 */ }
            }
        }

        settings.HasRunBefore = true;
        Save(settings);
        return settings;
    }

    /// <summary>新しい既定カタログを、ユーザー設定へ非破壊的にマージする。</summary>
    private static void MergeBuiltInDefaults(AppSettings settings)
    {
        if (settings.BuiltInCatalogVersion >= DefaultCatalog.CurrentVersion && settings.Entries.Count > 0)
            return;

        var existingIds = settings.Entries.Select(e => e.Id).ToHashSet();
        foreach (var def in DefaultCatalog.BuildDefaults())
        {
            if (existingIds.Contains(def.Id)) continue;
            if (settings.RemovedBuiltInIds.Contains(def.Id)) continue;
            settings.Entries.Add(def);
        }

        settings.BuiltInCatalogVersion = DefaultCatalog.CurrentVersion;
    }

    public void Save() => Save(Settings);

    private static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(PathTokenResolver.NerongaDataRoot);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        var path = PathTokenResolver.ConfigFilePath;
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, path, overwrite: true);
        File.Delete(tmp);
    }

    public void ResetToDefaults()
    {
        Settings.Entries.Clear();
        Settings.RemovedBuiltInIds.Clear();
        Settings.BuiltInCatalogVersion = 0;
        MergeBuiltInDefaults(Settings);
        Save();
    }

    public void RemoveEntry(string id)
    {
        var entry = Settings.Entries.FirstOrDefault(e => e.Id == id);
        if (entry is null) return;
        Settings.Entries.Remove(entry);
        if (entry.IsBuiltIn) Settings.RemovedBuiltInIds.Add(id);
        Save();
    }

    public void AddCustomEntry(PathEntryDefinition entry)
    {
        entry.IsBuiltIn = false;
        if (string.IsNullOrWhiteSpace(entry.Id))
            entry.Id = "custom." + Guid.NewGuid().ToString("N");
        Settings.Entries.Add(entry);
        Save();
    }
}
