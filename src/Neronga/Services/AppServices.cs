namespace Neronga.Services;

/// <summary>
/// アプリ規模的にフルDIコンテナは不要と判断し、シンプルなサービスロケータで統一する。
/// </summary>
public static class AppServices
{
    private static SettingsStore? _settings;
    private static ScannerService? _scanner;
    private static VaultService? _vault;
    private static HistoryService? _history;

    public static SettingsStore Settings => _settings ??= new SettingsStore();
    public static ScannerService Scanner => _scanner ??= new ScannerService();
    public static VaultService Vault => _vault ??= new VaultService();
    public static HistoryService History => _history ??= new HistoryService();
}
