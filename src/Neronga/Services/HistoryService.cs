using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LibGit2Sharp;
using Neronga.Models;

namespace Neronga.Services;

public sealed class HistoryCommitInfo
{
    public required string Sha { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset When { get; init; }
}

public sealed class TrackedFileInfo
{
    public required string FileId { get; init; }
    public required string OriginalPath { get; init; }
    public required string Vendor { get; init; }
    public required string Label { get; init; }
    public required EntryKind Kind { get; init; }
    public int CommitCount { get; set; }
    public DateTimeOffset? LastEditedAt { get; set; }
}

/// <summary>
/// 編集対象ファイル（MCP設定 / カスタム指示ファイル）の変更履歴を、
/// アプリ専用の内部Gitリポジトリ (%LocalAppData%\neronga\history) に閉じて記録する。
/// 実ファイルへの書き戻しはこのサービスの責務外（呼び出し元が行う）。
/// </summary>
public sealed class HistoryService
{
    private const string FilesDir = "files";
    private const string ManifestFileName = "manifest.json";
    private const string TombstonesFileName = "tombstones.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    // 署名は必ずコミットごとに生成する。static にすると全コミットが
    // アプリ起動時刻になり、履歴の並び順と「最終編集」表示が壊れる。
    private static Signature CreateSignature() => new("neronga", "neronga@localhost", DateTimeOffset.Now);

    private readonly string _repoPath;

    public HistoryService()
    {
        _repoPath = PathTokenResolver.HistoryRepoRoot;
        Directory.CreateDirectory(_repoPath);
        Directory.CreateDirectory(Path.Combine(_repoPath, FilesDir));
        if (!Repository.IsValid(_repoPath))
        {
            Repository.Init(_repoPath);
        }
    }

    public static string ComputeFileId(string originalPath)
    {
        var normalized = originalPath.Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }

    private string SnapshotRelativePath(string fileId, string originalPath)
    {
        var baseName = SanitizeFileNamePart(Path.GetFileName(originalPath));
        return Path.Combine(FilesDir, $"{fileId}__{baseName}").Replace('\\', '/');
    }

