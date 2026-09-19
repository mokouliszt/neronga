using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Neronga.Models;
using Neronga.Services;

namespace Neronga.Converters;

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value!;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value!;
}

/// <summary>true のとき Collapsed、false のとき Visible（"組み込みではない" バッジの表示切替用）。</summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>数値が 0 のとき Visible、それ以外は Collapsed（「該当なし」メッセージ表示用）。</summary>
public sealed class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class EntryKindLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is EntryKind k ? DisplayHelpers.KindLabel(k) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ScopeTypeLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ScopeType s ? DisplayHelpers.ScopeLabel(s) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>複数行のテキストを行の配列に分割する（diffビューでの行単位描画用）。</summary>
public sealed class SplitLinesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? s.Replace("\r\n", "\n").Split('\n') : Array.Empty<string>();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>unified diff の行頭記号に応じて表示色を返す（テーマ対応）。</summary>
public sealed class DiffLineToBrushConverter : IValueConverter
{
    // GitHub の diff 配色に準拠。ダーク/ライトで明度を切り替える。
    private static readonly System.Windows.Media.Brush AddedDark = Freeze(0x7E, 0xE7, 0x87);
    private static readonly System.Windows.Media.Brush RemovedDark = Freeze(0xFF, 0x7B, 0x72);
    private static readonly System.Windows.Media.Brush HunkDark = Freeze(0x8B, 0x94, 0x9E);
    private static readonly System.Windows.Media.Brush AddedLight = Freeze(0x1A, 0x7F, 0x37);
    private static readonly System.Windows.Media.Brush RemovedLight = Freeze(0xCF, 0x22, 0x2E);
    private static readonly System.Windows.Media.Brush HunkLight = Freeze(0x6E, 0x77, 0x81);

    private static System.Windows.Media.Brush Freeze(byte r, byte g, byte b)
    {
        var brush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var line = value as string ?? string.Empty;
        var isDark = Wpf.Ui.Appearance.ApplicationThemeManager.GetAppTheme()
                     == Wpf.Ui.Appearance.ApplicationTheme.Dark;

        // ファイルヘッダーとハンクヘッダーを先に判定する（"---" は "-" にも一致するため）。
        if (line.StartsWith("+++") || line.StartsWith("---") || line.StartsWith("@@"))
            return isDark ? HunkDark : HunkLight;
        if (line.StartsWith("+"))
            return isDark ? AddedDark : AddedLight;
        if (line.StartsWith("-"))
            return isDark ? RemovedDark : RemovedLight;

        // 変更のない行はテーマの既定文字色を継承させる。
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
