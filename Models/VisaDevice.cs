using CommunityToolkit.Mvvm.ComponentModel;
using Ivi.Visa;

namespace DSO
{
    public partial class VisaDevice : ObservableObject
    {
        [ObservableProperty]
        public string? address;
        [ObservableProperty]
        public string? name;
        [ObservableProperty]
        public HardwareInterfaceType? hwType;

    }
}
