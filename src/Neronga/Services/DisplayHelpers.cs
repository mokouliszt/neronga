using Neronga.Localization;
using Neronga.Models;

namespace Neronga.Services;

public static class DisplayHelpers
{
    public static string KindLabel(EntryKind kind) => kind switch
    {
        EntryKind.SkillsFolder => Loc.S("kind.skill"),
        EntryKind.InstructionFile => Loc.S("kind.instruction"),
        EntryKind.InstructionFolder => Loc.S("kind.instruction"),
        EntryKind.McpConfigFile => Loc.S("kind.mcp"),
        _ => kind.ToString()
    };

    public static string ScopeLabel(ScopeType scope) => scope switch
    {
        ScopeType.Global => Loc.S("scope.global"),
        ScopeType.Project => Loc.S("scope.project"),
        _ => scope.ToString()
    };

    public static string FormatSize(long? bytesOrCount, bool isDirectory)
    {
        if (bytesOrCount is null) return string.Empty;
        if (isDirectory) return Loc.F("size.files", bytesOrCount.Value);

        double v = bytesOrCount.Value;
        string[] units = { "B", "KB", "MB", "GB" };
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return u == 0 ? $"{v:0} {units[u]}" : $"{v:0.0} {units[u]}";
    }

    public static string FormatRelativeOrDate(DateTime utc)
    {
        var local = utc.ToLocalTime();
        var span = DateTime.Now - local;
        if (span.TotalMinutes < 1) return Loc.S("time.justNow");
        if (span.TotalMinutes < 60) return Loc.F("time.minutesAgo", (int)span.TotalMinutes);
        if (span.TotalHours < 24) return Loc.F("time.hoursAgo", (int)span.TotalHours);
        if (span.TotalDays < 7) return Loc.F("time.daysAgo", (int)span.TotalDays);
        return local.ToString("yyyy-MM-dd HH:mm");
    }
}
