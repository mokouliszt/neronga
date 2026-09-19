namespace Neronga.Models;

/// <summary>
/// %LocalAppData%\neronga\vault\&lt;uuid&gt;\__neronga_meta.json の内容。
/// 透明化（一時退避）した項目を元の場所へ復元するための情報を保持する。
/// </summary>
public sealed class HiddenEntryMeta
{
    public string VaultId { get; set; } = string.Empty;
    public string OriginalFullPath { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public EntryKind Kind { get; set; }
    public bool WasDirectory { get; set; }
    public DateTime HiddenAtUtc { get; set; }

    /// <summary>vault フォルダ内での実体名（元のファイル/フォルダ名と同じ）。</summary>
    public string StoredName { get; set; } = string.Empty;
}