    private static string SanitizeFileNamePart(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    private Dictionary<string, TrackedFileInfo> LoadManifest()
    {
        var path = Path.Combine(_repoPath, ManifestFileName);
        if (!File.Exists(path)) return new Dictionary<string, TrackedFileInfo>();
        try
        {
            var json = File.ReadAllText(path);
            var raw = JsonSerializer.Deserialize<Dictionary<string, ManifestEntry>>(json, JsonOptions) ?? new();
            var result = new Dictionary<string, TrackedFileInfo>();
            foreach (var (id, e) in raw)
            {
                result[id] = new TrackedFileInfo
                {
                    FileId = id,
                    OriginalPath = e.OriginalPath,
                    Vendor = e.Vendor,
                    Label = e.Label,
                    Kind = e.Kind
                };
            }
            return result;
        }
        catch { return new Dictionary<string, TrackedFileInfo>(); }
    }

    private sealed class ManifestEntry
    {
        public string OriginalPath { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public EntryKind Kind { get; set; }
    }

    private void SaveManifestEntry(string fileId, string originalPath, string vendor, string label, EntryKind kind)
    {
        var manifestPath = Path.Combine(_repoPath, ManifestFileName);
        Dictionary<string, ManifestEntry> raw;
        if (File.Exists(manifestPath))
        {
            try { raw = JsonSerializer.Deserialize<Dictionary<string, ManifestEntry>>(File.ReadAllText(manifestPath), JsonOptions) ?? new(); }
            catch { raw = new(); }
        }
        else raw = new();

        raw[fileId] = new ManifestEntry { OriginalPath = originalPath, Vendor = vendor, Label = label, Kind = kind };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(raw, JsonOptions));

        using var repo = new Repository(_repoPath);
        Commands.Stage(repo, ManifestFileName);
        var status = repo.RetrieveStatus();
        if (status.Staged.Any() || status.Added.Any())
        {
            repo.Commit($"register: {label} を追跡対象に追加", CreateSignature(), CreateSignature());
        }
    }

    /// <summary>このファイルを追跡対象として登録する（未登録の場合のみ manifest を更新）。</summary>
    public string RegisterFile(string originalPath, string vendor, string label, EntryKind kind)
    {
        var fileId = ComputeFileId(originalPath);
        var manifest = LoadManifest();
        if (!manifest.ContainsKey(fileId))
        {
            SaveManifestEntry(fileId, originalPath, vendor, label, kind);
        }
        return fileId;
    }

    /// <summary>
    /// ディスク上の現在の内容が、記録済みの最新スナップショットと異なる場合、
    /// 「外部での変更を反映」コミットとして取り込む。
    /// </summary>
    public void EnsureBaseline(string fileId, string originalPath)
    {
        string currentContent;
        try { currentContent = File.ReadAllText(originalPath); }
        catch { return; }

        var latest = GetLatestContent(fileId);
        if (latest is not null && latest == currentContent) return;

        var message = latest is null
            ? "baseline: 取り込み時点の内容を記録"
            : "sync: アプリ外での変更を記録";
        WriteSnapshotAndCommit(fileId, originalPath, currentContent, message);
    }

    public string? GetLatestContent(string fileId)
    {
        var relPath = FindSnapshotRelativePathOnDisk(fileId);
        if (relPath is null) return null;
        var full = Path.Combine(_repoPath, relPath);
        return File.Exists(full) ? File.ReadAllText(full) : null;
    }

    private string? FindSnapshotRelativePathOnDisk(string fileId)
    {
        var dir = Path.Combine(_repoPath, FilesDir);
        if (!Directory.Exists(dir)) return null;
        var match = Directory.EnumerateFiles(dir, $"{fileId}__*").FirstOrDefault();
        if (match is null) return null;
        return Path.GetRelativePath(_repoPath, match).Replace('\\', '/');
    }

    /// <summary>編集内容をコミットする（呼び出し元が実ファイルへの書き戻しも別途行うこと）。</summary>
    public void CommitEdit(string fileId, string originalPath, string newContent, string message)
    {
        WriteSnapshotAndCommit(fileId, originalPath, newContent, message);
    }

    private void WriteSnapshotAndCommit(string fileId, string originalPath, string content, string message)
    {
        var relPath = SnapshotRelativePath(fileId, originalPath);
        var fullPath = Path.Combine(_repoPath, relPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);

        using var repo = new Repository(_repoPath);
        Commands.Stage(repo, relPath);
        var status = repo.RetrieveStatus(relPath);
        if (status == FileStatus.Unaltered) return; // 内容に変化なし
        repo.Commit(message, CreateSignature(), CreateSignature());
    }

    /// <summary>
    /// 指定パスの内容がそのコミットで変化したかどうかを、親コミットとのblob比較で判定する。
    /// LibGit2Sharp の Commits.QueryBy(path) は特定の履歴形状で
    /// KeyNotFoundException を投げることがあるため使用しない。
    /// </summary>
    private static bool TouchesPath(Commit commit, string relPath)
    {
        var currentSha = (commit[relPath]?.Target as Blob)?.Sha;
        var parent = commit.Parents.FirstOrDefault();
        var parentSha = parent is null ? null : (parent[relPath]?.Target as Blob)?.Sha;
        return !string.Equals(currentSha, parentSha, StringComparison.Ordinal);
    }

    /// <summary>
    /// 対象パスを変更したコミットを新しい順に返す。
    /// stopAtSha が指定された場合、そのコミットに到達した時点で打ち切る（そのコミット自身も含めない）。
    /// 履歴削除の境界より前を見せないために使う。
    /// </summary>
    private static List<Commit> CommitsTouching(Repository repo, string relPath, string? stopAtSha)
    {
        // revwalk は新しい順に返す。同一秒のコミットが並んでも順序が崩れないよう、
        // タイムスタンプで並べ直さずウォーク順をそのまま使う。
        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
        };

        var result = new List<Commit>();
        foreach (var commit in repo.Commits.QueryBy(filter))
        {
            if (stopAtSha is not null && string.Equals(commit.Sha, stopAtSha, StringComparison.Ordinal))
                break;
            if (TouchesPath(commit, relPath)) result.Add(commit);
        }
        return result;
    }

    /// <summary>
    /// 履歴を削除した位置（削除コミットのSHA）をファイルIDごとに記録したもの。
    /// Gitのコミット自体は残るため、ここより前を走査対象から外すことで
    /// 「削除した履歴が再編集時に復活する」のを防ぐ。
    /// </summary>
    private Dictionary<string, string> LoadTombstones()
    {
        var path = Path.Combine(_repoPath, TombstonesFileName);
        if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path), JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch { return new Dictionary<string, string>(StringComparer.Ordinal); }
    }

    private string? TombstoneFor(string fileId) =>
        LoadTombstones().TryGetValue(fileId, out var sha) ? sha : null;

    public List<HistoryCommitInfo> GetHistory(string fileId)
    {
        var relPath = FindSnapshotRelativePathOnDisk(fileId);
        if (relPath is null) return new List<HistoryCommitInfo>();

        var stopAt = TombstoneFor(fileId);
        using var repo = new Repository(_repoPath);
        return CommitsTouching(repo, relPath, stopAt)
            .Select(c => new HistoryCommitInfo
            {
                Sha = c.Sha,
                Message = c.MessageShort,
                When = c.Author.When
            })
            .ToList();
    }

    public string GetContentAtCommit(string fileId, string sha)
    {
        var relPath = FindSnapshotRelativePathOnDisk(fileId);
        if (relPath is null) return string.Empty;

        using var repo = new Repository(_repoPath);
        var commit = repo.Lookup<Commit>(sha);
        if (commit is null) return string.Empty;
        var entry = commit[relPath];
        if (entry?.Target is not Blob blob) return string.Empty;
        return blob.GetContentText();
    }

