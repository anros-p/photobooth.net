using CommunityToolkit.Mvvm.ComponentModel;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Services;
using Photobooth.Imaging.Composition;

namespace Photobooth.App.ViewModels.Kiosk;

public sealed partial class CapturingViewModel : ViewModelBase
{
    private readonly KioskNavigator _navigator;
    private readonly CameraService _cameraService;
    private readonly ImageCompositor _compositor;
    private readonly IStringLocalizer _loc;

    [ObservableProperty]
    private int _currentSlot;

    [ObservableProperty]
    private int _totalSlots;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public CapturingViewModel(
        KioskNavigator navigator,
        CameraService cameraService,
        ImageCompositor compositor,
        IStringLocalizer loc)
    {
        _navigator = navigator;
        _cameraService = cameraService;
        _compositor = compositor;
        _loc = loc;
    }

    /// <summary>
    /// Executes the full multi-capture flow for <paramref name="activeEvent"/>,
    /// then navigates to <see cref="KioskState.Review"/>.
    /// Emits the composed image path via <paramref name="onComposed"/>.
    /// </summary>
    public async Task ExecuteAsync(
        Event activeEvent,
        string sessionDirectory,
        Action<string> onComposed,
        CancellationToken ct)
    {
        var camera = _cameraService.ActiveCamera
            ?? throw new InvalidOperationException("No camera connected.");

        var layout = activeEvent.Layout;
        TotalSlots = layout?.PhotoSlots.Count ?? 1;
        var capturePaths = new List<string>();

        for (int i = 0; i < TotalSlots; i++)
        {
            CurrentSlot = i + 1;
            StatusMessage = _loc["Capturing.Title"];

            var path = Path.Combine(sessionDirectory, $"capture_{i:D2}.jpg");
            var captured = await camera.CaptureAsync(path, ct).ConfigureAwait(false);
            capturePaths.Add(captured.FilePath);

            // Small pause between captures so the flash/preview isn't jarring
            if (i < TotalSlots - 1)
                await Task.Delay(500, ct).ConfigureAwait(false);
        }

        // Compose
        StatusMessage = "Composing…";
        string composedPath;

        if (layout is not null)
        {
            var request = new CompositionRequest
            {
                Layout = layout,
                CaptureFilePaths = capturePaths
            };
            var result = await Task.Run(
                () => _compositor.Compose(request, Path.Combine(sessionDirectory, "composed.jpg")),
                ct).ConfigureAwait(false);
            composedPath = result.OutputPath;
        }
        else
        {
            composedPath = capturePaths[0];
        }

        onComposed(composedPath);
        _navigator.NavigateTo(KioskState.Review);
    }
}
