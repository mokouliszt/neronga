using System.Windows;
using System.Windows.Media;
using Neronga.Localization;
using Neronga.Services;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Neronga;

public partial class App : Application
{
    /// <summary>の黄色をアクセントカラーとして常に使用する。</summary>
    private static readonly Color BrandAccent = Color.FromRgb(0xFE, 0xE7, 0x04);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            TryLogFatal(args.ExceptionObject as Exception);
        };
        DispatcherUnhandledException += (_, args) =>
        {
            ReportError(args.Exception);
            args.Handled = true;
        };

        try
        {
            // 設定を読み込んでおく（初回起動時の既定値作成もここで走る）
            var settings = AppServices.Settings.Settings;
            Loc.Instance.SetLanguage(settings.Language);
            ApplicationThemeManager.Changed += OnApplicationThemeChanged;
            ApplyTheme(settings.Theme);

            var window = new MainWindow();
            window.Show();
        }
        catch (Exception ex)
        {
            ReportError(ex);
            Shutdown(1);
        }
    }

    public static void ApplyTheme(string themeName)
    {
        var theme = themeName switch
        {
            "Light" => ApplicationTheme.Light,
            "Dark" => ApplicationTheme.Dark,
            _ => ApplicationThemeManager.GetSystemTheme() == SystemTheme.Dark
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light
        };

        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, updateAccent: false);
        EnsureBrandAccent(theme);
    }

    private static bool _applyingAccent;

    private static readonly SolidColorBrush InkBrush = CreateFrozen(0x1A, 0x1A, 0x17);
    private static readonly SolidColorBrush InkSoftBrush = CreateFrozen(0x2E, 0x2E, 0x29);
    private static readonly SolidColorBrush AccentTextBrush = CreateFrozen(0x9A, 0x82, 0x00);

    private static SolidColorBrush CreateFrozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// テーマを切り替えると WPF-UI がアクセント色をWindowsの既定色（青）へ戻してしまうため、
    /// 起動時とテーマ変更時の両方でブランド色を必ず上書きし直す。
    /// </summary>
    private static void EnsureBrandAccent(ApplicationTheme theme)
    {
        if (_applyingAccent) return;
        _applyingAccent = true;
        try
        {
            ApplicationAccentColorManager.Apply(BrandAccent, theme, systemGlassColor: false);
            ApplyAccentForegroundOverrides();
        }
        finally
        {
            _applyingAccent = false;
        }
    }

    /// <summary>
    /// アクセント（明るい黄色）の上に白文字が乗ると読めないため、濃色へ差し替える。
    /// テーマ辞書の差し替え順に左右されないよう、マージ辞書より優先される
    /// Application.Resources 直下へ設定する。
    /// </summary>
    private static void ApplyAccentForegroundOverrides()
    {
        var resources = Current?.Resources;
        if (resources is null) return;

        resources["TextOnAccentFillColorPrimaryBrush"] = InkBrush;
        resources["TextOnAccentFillColorSecondaryBrush"] = InkSoftBrush;
        resources["TextOnAccentFillColorSelectedTextBrush"] = InkBrush;
        resources["AccentTextFillColorPrimaryBrush"] = AccentTextBrush;
        resources["AccentTextFillColorSecondaryBrush"] = AccentTextBrush;
    }

    private static void OnApplicationThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
        => EnsureBrandAccent(currentApplicationTheme);

    /// <summary>
    /// 例外の内側まで辿って原因を特定できる形で表示し、あわせて crash.log にも残す。
    /// XamlParseException の場合は発生箇所の行番号も出す。
    /// </summary>
    internal static void ReportError(Exception ex)
    {
        var detail = BuildExceptionDetail(ex);
        TryLogFatal(ex);

        var logPath = System.IO.Path.Combine(PathTokenResolver.NerongaDataRoot, "crash.log");
        System.Windows.MessageBox.Show(
            Loc.F("error.unexpected", detail, logPath),
            Loc.S("error.title"), System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    private static string BuildExceptionDetail(Exception? ex)
    {
        var sb = new System.Text.StringBuilder();
        var depth = 0;
        while (ex is not null && depth < 8)
        {
            sb.AppendLine($"[{depth}] {ex.GetType().Name}: {ex.Message}");
            if (ex is System.Windows.Markup.XamlParseException xpe)
            {
                sb.AppendLine($"    XAML {xpe.BaseUri} 行 {xpe.LineNumber}, 位置 {xpe.LinePosition}");
            }
            ex = ex.InnerException;
            depth++;
        }
        return sb.ToString().TrimEnd();
    }

    private static void TryLogFatal(Exception? ex)
    {
        try
        {
            System.IO.Directory.CreateDirectory(PathTokenResolver.NerongaDataRoot);
            var path = System.IO.Path.Combine(PathTokenResolver.NerongaDataRoot, "crash.log");
            System.IO.File.AppendAllText(path, $"{DateTime.Now:O}\n{ex}\n\n");
        }
        catch { /* もう打つ手なし */ }
    }
}
