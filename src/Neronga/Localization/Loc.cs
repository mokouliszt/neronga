using System.ComponentModel;

namespace Neronga.Localization;

/// <summary>
/// UI文言の一元管理と実行時の言語切り替え。
/// XAML からは {loc:T Key} 経由でインデクサにバインドしているため、
/// 言語を変えると PropertyChanged("Item[]") で全バインディングが更新される。
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    public static Loc Instance { get; } = new();

    public const string Japanese = "ja";
    public const string English = "en";

    private string _language = Japanese;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>言語が切り替わったとき。動的に組み立てた文言を再生成するために使う。</summary>
    public static event EventHandler? LanguageChanged;

    public string Language => _language;
    public bool IsEnglish => _language == English;

    private Loc() { }

    public void SetLanguage(string language)
    {
        var normalized = language == English ? English : Japanese;
        if (_language == normalized) return;
        _language = normalized;

        // インデクサ全体の再評価を促す
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnglish)));
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public string this[string key] => Get(key);

    /// <summary>文言を取得する。未定義のキーはキー名をそのまま返す（欠落に気づけるように）。</summary>
    public static string S(string key) => Instance.Get(key);

    /// <summary>書式付きの文言を取得する。</summary>
    public static string F(string key, params object?[] args)
    {
        var format = Instance.Get(key);
        try { return string.Format(format, args); }
        catch (FormatException) { return format; }
    }

    private string Get(string key)
    {
        var table = _language == English ? En : Ja;
        if (table.TryGetValue(key, out var value)) return value;
        if (Ja.TryGetValue(key, out var fallback)) return fallback;
        return key;
    }

    private static readonly Dictionary<string, string> Ja = new(StringComparer.Ordinal)
    {
        // 共通
        ["app.name"] = "neronga",
        ["common.separator"] = " ・ ",
        ["common.refresh"] = "更新",
        ["common.delete"] = "削除",
        ["common.open"] = "開く",
        ["common.ok"] = "OK",
        ["common.cancel"] = "キャンセル",

        // ナビゲーション
        ["nav.items"] = "アイテム一覧",
        ["nav.hidden"] = "透明化済み",
        ["nav.history"] = "編集履歴",
        ["nav.settings"] = "設定",
        ["app.subtitle"] = "NERONGA",

        // アイテム一覧
        ["list.search.placeholder"] = "ベンダー名・ファイル名・パスで検索",
        ["list.rescan"] = "再スキャン",
        ["list.hide"] = "透明化",
        ["list.tooltip.openFolder"] = "フォルダを開く",
        ["list.tooltip.copyPath"] = "パスをコピー",
        ["list.tooltip.edit"] = "編集する（差分履歴つき）",
        ["list.empty"] = "該当する項目が見つかりませんでした。",
        ["list.welcome.title"] = "neronga へようこそ",
        ["list.welcome.message"] = "各社エージェントのグローバル設定は既に自動検出されています。プロジェクトごとの CLAUDE.md や AGENTS.md も見つけたい場合は、設定画面から開発フォルダを追加してください。",
        ["list.noWorkspace.title"] = "プロジェクトフォルダ未設定",
        ["list.noWorkspace.message"] = "現在はグローバル設定のみをスキャンしています。設定画面で開発フォルダを追加すると、プロジェクトごとの設定も検出できます。",
        ["list.status.idle"] = "スキャン待機中",
        ["list.status.scanning"] = "スキャン中...",
        ["list.status.scanned"] = "{0} 件を検出（{1:0.0} 秒） ・ 最終スキャン {2}",
        ["list.status.failed"] = "スキャンに失敗しました: {0}",
        ["list.hide.failed"] = "透明化に失敗しました: {0}",

        // 透明化済み
        ["hidden.title"] = "透明化済みの項目",
        ["hidden.restore"] = "復元",
        ["hidden.deleteForever"] = "完全に削除",
        ["hidden.at"] = "透明化: ",
        ["hidden.empty"] = "透明化された項目はまだありません。",
        ["hidden.conflict.title"] = "neronga - 復元の確認",
        ["hidden.conflict.message"] = "元の場所に同名の項目が既に存在します。\n\n「はい」: 上書きして復元する\n「いいえ」: 別名で復元する\n「キャンセル」: 復元を中止する",
        ["hidden.restore.failed"] = "復元に失敗しました: {0}",
        ["hidden.deleteForever.title"] = "neronga - 完全削除の確認",
        ["hidden.deleteForever.message"] = "「{0}」({1}) を完全に削除します。この操作は取り消せません。よろしいですか？",
        ["hidden.delete.failed"] = "削除に失敗しました: {0}",

        // 編集履歴
        ["history.title"] = "編集履歴",
        ["history.size"] = "履歴の使用容量 {0}",
        ["history.deleteAll"] = "すべての履歴を削除",
        ["history.tooltip.delete"] = "この履歴を削除",
        ["history.edits"] = "{0} 回編集",
        ["history.empty.title"] = "まだ neronga から編集されたファイルはありません。",
        ["history.empty.hint"] = "アイテム一覧で指示ファイルやMCP設定の「編集」ボタンを押すと、ここに変更履歴が記録されます。",
        ["history.delete.title"] = "neronga - 履歴の削除",
        ["history.delete.message"] = "「{0}」の編集履歴（{1} 件）を削除します。\n\n対象: {2}\n\n実ファイルそのものは変更されません。削除されるのは neronga が保持している変更履歴だけです。\nこの操作は取り消せません。\n\n内部のGitオブジェクトはリポジトリ内に残りますが、以降この履歴が表示・復元されることはありません。実体ごと消去したい場合は「すべての履歴を削除」を使用してください。",
        ["history.deleted"] = "「{0}」の編集履歴を削除しました。",
        ["history.deleteAll.title"] = "neronga - すべての履歴を削除",
        ["history.deleteAll.message"] = "追跡中の {0} 件すべての編集履歴を完全に削除します。\n\n履歴リポジトリごと作り直すため、過去のバージョンは一切復元できなくなります。\n実ファイルそのものは変更されません。",
        ["history.deletedAll"] = "すべての編集履歴を削除しました。",
        ["history.delete.failed"] = "履歴の削除に失敗しました: {0}",
        ["history.load.failed"] = "変更履歴を読み込めませんでした: {0}",

        // 設定
        ["settings.title"] = "設定",
        ["settings.roots.title"] = "プロジェクトルート（再帰スキャン対象フォルダ）",
        ["settings.roots.desc"] = "ここに追加したフォルダの配下を再帰的に探索し、CLAUDE.md や AGENTS.md などプロジェクト単位の設定を検出します。",
        ["settings.roots.add"] = "フォルダを追加",
        ["settings.roots.dialog"] = "プロジェクトルートフォルダを選択",
        ["settings.scan.title"] = "スキャン設定",
        ["settings.scan.depth"] = "最大探索階層",
        ["settings.scan.excluded"] = "除外するフォルダ名（カンマ区切り）",
        ["settings.appearance.title"] = "外観",
        ["settings.appearance.theme"] = "テーマ",
        ["settings.appearance.language"] = "言語",
        ["settings.add.title"] = "監視パスを追加",
        ["settings.add.vendor"] = "ベンダー名（例: MyTool）",
        ["settings.add.label"] = "ラベル（例: グローバル指示ファイル）",
        ["settings.add.path"] = "パス（グローバル=絶対パス／プロジェクト=相対パターン）",
        ["settings.add.browse"] = "参照",
        ["settings.add.browse.dialog"] = "フォルダを選択（ファイルの場合は選択後にファイル名を追記してください）",
        ["settings.add.kind"] = "種別",
        ["settings.add.scope"] = "スコープ",
        ["settings.add.submit"] = "追加",
        ["settings.add.incomplete"] = "ベンダー名・ラベル・パスをすべて入力してください。",
        ["settings.entries.title"] = "監視対象パス一覧",
        ["settings.entries.reset"] = "既定値にリセット",
        ["settings.entries.reset.confirm"] = "パス設定をすべて既定値にリセットします。よろしいですか？（プロジェクトルートの一覧は保持されます）",
        ["settings.entry.userAdded"] = "ユーザー追加",

        // エディタ
        ["editor.history.title"] = "変更履歴（このアプリ内で完結）",
        ["editor.history.restore"] = "選択したバージョンをエディタへ復元",
        ["editor.diff.title"] = "差分（選択バージョンと1つ前の比較）",
        ["editor.save"] = "保存",
        ["editor.close"] = "閉じる",
        ["editor.status.loaded"] = "読み込み済み ・ {0}",
        ["editor.status.loadFailed"] = "読み込みに失敗しました: {0}",
        ["editor.status.saved"] = "保存しました ・ {0}",
        ["editor.status.savedNoHistory"] = "保存しました ・ ただし変更履歴の記録に失敗しました: {0}",
        ["editor.status.versionLoaded"] = "{0} の内容をエディタへ読み込みました。「保存」を押すと確定します。",
        ["editor.save.failed"] = "保存に失敗しました: {0}",
        ["editor.diff.first"] = "(これが最初に記録されたバージョンです)",
        ["editor.diff.noChange"] = "(このバージョンでは内容の変化はありません)",
        ["editor.commit.message"] = "編集: {0}",
        ["editor.unsaved.title"] = "neronga",
        ["editor.unsaved.message"] = "保存されていない変更があります。保存せずに閉じますか？",

        // 種別 / スコープ
        ["kind.skill"] = "スキル",
        ["kind.instruction"] = "指示ファイル",
        ["kind.mcp"] = "MCP設定",
        ["scope.global"] = "グローバル",
        ["scope.project"] = "プロジェクト",

        // 数量 / 時刻
        ["size.files"] = "{0} 個のファイル",
        ["time.justNow"] = "たった今",
        ["time.minutesAgo"] = "{0} 分前",
        ["time.hoursAgo"] = "{0} 時間前",
        ["time.daysAgo"] = "{0} 日前",

        // エラー
        ["error.title"] = "neronga - エラー",
        ["error.unexpected"] = "予期しないエラーが発生しました。\n\n{0}\n\n詳細ログ: {1}",
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        // Common
        ["app.name"] = "neronga",
        ["common.separator"] = " · ",
        ["common.refresh"] = "Refresh",
        ["common.delete"] = "Delete",
        ["common.open"] = "Open",
        ["common.ok"] = "OK",
        ["common.cancel"] = "Cancel",

        // Navigation
        ["nav.items"] = "Items",
        ["nav.hidden"] = "Transparent",
        ["nav.history"] = "History",
        ["nav.settings"] = "Settings",
        ["app.subtitle"] = "NERONGA",

        // Item list
        ["list.search.placeholder"] = "Search by vendor, file name, or path",
        ["list.rescan"] = "Rescan",
        ["list.hide"] = "Make transparent",
        ["list.tooltip.openFolder"] = "Open containing folder",
        ["list.tooltip.copyPath"] = "Copy path",
        ["list.tooltip.edit"] = "Edit (with revision history)",
        ["list.empty"] = "No matching items were found.",
        ["list.welcome.title"] = "Welcome to neronga",
        ["list.welcome.message"] = "Global configurations for each agent have already been detected. To also find per-project CLAUDE.md or AGENTS.md files, add your development folders in Settings.",
        ["list.noWorkspace.title"] = "No project folders registered",
        ["list.noWorkspace.message"] = "Only global configurations are being scanned. Add development folders in Settings to detect per-project configurations as well.",
        ["list.status.idle"] = "Ready to scan",
        ["list.status.scanning"] = "Scanning...",
        ["list.status.scanned"] = "{0} items found ({1:0.0}s) · last scan {2}",
        ["list.status.failed"] = "Scan failed: {0}",
        ["list.hide.failed"] = "Failed to make transparent: {0}",

        // Transparent
        ["hidden.title"] = "Transparent items",
        ["hidden.restore"] = "Restore",
        ["hidden.deleteForever"] = "Delete permanently",
        ["hidden.at"] = "Hidden: ",
        ["hidden.empty"] = "No items have been made transparent yet.",
        ["hidden.conflict.title"] = "neronga - Confirm restore",
        ["hidden.conflict.message"] = "An item with the same name already exists at the original location.\n\nYes: overwrite and restore\nNo: restore under a different name\nCancel: abort the restore",
        ["hidden.restore.failed"] = "Restore failed: {0}",
        ["hidden.deleteForever.title"] = "neronga - Confirm permanent deletion",
        ["hidden.deleteForever.message"] = "\"{0}\" ({1}) will be deleted permanently. This cannot be undone. Continue?",
        ["hidden.delete.failed"] = "Deletion failed: {0}",

        // History
        ["history.title"] = "Edit history",
        ["history.size"] = "History size {0}",
        ["history.deleteAll"] = "Delete all history",
        ["history.tooltip.delete"] = "Delete this history",
        ["history.edits"] = "{0} edits",
        ["history.empty.title"] = "No files have been edited through neronga yet.",
        ["history.empty.hint"] = "Use the Edit button on an instruction file or MCP config in Items, and changes will be recorded here.",
        ["history.delete.title"] = "neronga - Delete history",
        ["history.delete.message"] = "The edit history for \"{0}\" ({1} entries) will be deleted.\n\nTarget: {2}\n\nThe actual file is not modified. Only the revision history kept by neronga is removed.\nThis cannot be undone.\n\nThe underlying Git objects stay in the repository, but this history will never be shown or restored again. Use \"Delete all history\" to erase everything physically.",
        ["history.deleted"] = "Deleted the edit history for \"{0}\".",
        ["history.deleteAll.title"] = "neronga - Delete all history",
        ["history.deleteAll.message"] = "All edit history for the {0} tracked files will be permanently deleted.\n\nThe history repository is recreated from scratch, so no previous version can be recovered.\nThe actual files are not modified.",
        ["history.deletedAll"] = "Deleted all edit history.",
        ["history.delete.failed"] = "Failed to delete history: {0}",
        ["history.load.failed"] = "Could not load the edit history: {0}",

        // Settings
        ["settings.title"] = "Settings",
        ["settings.roots.title"] = "Project roots (scanned recursively)",
        ["settings.roots.desc"] = "Folders added here are searched recursively for per-project configurations such as CLAUDE.md and AGENTS.md.",
        ["settings.roots.add"] = "Add folder",
        ["settings.roots.dialog"] = "Select a project root folder",
        ["settings.scan.title"] = "Scan settings",
        ["settings.scan.depth"] = "Maximum depth",
        ["settings.scan.excluded"] = "Excluded folder names (comma separated)",
        ["settings.appearance.title"] = "Appearance",
        ["settings.appearance.theme"] = "Theme",
        ["settings.appearance.language"] = "Language",
        ["settings.add.title"] = "Add a watched path",
        ["settings.add.vendor"] = "Vendor (e.g. MyTool)",
        ["settings.add.label"] = "Label (e.g. Global instruction file)",
        ["settings.add.path"] = "Path (Global = absolute path / Project = relative pattern)",
        ["settings.add.browse"] = "Browse",
        ["settings.add.browse.dialog"] = "Select a folder (for a file, append the file name afterwards)",
        ["settings.add.kind"] = "Kind",
        ["settings.add.scope"] = "Scope",
        ["settings.add.submit"] = "Add",
        ["settings.add.incomplete"] = "Please fill in the vendor, label, and path.",
        ["settings.entries.title"] = "Watched paths",
        ["settings.entries.reset"] = "Reset to defaults",
        ["settings.entries.reset.confirm"] = "All path settings will be reset to their defaults. Continue? (Your project roots are kept.)",
        ["settings.entry.userAdded"] = "Custom",

        // Editor
        ["editor.history.title"] = "Revision history (kept inside this app)",
        ["editor.history.restore"] = "Load the selected version into the editor",
        ["editor.diff.title"] = "Diff (selected version vs the previous one)",
        ["editor.save"] = "Save",
        ["editor.close"] = "Close",
        ["editor.status.loaded"] = "Loaded · {0}",
        ["editor.status.loadFailed"] = "Failed to load: {0}",
        ["editor.status.saved"] = "Saved · {0}",
        ["editor.status.savedNoHistory"] = "Saved · but recording the revision history failed: {0}",
        ["editor.status.versionLoaded"] = "Loaded the version from {0} into the editor. Press Save to apply it.",
        ["editor.save.failed"] = "Save failed: {0}",
        ["editor.diff.first"] = "(This is the earliest recorded version.)",
        ["editor.diff.noChange"] = "(No content changed in this version.)",
        ["editor.commit.message"] = "edit: {0}",
        ["editor.unsaved.title"] = "neronga",
        ["editor.unsaved.message"] = "There are unsaved changes. Close without saving?",

        // Kind / scope
        ["kind.skill"] = "Skill",
        ["kind.instruction"] = "Instructions",
        ["kind.mcp"] = "MCP config",
        ["scope.global"] = "Global",
        ["scope.project"] = "Project",

        // Counts / time
        ["size.files"] = "{0} files",
        ["time.justNow"] = "just now",
        ["time.minutesAgo"] = "{0} min ago",
        ["time.hoursAgo"] = "{0} h ago",
        ["time.daysAgo"] = "{0} d ago",

        // Errors
        ["error.title"] = "neronga - Error",
        ["error.unexpected"] = "An unexpected error occurred.\n\n{0}\n\nLog: {1}",
    };
}