    /// <summary>2つのコミット間の統一diffテキストを返す。</summary>
    public string GetDiffText(string fileId, string shaOld, string shaNew)
    {
        var relPath = FindSnapshotRelativePathOnDisk(fileId);
        if (relPath is null) return string.Empty;

        using var repo = new Repository(_repoPath);
        var commitOld = repo.Lookup<Commit>(shaOld);
        var commitNew = repo.Lookup<Commit>(shaNew);
        if (commitOld is null || commitNew is null) return string.Empty;

        var patch = repo.Diff.Compare<Patch>(commitOld.Tree, commitNew.Tree, new[] { relPath });
        return patch[relPath]?.Patch ?? string.Empty;
    }

    public List<TrackedFileInfo> ListTrackedFiles()
    {
        var manifest = LoadManifest();
        var tombstones = LoadTombstones();
        var result = new List<TrackedFileInfo>();
        using var repo = new Repository(_repoPath);
        foreach (var info in manifest.Values)
        {
            var relPath = FindSnapshotRelativePathOnDisk(info.FileId);
            if (relPath is null) continue;
            tombstones.TryGetValue(info.FileId, out var stopAt);
            var commits = CommitsTouching(repo, relPath, stopAt);
            info.CommitCount = commits.Count;
            // CommitsTouching は新しい順。先頭が最終編集。
            info.LastEditedAt = commits.Count > 0 ? commits[0].Author.When : null;
            if (info.CommitCount > 0) result.Add(info);
        }
        return result.OrderByDescending(i => i.LastEditedAt).ToList();
    }

    /// <summary>
    /// 1ファイル分の編集履歴を削除する（追跡対象からも外す）。
    /// 元の実ファイルには一切触れない。
    /// </summary>
    public void DeleteHistory(string fileId)
    {
        var relPath = FindSnapshotRelativePathOnDisk(fileId);

        // manifest から当該エントリを除去
        var manifestPath = Path.Combine(_repoPath, ManifestFileName);
        Dictionary<string, ManifestEntry> raw;
        if (File.Exists(manifestPath))
        {
            try { raw = JsonSerializer.Deserialize<Dictionary<string, ManifestEntry>>(File.ReadAllText(manifestPath), JsonOptions) ?? new(); }
            catch { raw = new(); }
        }
        else raw = new();

        raw.Remove(fileId);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(raw, JsonOptions));

        // スナップショット本体を削除
        if (relPath is not null)
        {
            var full = Path.Combine(_repoPath, relPath);
            if (File.Exists(full)) File.Delete(full);
        }

        string boundarySha;
        using (var repo = new Repository(_repoPath))
        {
            Commands.Stage(repo, ManifestFileName);
            if (relPath is not null) Commands.Stage(repo, relPath);

            boundarySha = repo.RetrieveStatus().Any()
                ? repo.Commit("delete-history: 編集履歴を削除", CreateSignature(), CreateSignature()).Sha
                : repo.Head.Tip?.Sha ?? string.Empty;
        }

        // 削除位置を記録する。Gitのコミットは残るが、以降はここより前を走査しない。
        if (!string.IsNullOrEmpty(boundarySha))
        {
            var tombstones = LoadTombstones();
            tombstones[fileId] = boundarySha;
            File.WriteAllText(Path.Combine(_repoPath, TombstonesFileName),
                JsonSerializer.Serialize(tombstones, JsonOptions));

            using var repo = new Repository(_repoPath);
            Commands.Stage(repo, TombstonesFileName);
            if (repo.RetrieveStatus().Any())
            {
                repo.Commit("delete-history: 削除位置を記録", CreateSignature(), CreateSignature());
            }
        }

        // 追跡対象が空になったら、リポジトリごと作り直して実体も残さない。
        if (LoadManifest().Count == 0)
        {
            DeleteAllHistory();
        }
    }

    /// <summary>
    /// 履歴リポジトリごと完全に消去して作り直す。過去のGitオブジェクトも残らない。
    /// </summary>
    public void DeleteAllHistory()
    {
        ForceDeleteDirectory(_repoPath);
        Directory.CreateDirectory(_repoPath);
        Directory.CreateDirectory(Path.Combine(_repoPath, FilesDir));
        Repository.Init(_repoPath);
    }

    /// <summary>履歴リポジトリがディスク上で占めている容量。</summary>
    public long GetRepositorySizeBytes()
    {
        try
        {
            if (!Directory.Exists(_repoPath)) return 0;
            return new DirectoryInfo(_repoPath)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f =>
                {
                    try { return f.Length; } catch { return 0L; }
                });
        }
        catch { return 0; }
    }

    /// <summary>
    /// Gitのオブジェクトファイルは読み取り専用属性が付くため、
    /// 属性を解除しながら再帰的に削除する。
    /// </summary>
    private static void ForceDeleteDirectory(string path)
    {
        if (!Directory.Exists(path)) return;

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            try { File.SetAttributes(file, FileAttributes.Normal); } catch { /* ベストエフォート */ }
        }

        try { Directory.Delete(path, recursive: true); }
        catch (IOException)
        {
            // ハンドルの解放待ちで失敗することがあるので一度だけ再試行する。
            Thread.Sleep(120);
            Directory.Delete(path, recursive: true);
        }
    }
}
