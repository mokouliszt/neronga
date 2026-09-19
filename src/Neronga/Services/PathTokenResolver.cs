using System.IO;

namespace Neronga.Services;

/// <summary>
/// PathEntryDefinition.PathTemplate 内の %USERPROFILE% 等のトークンを実際のパスへ展開する。
/// </summary>
public static class PathTokenResolver
{
    public static string Resolve(string template)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);       // Roaming
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var result = template
            .Replace("%USERPROFILE%", userProfile, StringComparison.OrdinalIgnoreCase)
            .Replace("%APPDATA%", appData, StringComparison.OrdinalIgnoreCase)
            .Replace("%LOCALAPPDATA%", localAppData, StringComparison.OrdinalIgnoreCase)
            .Replace("%DOCUMENTS%", documents, StringComparison.OrdinalIgnoreCase);

        // テンプレートは '/' 区切りで書かれることがあるため、実行環境のセパレータへ正規化する。
        result = result.Replace('/', Path.DirectorySeparatorChar);
        return result;
    }

    /// <summary>neronga 自身のデータルート（%LocalAppData%\neronga）。</summary>
    public static string NerongaDataRoot
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "neronga");
        }
    }

    public static string VaultRoot => Path.Combine(NerongaDataRoot, "vault");
    public static string HistoryRepoRoot => Path.Combine(NerongaDataRoot, "history");
    public static string ConfigFilePath => Path.Combine(NerongaDataRoot, "config.json");
}
