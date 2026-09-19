using System.IO;
using Neronga.Models;

namespace Neronga.Services;

public sealed class ScannerService
{
    public Task<List<ScannedItem>> ScanAsync(AppSettings settings, CancellationToken ct = default)
        => Task.Run(() => Scan(settings, ct), ct);

    private static List<ScannedItem> Scan(AppSettings settings, CancellationToken ct)
    {
        var results = new List<ScannedItem>();
        var enabled = settings.Entries.Where(e => e.Enabled).ToList();

        // ---- Global entries ----
        foreach (var entry in enabled.Where(e => e.Scope == ScopeType.Global))
        {
            ct.ThrowIfCancellationRequested();
            string resolved;
            try { resolved = PathTokenResolver.Resolve(entry.PathTemplate); }
            catch { continue; }
            results.AddRange(ExpandPath(entry, resolved, workspaceRoot: null));
        }

        // ---- Project entries (checked at every directory under each workspace root) ----
        var projectEntries = enabled.Where(e => e.Scope == ScopeType.Project).ToList();
        if (projectEntries.Count > 0)
        {
            var excluded = new HashSet<string>(settings.ExcludedFolderNames, StringComparer.OrdinalIgnoreCase);
            foreach (var root in settings.WorkspaceRoots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ct.ThrowIfCancellationRequested();
                if (!Directory.Exists(root)) continue;
                WalkWorkspace(root, settings.WorkspaceScanDepth, excluded, projectEntries, results, ct);
            }
        }

        return results;
    }

    private static void WalkWorkspace(
        string root, int maxDepth, HashSet<string> excluded,
        List<PathEntryDefinition> projectEntries, List<ScannedItem> results, CancellationToken ct)
    {
        var stack = new Stack<(string Dir, int Depth)>();
        stack.Push((root, 0));

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var (dir, depth) = stack.Pop();

            foreach (var entry in projectEntries)
            {
                var relative = entry.PathTemplate.Replace('/', Path.DirectorySeparatorChar);
                string candidate;
                try { candidate = Path.Combine(dir, relative); }
                catch { continue; }
                results.AddRange(ExpandPath(entry, candidate, root));
            }

            if (depth >= maxDepth) continue;

            IEnumerable<string> subdirs;
            try { subdirs = Directory.EnumerateDirectories(dir); }
            catch { continue; }

            foreach (var sub in subdirs)
            {
                string name;
                try { name = Path.GetFileName(sub); }
                catch { continue; }
                if (excluded.Contains(name)) continue;
                stack.Push((sub, depth + 1));
            }
        }
    }

    private static IEnumerable<ScannedItem> ExpandPath(PathEntryDefinition entry, string resolvedPath, string? workspaceRoot)
    {
        switch (entry.Kind)
        {
            case EntryKind.InstructionFile:
            case EntryKind.McpConfigFile:
                {
                    if (SafeFileExists(resolvedPath))
                    {
                        var item = MakeFileItem(entry, resolvedPath, workspaceRoot);
                        if (item is not null) yield return item;
                    }
                    break;
                }

            case EntryKind.SkillsFolder:
                {
                    if (!SafeDirExists(resolvedPath)) yield break;
                    foreach (var dir in SafeEnumerateDirectories(resolvedPath))
                    {
                        // ドットで始まるフォルダ（.system 等）はエージェント内部/システム用のため対象外。
                        if (IsDotPrefixed(dir)) continue;
                        var item = MakeSkillDirItem(entry, dir, workspaceRoot);
                        if (item is not null) yield return item;
                    }
                    break;
                }

            case EntryKind.InstructionFolder:
                {
                    // .clinerules 等、旧形式では単一ファイルの場合がある
                    if (SafeFileExists(resolvedPath))
                    {
                        var item = MakeFileItem(entry, resolvedPath, workspaceRoot);
                        if (item is not null) yield return item;
                        yield break;
                    }
                    if (!SafeDirExists(resolvedPath)) yield break;
                    foreach (var file in SafeEnumerateFilesRecursive(resolvedPath, maxDepth: 4))
                    {
                        var item = MakeFileItem(entry, file, workspaceRoot);
                        if (item is not null) yield return item;
                    }
                    break;
                }
        }
    }

    private static ScannedItem? MakeFileItem(PathEntryDefinition entry, string fullPath, string? workspaceRoot)
    {
        try
        {
            var info = new FileInfo(fullPath);
            return new ScannedItem
            {
                Vendor = entry.DisplayVendor,
                Label = entry.DisplayLabel,
                Kind = entry.Kind,
                Scope = entry.Scope,
                FullPath = info.FullName,
                IsDirectory = false,
                SizeInfo = info.Length,
                LastModifiedUtc = info.LastWriteTimeUtc,
                SourceEntryId = entry.Id,
                WorkspaceRoot = workspaceRoot,
                Note = entry.DisplayNote
            };
        }
        catch { return null; }
    }

    private static ScannedItem? MakeSkillDirItem(PathEntryDefinition entry, string fullPath, string? workspaceRoot)
    {
        try
        {
            var info = new DirectoryInfo(fullPath);
            long fileCount = 0;
            try { fileCount = info.EnumerateFiles("*", SearchOption.AllDirectories).LongCount(); } catch { /* ignore */ }
            return new ScannedItem
            {
                Vendor = entry.DisplayVendor,
                Label = entry.DisplayLabel,
                Kind = entry.Kind,
                Scope = entry.Scope,
                FullPath = info.FullName,
                IsDirectory = true,
                SizeInfo = fileCount,
                LastModifiedUtc = info.LastWriteTimeUtc,
                SourceEntryId = entry.Id,
                WorkspaceRoot = workspaceRoot,
                Note = entry.DisplayNote
            };
        }
        catch { return null; }
    }

    private static bool IsDotPrefixed(string path)
    {
        try
        {
            var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return name.StartsWith('.');
        }
        catch { return false; }
    }

    private static bool SafeFileExists(string path)
    {
        try { return File.Exists(path); } catch { return false; }
    }

    private static bool SafeDirExists(string path)
    {
        try { return Directory.Exists(path); } catch { return false; }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        IEnumerable<string> dirs;
        try { dirs = Directory.EnumerateDirectories(path).ToList(); }
        catch { yield break; }
        foreach (var d in dirs) yield return d;
    }

    private static IEnumerable<string> SafeEnumerateFilesRecursive(string path, int maxDepth)
    {
        var stack = new Stack<(string Dir, int Depth)>();
        stack.Push((path, 0));
        while (stack.Count > 0)
        {
            var (dir, depth) = stack.Pop();
            List<string> files;
            try { files = Directory.EnumerateFiles(dir).ToList(); }
            catch { continue; }
            foreach (var f in files) yield return f;

            if (depth >= maxDepth) continue;
            List<string> subdirs;
            try { subdirs = Directory.EnumerateDirectories(dir).ToList(); }
            catch { continue; }
            foreach (var sd in subdirs)
            {
                if (IsDotPrefixed(sd)) continue;
                stack.Push((sd, depth + 1));
            }
        }
    }
}
