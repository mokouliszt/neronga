namespace Neronga.Models;

/// <summary>
/// %LocalAppData%\neronga\config.json に保存されるアプリ全体の設定。
/// </summary>
public sealed class AppSettings
{
    /// <summary>設定ファイルのスキーマバージョン。将来のマイグレーション用。</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>組み込みカタログのバージョン。これが古い場合、新しい既定値をマージする。</summary>
    public int BuiltInCatalogVersion { get; set; } = 0;

    public List<PathEntryDefinition> Entries { get; set; } = new();

    /// <summary>再帰スキャン対象のプロジェクトルートフォルダ一覧（絶対パス）。</summary>
    public List<string> WorkspaceRoots { get; set; } = new();

    /// <summary>ユーザーが明示的に削除した組み込み項目のID。再起動時に復活させないために記録する。</summary>
    public HashSet<string> RemovedBuiltInIds { get; set; } = new();

    /// <summary>ワークスペース再帰スキャンの最大階層数。</summary>
    public int WorkspaceScanDepth { get; set; } = 6;

    /// <summary>再帰スキャン時に除外するフォルダ名（大文字小文字区別なし）。</summary>
    public List<string> ExcludedFolderNames { get; set; } = new()
    {
        "node_modules", ".git", "bin", "obj", "dist", "build", "out",
        ".venv", "venv", "__pycache__", "target", ".next", ".nuxt",
        "vendor", ".idea", ".vs", "packages", ".cache"
    };

    /// <summary>Light / Dark / System。</summary>
    public string Theme { get; set; } = "System";

    /// <summary>UIの表示言語。"ja" または "en"。</summary>
    public string Language { get; set; } = "ja";

    /// <summary>初回起動時のウェルカムカードを閉じたかどうか。</summary>
    public bool WelcomeDismissed { get; set; }

    /// <summary>これまでに一度でも起動したことがあるか（初回セットアップ判定用）。</summary>
    public bool HasRunBefore { get; set; }
}
