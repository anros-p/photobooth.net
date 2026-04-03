using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;
using Photobooth.Printing.Print;

namespace Photobooth.App.ViewModels.Kiosk;

public sealed partial class ReviewViewModel : ViewModelBase, IDisposable
{
    private readonly KioskNavigator _navigator;
    private readonly PrintQueue? _printQueue;
    private readonly IStringLocalizer _loc;

    [ObservableProperty]
    private Bitmap? _composedImage;

    [ObservableProperty]
    private bool _isPrintingEnabled;

    [ObservableProperty]
    private bool _isSharingEnabled;

    [ObservableProperty]
    private bool _isPrinting;

    public string ComposedImagePath { get; private set; } = string.Empty;

    public ReviewViewModel(KioskNavigator navigator, PrintQueue? printQueue, IStringLocalizer loc)
    {
        _navigator = navigator;
        _printQueue = printQueue;
        _loc = loc;
    }

    public string ReviewTitle => _loc["Review.Title"];
    public string PrintLabel => _loc["Review.Print"];
    public string ShareLabel => _loc["Review.Share"];
    public string RetakeLabel => _loc["Review.Retake"];
    public string DoneLabel => _loc["Review.Done"];

    public void Load(string composedImagePath, Event activeEvent)
    {
        ComposedImagePath = composedImagePath;
        IsPrintingEnabled = activeEvent.PrintingEnabled && _printQueue is not null;
        IsSharingEnabled = activeEvent.EnabledShareChannels != ShareChannel.None;

        using var ms = new MemoryStream(File.ReadAllBytes(composedImagePath));
        ComposedImage = new Bitmap(ms);
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (_printQueue is null || string.IsNullOrEmpty(ComposedImagePath)) return;
        IsPrinting = true;
        try
        {
            var opts = new PrintOptions { Copies = 1 };
            await _printQueue.EnqueueAsync(Guid.NewGuid(), ComposedImagePath, opts, 0, 0)
                .ConfigureAwait(false);
        }
        finally { IsPrinting = false; }
    }

    [RelayCommand]
    private void Share() => _navigator.NavigateTo(KioskState.Sharing);

    [RelayCommand]
    private void Retake() => _navigator.NavigateTo(KioskState.Preview);

    [RelayCommand]
    private void Done() => _navigator.Reset();

    public void Dispose() => ComposedImage?.Dispose();
}
