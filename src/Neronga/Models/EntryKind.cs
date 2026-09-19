namespace Neronga.Models;

/// <summary>
/// neronga が対象とする4種類のパス種別。
/// </summary>
public enum EntryKind
{
    /// <summary>Agent Skills を格納するフォルダ（中に SKILL.md を持つサブフォルダが並ぶ）。</summary>
    SkillsFolder,

    /// <summary>単一のカスタム指示ファイル（CLAUDE.md, AGENTS.md 等）。</summary>
    InstructionFile,

    /// <summary>複数のカスタム指示/ルールファイルを格納するフォルダ（rules/, .clinerules/ 等）。</summary>
    InstructionFolder,

    /// <summary>MCPサーバー設定を含む設定ファイル（json / toml 等）。</summary>
    McpConfigFile
}

/// <summary>
/// パスがユーザーのホーム配下に固定されるグローバル設定か、
/// 各プロジェクトルート配下の相対パターンとして探すプロジェクト設定か。
/// </summary>
public enum ScopeType
{
    Global,
    Project
}
