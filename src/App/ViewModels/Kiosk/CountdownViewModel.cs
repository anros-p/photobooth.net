using CommunityToolkit.Mvvm.ComponentModel;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;

namespace Photobooth.App.ViewModels.Kiosk;

public sealed partial class CountdownViewModel : ViewModelBase
{
    private readonly KioskNavigator _navigator;
    private readonly IStringLocalizer _loc;

    [ObservableProperty]
    private int _secondsRemaining;

    [ObservableProperty]
    private int _currentSlot = 1;

    [ObservableProperty]
    private int _totalSlots = 1;

    public CountdownViewModel(KioskNavigator navigator, IStringLocalizer loc)
    {
        _navigator = navigator;
        _loc = loc;
    }

    public string ProgressLabel =>
        TotalSlots > 1
            ? _loc["Capturing.Progress"]
                .Replace("{current}", CurrentSlot.ToString())
                .Replace("{total}", TotalSlots.ToString())
            : string.Empty;

    public async Task StartAsync(int seconds, int slot, int totalSlots, CancellationToken ct)
    {
        CurrentSlot = slot;
        TotalSlots = totalSlots;

        for (int i = seconds; i >= 1; i--)
        {
            SecondsRemaining = i;
            OnPropertyChanged(nameof(ProgressLabel));
            await Task.Delay(1000, ct).ConfigureAwait(false);
        }

        _navigator.NavigateTo(KioskState.Capturing);
    }
}
