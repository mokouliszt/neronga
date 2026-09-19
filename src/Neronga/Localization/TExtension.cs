using System.Windows.Data;
using System.Windows.Markup;

namespace Neronga.Localization;

/// <summary>
/// XAML で {loc:T list.rescan} のように書くための拡張。
/// 実体は Loc.Instance のインデクサへのバインディングなので、
/// 言語を切り替えると再起動なしで表示が更新される。
/// </summary>
public sealed class TExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public TExtension() { }

    public TExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Loc.Instance,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
