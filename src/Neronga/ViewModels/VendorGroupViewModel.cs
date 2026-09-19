using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Neronga.ViewModels;

public partial class VendorGroupViewModel : ObservableObject
{
    public string Vendor { get; }

    public ObservableCollection<ScannedItemViewModel> Items { get; } = new();

    public int Count => Items.Count;

    [ObservableProperty]
    private bool isExpanded = true;

    public VendorGroupViewModel(string vendor)
    {
        Vendor = vendor;
    }

    public void NotifyCountChanged() => OnPropertyChanged(nameof(Count));
}
