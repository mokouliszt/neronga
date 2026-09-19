using Neronga.Localization;
using Neronga.Services;

namespace Neronga.Models;

/// <summary>
/// 監視対象のパス定義。既定値（IsBuiltIn = true）とユーザー追加分の両方をこの型で表す。
/// Global の場合 PathTemplate は %USERPROFILE% 等のトークンを含む絶対パス。
/// Project の場合 PathTemplate はワークスペースルートからの相対パターン（例: "AGENTS.md", ".claude/skills"）。
/// </summary>
public sealed class PathEntryDefinition
{
    /// <summary>安定したID。ユーザーの無効化/削除状態の追跡や、既定値マージのキーに使う。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>ベンダー表示名（グループ化に使用）。例: "Claude Code"。</summary>
    public string Vendor { get; set; } = string.Empty;

    /// <summary>この項目の人間向けラベル。例: "グローバル スキル"。</summary>
    public string Label { get; set; } = string.Empty;

    public EntryKind Kind { get; set; }

    public ScopeType Scope { get; set; }

    public string PathTemplate { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public bool IsBuiltIn { get; set; }

    /// <summary>補足事項（例: このファイルはMCP設定以外の一般設定も含む、等の注意書き）。</summary>
    public string? Note { get; set; }

    // 以下は表示用。ラベル等は config.json に保存されるため、英語表記は保存値ではなく
    // IDからカタログを引き直して解決する（既存の設定ファイルでもそのまま英語化される）。

    public string DisplayVendor =>
        Loc.Instance.IsEnglish ? DefaultCatalog.EnglishVendorFor(Vendor) ?? Vendor : Vendor;

    public string DisplayLabel =>
        Loc.Instance.IsEnglish && IsBuiltIn ? DefaultCatalog.EnglishLabelFor(Id) ?? Label : Label;

    public string? DisplayNote =>
        Loc.Instance.IsEnglish && IsBuiltIn ? DefaultCatalog.EnglishNoteFor(Id) ?? Note : Note;
}
