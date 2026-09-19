using Neronga.Models;
using System.IO;

namespace Neronga.Services;

/// <summary>
/// 各エージェントベンダーの既定パスのカタログ。
/// このバージョンを上げると、既存ユーザーの設定に新しい既定値が自動マージされる
///（ただしユーザーが明示的に削除したIDは復活しない）。
/// </summary>
public static class DefaultCatalog
{
    public const int CurrentVersion = 1;

    public static List<PathEntryDefinition> BuildDefaults()
    {
        var list = new List<PathEntryDefinition>();

        void Add(string id, string vendor, string label, EntryKind kind, ScopeType scope, string path, string? note = null)
        {
            list.Add(new PathEntryDefinition
            {
                Id = id,
                Vendor = vendor,
                Label = label,
                Kind = kind,
                Scope = scope,
                PathTemplate = path,
                Enabled = true,
                IsBuiltIn = true,
                Note = note
            });
        }

        // ===================== Claude Code =====================
        const string claude = "Claude Code";
        Add("claudecode.global.skills", claude, "グローバル スキル (skills/)", EntryKind.SkillsFolder, ScopeType.Global, "%USERPROFILE%/.claude/skills");
        Add("claudecode.global.claudemd", claude, "グローバル指示ファイル (CLAUDE.md)", EntryKind.InstructionFile, ScopeType.Global, "%USERPROFILE%/.claude/CLAUDE.md");
        Add("claudecode.global.rules", claude, "グローバル ルール (rules/)", EntryKind.InstructionFolder, ScopeType.Global, "%USERPROFILE%/.claude/rules");
        Add("claudecode.global.commands", claude, "グローバル コマンド (commands/)", EntryKind.InstructionFolder, ScopeType.Global, "%USERPROFILE%/.claude/commands");
        Add("claudecode.global.agents", claude, "グローバル サブエージェント (agents/)", EntryKind.InstructionFolder, ScopeType.Global, "%USERPROFILE%/.claude/agents");
        Add("claudecode.global.outputstyles", claude, "グローバル 出力スタイル (output-styles/)", EntryKind.InstructionFolder, ScopeType.Global, "%USERPROFILE%/.claude/output-styles");
        Add("claudecode.global.mcp", claude, "個人MCPサーバー設定 (.claude.json)", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.claude.json",
            "MCPサーバー設定のほか、ログイン状態やUI設定も含まれるファイルです。");

        Add("claudecode.project.claudemd", claude, "プロジェクト指示ファイル (CLAUDE.md)", EntryKind.InstructionFile, ScopeType.Project, "CLAUDE.md");
        Add("claudecode.project.claudemd.local", claude, "プロジェクト個人用指示 (CLAUDE.local.md)", EntryKind.InstructionFile, ScopeType.Project, "CLAUDE.local.md");
        Add("claudecode.project.mcp", claude, "プロジェクトMCPサーバー設定 (.mcp.json)", EntryKind.McpConfigFile, ScopeType.Project, ".mcp.json");
        Add("claudecode.project.skills", claude, "プロジェクト スキル (.claude/skills/)", EntryKind.SkillsFolder, ScopeType.Project, ".claude/skills");
        Add("claudecode.project.rules", claude, "プロジェクト ルール (.claude/rules/)", EntryKind.InstructionFolder, ScopeType.Project, ".claude/rules");
        Add("claudecode.project.commands", claude, "プロジェクト コマンド (.claude/commands/)", EntryKind.InstructionFolder, ScopeType.Project, ".claude/commands");
        Add("claudecode.project.agents", claude, "プロジェクト サブエージェント (.claude/agents/)", EntryKind.InstructionFolder, ScopeType.Project, ".claude/agents");

        // ===================== OpenAI Codex CLI =====================
        const string codex = "OpenAI Codex CLI";
        Add("codex.global.config", codex, "設定 / MCPサーバー (config.toml)", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.codex/config.toml",
            "MCPサーバー設定のほか、モデルや承認ポリシーなどの一般設定も含みます。");
        Add("codex.global.skills", codex, "グローバル スキル (skills/)", EntryKind.SkillsFolder, ScopeType.Global, "%USERPROFILE%/.codex/skills");
        Add("codex.global.prompts", codex, "カスタムプロンプト (prompts/)", EntryKind.InstructionFolder, ScopeType.Global, "%USERPROFILE%/.codex/prompts");

        Add("codex.project.config", codex, "プロジェクト設定 / MCP (.codex/config.toml)", EntryKind.McpConfigFile, ScopeType.Project, ".codex/config.toml");
        Add("codex.project.skills", codex, "プロジェクト スキル (.codex/skills/)", EntryKind.SkillsFolder, ScopeType.Project, ".codex/skills");

        // ===================== GitHub Copilot =====================
        const string copilot = "GitHub Copilot";
        Add("copilot.global.instructions.userprofile", copilot, "個人指示 (copilot-instructions.md)", EntryKind.InstructionFile, ScopeType.Global, "%USERPROFILE%/copilot-instructions.md");
        Add("copilot.global.cli.instructions", copilot, "Copilot CLI 個人指示", EntryKind.InstructionFile, ScopeType.Global, "%USERPROFILE%/.copilot/copilot-instructions.md");
        Add("copilot.global.cli.mcp", copilot, "Copilot CLI MCPサーバー設定", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.copilot/mcp-config.json");
        Add("copilot.global.vscode.mcp", copilot, "VS Code MCPサーバー設定 (グローバル)", EntryKind.McpConfigFile, ScopeType.Global, "%APPDATA%/Code/User/mcp.json",
            "VS Code全体で共有される設定です。Copilot専用のファイルではありません。");

        Add("copilot.project.instructions", copilot, "リポジトリ指示 (.github/copilot-instructions.md)", EntryKind.InstructionFile, ScopeType.Project, ".github/copilot-instructions.md");
        Add("copilot.project.instructions.folder", copilot, "パス別指示 (.github/instructions/)", EntryKind.InstructionFolder, ScopeType.Project, ".github/instructions");
        Add("copilot.project.agents", copilot, "エージェント定義 (.github/agents/)", EntryKind.InstructionFolder, ScopeType.Project, ".github/agents");
        Add("copilot.project.vscode.mcp", copilot, "VS Code MCPサーバー設定 (プロジェクト)", EntryKind.McpConfigFile, ScopeType.Project, ".vscode/mcp.json");

        // ===================== Google (Gemini CLI / Antigravity) =====================
        const string google = "Google (Gemini CLI / Antigravity)";
        Add("google.global.instructions", google, "グローバル指示 (GEMINI.md)", EntryKind.InstructionFile, ScopeType.Global, "%USERPROFILE%/.gemini/GEMINI.md");
        Add("google.global.mcp.antigravity", google, "Antigravity MCPサーバー設定", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.gemini/config/mcp_config.json");
        Add("google.global.mcp.legacy", google, "Gemini CLI(旧) 設定 / MCP (settings.json)", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.gemini/settings.json",
            "旧 Gemini CLI 用の設定ファイルです。Antigravity 移行後は使われない場合があります。");
        Add("google.global.skills", google, "Antigravity グローバル スキル (config/skills/)", EntryKind.SkillsFolder, ScopeType.Global, "%USERPROFILE%/.gemini/config/skills");
        Add("google.global.agents", google, "Antigravity グローバル エージェント (config/agents/)", EntryKind.InstructionFolder, ScopeType.Global, "%USERPROFILE%/.gemini/config/agents");

        Add("google.project.mcp", google, "プロジェクト MCPサーバー設定 (.agents/mcp_config.json)", EntryKind.McpConfigFile, ScopeType.Project, ".agents/mcp_config.json");

        // ===================== Cursor =====================
        const string cursor = "Cursor";
        Add("cursor.global.mcp", cursor, "グローバル MCPサーバー設定", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.cursor/mcp.json");
        Add("cursor.project.mcp", cursor, "プロジェクト MCPサーバー設定", EntryKind.McpConfigFile, ScopeType.Project, ".cursor/mcp.json");
        Add("cursor.project.rules", cursor, "プロジェクト ルール (.cursor/rules/)", EntryKind.InstructionFolder, ScopeType.Project, ".cursor/rules");
        Add("cursor.project.legacyrules", cursor, "プロジェクト ルール(旧形式) (.cursorrules)", EntryKind.InstructionFile, ScopeType.Project, ".cursorrules");

        // ===================== Windsurf =====================
        const string windsurf = "Windsurf";
        Add("windsurf.global.mcp", windsurf, "グローバル MCPサーバー設定", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.codeium/windsurf/mcp_config.json");
        Add("windsurf.global.rules", windsurf, "グローバル ルール (global_rules.md)", EntryKind.InstructionFile, ScopeType.Global, "%USERPROFILE%/.codeium/windsurf/memories/global_rules.md");
        Add("windsurf.project.rules", windsurf, "プロジェクト ルール (.windsurf/rules/)", EntryKind.InstructionFolder, ScopeType.Project, ".windsurf/rules");
        Add("windsurf.project.legacyrules", windsurf, "プロジェクト ルール(旧形式) (.windsurfrules)", EntryKind.InstructionFile, ScopeType.Project, ".windsurfrules");

        // ===================== Cline =====================
        const string cline = "Cline";
        Add("cline.global.rules", cline, "グローバル ルール (Documents\\Cline\\Rules)", EntryKind.InstructionFolder, ScopeType.Global, "%DOCUMENTS%/Cline/Rules");
        Add("cline.global.mcp", cline, "MCPサーバー設定 (VS Code拡張ストレージ)", EntryKind.McpConfigFile, ScopeType.Global,
            "%APPDATA%/Code/User/globalStorage/saoudrizwan.claude-dev/settings/cline_mcp_settings.json",
            "VS Code版Clineの内部ストレージです。VS Codeのバリエーション(Insiders等)により実際の場所が異なる場合があります。");
        Add("cline.project.rules", cline, "プロジェクト ルール (.clinerules/)", EntryKind.InstructionFolder, ScopeType.Project, ".clinerules");

        // ===================== Roo Code =====================
        const string roocode = "Roo Code";
        Add("roocode.project.mcp", roocode, "プロジェクト MCPサーバー設定 (.roo/mcp.json)", EntryKind.McpConfigFile, ScopeType.Project, ".roo/mcp.json");
        Add("roocode.project.rules", roocode, "プロジェクト ルール (.roo/rules/)", EntryKind.InstructionFolder, ScopeType.Project, ".roo/rules");

        // ===================== Continue.dev =====================
        const string continueDev = "Continue.dev";
        Add("continue.global.config", continueDev, "グローバル設定 / MCP (config.yaml)", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.continue/config.yaml");
        Add("continue.global.rules", continueDev, "グローバル ルール (rules/)", EntryKind.InstructionFolder, ScopeType.Global, "%USERPROFILE%/.continue/rules");
        Add("continue.project.rules", continueDev, "プロジェクト ルール (.continue/rules/)", EntryKind.InstructionFolder, ScopeType.Project, ".continue/rules");

        // ===================== Amazon Q Developer CLI (best effort) =====================
        const string amazonq = "Amazon Q Developer CLI";
        Add("amazonq.global.mcp", amazonq, "グローバル MCPサーバー設定", EntryKind.McpConfigFile, ScopeType.Global, "%USERPROFILE%/.aws/amazonq/mcp.json",
            "未検証のパスを含みます。環境によって異なる場合があります。");

        // ===================== JetBrains AI Assistant / Junie (best effort) =====================
        const string junie = "JetBrains AI Assistant (Junie)";
        Add("junie.project.guidelines", junie, "プロジェクト指示 (.junie/guidelines.md)", EntryKind.InstructionFile, ScopeType.Project, ".junie/guidelines.md",
            "未検証のパスを含みます。");

        // ===================== 共通 (AGENTS.md / Agent Skills 標準) =====================
        const string universal = "共通 (AGENTS.md 標準)";
        Add("universal.project.agentsmd", universal, "共通プロジェクト指示 (AGENTS.md)", EntryKind.InstructionFile, ScopeType.Project, "AGENTS.md");
        Add("universal.project.skills", universal, "共通 Agent Skills (.agents/skills/)", EntryKind.SkillsFolder, ScopeType.Project, ".agents/skills");
        Add("universal.global.skills", universal, "共通 グローバル Agent Skills (~/.agents/skills/)", EntryKind.SkillsFolder, ScopeType.Global, "%USERPROFILE%/.agents/skills");

        return list;
    }


    /// <summary>
    /// 組み込みエントリの英語ラベル（キーはエントリID）。
    /// ラベルは config.json に保存されるため、言語ごとに保存し直すのではなく
    /// 表示時にIDから引き直す方式にしている。
    /// </summary>
    private static readonly Dictionary<string, string> EnglishLabels = new(StringComparer.Ordinal)
    {
        ["claudecode.global.skills"] = "Global skills (skills/)",
        ["claudecode.global.claudemd"] = "Global instructions (CLAUDE.md)",
        ["claudecode.global.rules"] = "Global rules (rules/)",
        ["claudecode.global.commands"] = "Global commands (commands/)",
        ["claudecode.global.agents"] = "Global subagents (agents/)",
        ["claudecode.global.outputstyles"] = "Global output styles (output-styles/)",
        ["claudecode.global.mcp"] = "Personal MCP servers (.claude.json)",
        ["claudecode.project.claudemd"] = "Project instructions (CLAUDE.md)",
        ["claudecode.project.claudemd.local"] = "Project personal instructions (CLAUDE.local.md)",
        ["claudecode.project.mcp"] = "Project MCP servers (.mcp.json)",
        ["claudecode.project.skills"] = "Project skills (.claude/skills/)",
        ["claudecode.project.rules"] = "Project rules (.claude/rules/)",
        ["claudecode.project.commands"] = "Project commands (.claude/commands/)",
        ["claudecode.project.agents"] = "Project subagents (.claude/agents/)",
        ["codex.global.config"] = "Settings / MCP servers (config.toml)",
        ["codex.global.skills"] = "Global skills (skills/)",
        ["codex.global.prompts"] = "Custom prompts (prompts/)",
        ["codex.project.config"] = "Project settings / MCP (.codex/config.toml)",
        ["codex.project.skills"] = "Project skills (.codex/skills/)",
        ["copilot.global.instructions.userprofile"] = "Personal instructions (copilot-instructions.md)",
        ["copilot.global.cli.instructions"] = "Copilot CLI personal instructions",
        ["copilot.global.cli.mcp"] = "Copilot CLI MCP servers",
        ["copilot.global.vscode.mcp"] = "VS Code MCP servers (global)",
        ["copilot.project.instructions"] = "Repository instructions (.github/copilot-instructions.md)",
        ["copilot.project.instructions.folder"] = "Path-specific instructions (.github/instructions/)",
        ["copilot.project.agents"] = "Agent definitions (.github/agents/)",
        ["copilot.project.vscode.mcp"] = "VS Code MCP servers (project)",
        ["google.global.instructions"] = "Global instructions (GEMINI.md)",
        ["google.global.mcp.antigravity"] = "Antigravity MCP servers",
        ["google.global.mcp.legacy"] = "Gemini CLI (legacy) settings / MCP (settings.json)",
        ["google.global.skills"] = "Antigravity global skills (config/skills/)",
        ["google.global.agents"] = "Antigravity global agents (config/agents/)",
        ["google.project.mcp"] = "Project MCP servers (.agents/mcp_config.json)",
        ["cursor.global.mcp"] = "Global MCP servers",
        ["cursor.project.mcp"] = "Project MCP servers",
        ["cursor.project.rules"] = "Project rules (.cursor/rules/)",
        ["cursor.project.legacyrules"] = "Project rules, legacy (.cursorrules)",
        ["windsurf.global.mcp"] = "Global MCP servers",
        ["windsurf.global.rules"] = "Global rules (global_rules.md)",
        ["windsurf.project.rules"] = "Project rules (.windsurf/rules/)",
        ["windsurf.project.legacyrules"] = "Project rules, legacy (.windsurfrules)",
        ["cline.global.rules"] = "Global rules (Documents\\Cline\\Rules)",
        ["cline.global.mcp"] = "MCP servers (VS Code extension storage)",
        ["cline.project.rules"] = "Project rules (.clinerules/)",
        ["roocode.project.mcp"] = "Project MCP servers (.roo/mcp.json)",
        ["roocode.project.rules"] = "Project rules (.roo/rules/)",
        ["continue.global.config"] = "Global settings / MCP (config.yaml)",
        ["continue.global.rules"] = "Global rules (rules/)",
        ["continue.project.rules"] = "Project rules (.continue/rules/)",
        ["amazonq.global.mcp"] = "Global MCP servers",
        ["junie.project.guidelines"] = "Project instructions (.junie/guidelines.md)",
        ["universal.project.agentsmd"] = "Shared project instructions (AGENTS.md)",
        ["universal.project.skills"] = "Shared Agent Skills (.agents/skills/)",
        ["universal.global.skills"] = "Shared global Agent Skills (~/.agents/skills/)",
    };

    /// <summary>日本語表記を含むベンダー名の英語表記。</summary>
    private static readonly Dictionary<string, string> EnglishVendors = new(StringComparer.Ordinal)
    {
        ["共通 (AGENTS.md 標準)"] = "Common (AGENTS.md standard)",
    };

    /// <summary>組み込みエントリの補足メモの英語版。</summary>
    private static readonly Dictionary<string, string> EnglishNotes = new(StringComparer.Ordinal)
    {
        ["claudecode.global.mcp"] = "Besides MCP servers, this file also holds sign-in state and UI settings.",
        ["codex.global.config"] = "Besides MCP servers, this file also holds general settings such as the model and approval policy.",
        ["copilot.global.vscode.mcp"] = "Shared across all of VS Code. This is not a Copilot-specific file.",
        ["google.global.mcp.legacy"] = "Used by the legacy Gemini CLI. It may be unused after moving to Antigravity.",
        ["cline.global.mcp"] = "Internal storage of the VS Code version of Cline. The actual location differs between VS Code variants such as Insiders.",
        ["amazonq.global.mcp"] = "Contains an unverified path. It may differ in your environment.",
        ["junie.project.guidelines"] = "Contains an unverified path.",
    };

    public static string? EnglishLabelFor(string id) =>
        EnglishLabels.TryGetValue(id, out var v) ? v : null;

    public static string? EnglishVendorFor(string vendor) =>
        EnglishVendors.TryGetValue(vendor, out var v) ? v : null;

    public static string? EnglishNoteFor(string id) =>
        EnglishNotes.TryGetValue(id, out var v) ? v : null;

    /// <summary>初回起動時、存在すれば自動的にワークスペースルートへ追加する「よくある」開発フォルダ候補。</summary>
    public static IEnumerable<string> WellKnownWorkspaceRootCandidates()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        yield return Path.Combine(userProfile, "source", "repos");
        yield return Path.Combine(documents, "GitHub");
        yield return Path.Combine(userProfile, "Projects");
        yield return Path.Combine(userProfile, "projects");
        yield return Path.Combine(userProfile, "dev");
        yield return Path.Combine(userProfile, "git");
        yield return Path.Combine(userProfile, "repos");
        yield return Path.Combine(userProfile, "workspace");
    }
}
