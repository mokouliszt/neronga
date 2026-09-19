namespace Neronga.Models;

using System.IO;

/// <summary>
/// スキャン結果として得られる、実際にディスク上に存在する1件の項目。
/// SkillsFolder や InstructionFolder の場合、中身のサブ項目ごとに1つずつ生成される
/// （＝ユーザーが個別に透明化できる単位）。
/// </summary>
public sealed class ScannedItem
{
    public required string Vendor { get; init; }
    public required string Label { get; init; }
    public required EntryKind Kind { get; init; }
    public required ScopeType Scope { get; init; }
    public required string FullPath { get; init; }
    public required bool IsDirectory { get; init; }

    /// <summary>ファイルの場合はバイト数。フォルダの場合は内包ファイル数の概算。</summary>
    public long? SizeInfo { get; init; }

    public DateTime LastModifiedUtc { get; init; }

    /// <summary>この項目を生んだ定義のID（Global）。Project由来の場合は null。</summary>
    public string? SourceEntryId { get; init; }

    /// <summary>Project スコープの場合、どのワークスペースルートから見つかったか。</summary>
    public string? WorkspaceRoot { get; init; }

    /// <summary>このファイル自体をテキストエディタで編集できるか（フォルダは不可）。</summary>
    public bool IsEditable => !IsDirectory &&
        (Kind == EntryKind.InstructionFile || Kind == EntryKind.McpConfigFile || Kind == EntryKind.InstructionFolder);

    /// <summary>この定義由来の補足メモ。</summary>
    public string? Note { get; init; }

    public string DisplayName => Path.GetFileName(FullPath.TrimEnd('\\', '/'));
}
