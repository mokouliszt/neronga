using Neronga.Models;

namespace Neronga.ViewModels;

/// <summary>
/// アイテム一覧・履歴ページのどちらから編集を要求されても、同じエディタウィンドウを開けるようにするための共通情報。
/// </summary>
public sealed record EditTarget(string FullPath, string Vendor, string Label, EntryKind Kind);
