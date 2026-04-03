using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;

namespace Photobooth.App.ViewModels.Kiosk;

public sealed partial class IdleViewModel : ViewModelBase
{
    private readonly KioskNavigator _navigator;
    private readonly IStringLocalizer _loc;

    public IdleViewModel(KioskNavigator navigator, IStringLocalizer loc)
    {
        _navigator = navigator;
        _loc = loc;
    }

    public string TapPrompt => _loc["Idle.Tap"];

    [RelayCommand]
    private void Start() => _navigator.NavigateTo(KioskState.Preview);
}
