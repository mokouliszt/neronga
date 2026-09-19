using System.IO;
using System.Text.Json;
using Neronga.Models;

namespace Neronga.Services;

public enum RestoreConflictAction
{
    Overwrite,
    RenameNew,
    Cancel
}

public sealed class VaultEntry
{
    public required string VaultId { get; init; }
    public required HiddenEntryMeta Meta { get; init; }
    public required string StoredFullPath { get; init; }
}

/// <summary>
/// 「透明化」（一時退避）機能。%LocalAppData%\neronga\vault\&lt;uuid&gt;\ 配下へ
/// ファイル/フォルダを移動し、元の場所への復元を可能にする。
/// </summary>
public sealed class VaultService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    private const string MetaFileName = "__neronga_meta.json";

    public VaultService()
    {
        Directory.CreateDirectory(PathTokenResolver.VaultRoot);
    }

    public HiddenEntryMeta Hide(ScannedItem item)
    {
        var vaultId = Guid.NewGuid().ToString("N");
        var vaultDir = Path.Combine(PathTokenResolver.VaultRoot, vaultId);
        Directory.CreateDirectory(vaultDir);

        var name = Path.GetFileName(item.FullPath.TrimEnd(Path.DirectorySeparatorChar));
        var destination = Path.Combine(vaultDir, name);

        MoveSafely(item.FullPath, destination, item.IsDirectory);

        var meta = new HiddenEntryMeta
        {
            VaultId = vaultId,
            OriginalFullPath = item.FullPath,
            Vendor = item.Vendor,
            Label = item.Label,
            Kind = item.Kind,
            WasDirectory = item.IsDirectory,
            HiddenAtUtc = DateTime.UtcNow,
            StoredName = name
        };

        File.WriteAllText(Path.Combine(vaultDir, MetaFileName), JsonSerializer.Serialize(meta, JsonOptions));
        return meta;
    }

    public List<VaultEntry> ListHidden()
    {
        var result = new List<VaultEntry>();
        if (!Directory.Exists(PathTokenResolver.VaultRoot)) return result;

        foreach (var dir in Directory.EnumerateDirectories(PathTokenResolver.VaultRoot))
        {
            var metaPath = Path.Combine(dir, MetaFileName);
            if (!File.Exists(metaPath)) continue;
            try
            {
                var meta = JsonSerializer.Deserialize<HiddenEntryMeta>(File.ReadAllText(metaPath), JsonOptions);
                if (meta is null) continue;
                var storedFullPath = Path.Combine(dir, meta.StoredName);
                result.Add(new VaultEntry { VaultId = meta.VaultId, Meta = meta, StoredFullPath = storedFullPath });
            }
            catch { /* 壊れたエントリはスキップ */ }
        }

        return result.OrderByDescending(e => e.Meta.HiddenAtUtc).ToList();
    }

    /// <summary>復元先に既に何かが存在するか確認する。</summary>
    public bool HasConflict(VaultEntry entry) =>
        File.Exists(entry.Meta.OriginalFullPath) || Directory.Exists(entry.Meta.OriginalFullPath);

    public void Restore(VaultEntry entry, RestoreConflictAction conflictAction = RestoreConflictAction.Cancel)
    {
        var originalPath = entry.Meta.OriginalFullPath;
        var parent = Path.GetDirectoryName(originalPath);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);

        if (HasConflict(entry))
        {
            switch (conflictAction)
            {
                case RestoreConflictAction.Overwrite:
                    DeleteExisting(originalPath, entry.Meta.WasDirectory);
                    break;
                case RestoreConflictAction.RenameNew:
                    originalPath = GenerateNonConflictingName(originalPath);
                    break;
                default:
                    throw new InvalidOperationException("復元先に既存の項目があります。");
            }
        }

        MoveSafely(entry.StoredFullPath, originalPath, entry.Meta.WasDirectory);

        var vaultDir = Path.GetDirectoryName(entry.StoredFullPath)!;
        try { Directory.Delete(vaultDir, recursive: true); } catch { /* ベストエフォート */ }
    }

    public void DeleteForever(VaultEntry entry)
    {
        var vaultDir = Path.GetDirectoryName(entry.StoredFullPath)!;
        DeleteExisting(entry.StoredFullPath, entry.Meta.WasDirectory);
        try { Directory.Delete(vaultDir, recursive: true); } catch { /* ignore */ }
    }

    private static string GenerateNonConflictingName(string originalPath)
    {
        var dir = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var nameNoExt = Path.GetFileNameWithoutExtension(originalPath);
        var ext = Path.GetExtension(originalPath);
        var isDirLike = string.IsNullOrEmpty(ext);
        var i = 1;
        string candidate;
        do
        {
            candidate = isDirLike
                ? Path.Combine(dir, $"{nameNoExt} (restored {i})")
                : Path.Combine(dir, $"{nameNoExt} (restored {i}){ext}");
            i++;
        } while (File.Exists(candidate) || Directory.Exists(candidate));
        return candidate;
    }

    private static void DeleteExisting(string path, bool isDirectory)
    {
        if (isDirectory && Directory.Exists(path)) Directory.Delete(path, recursive: true);
        else if (File.Exists(path)) File.Delete(path);
    }

    private static void MoveSafely(string source, string destination, bool isDirectory)
    {
        try
        {
            if (isDirectory) Directory.Move(source, destination);
            else File.Move(source, destination, overwrite: false);
        }
        catch (IOException)
        {
            // 別ドライブ間などで Move が使えない場合はコピー＋削除にフォールバックする。
            if (isDirectory)
            {
                CopyDirectoryRecursive(source, destination);
                Directory.Delete(source, recursive: true);
            }
            else
            {
                File.Copy(source, destination, overwrite: false);
                File.Delete(source);
            }
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.EnumerateFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: false);
        foreach (var dir in Directory.EnumerateDirectories(sourceDir))
            CopyDirectoryRecursive(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }
}
