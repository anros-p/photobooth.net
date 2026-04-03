using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Services;

namespace Photobooth.App.ViewModels.Kiosk;

public sealed partial class PreviewViewModel : ViewModelBase, IDisposable
{
    private readonly KioskNavigator _navigator;
    private readonly CameraService _cameraService;
    private readonly IStringLocalizer _loc;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private Bitmap? _currentFrame;

    public PreviewViewModel(KioskNavigator navigator, CameraService cameraService, IStringLocalizer loc)
    {
        _navigator = navigator;
        _cameraService = cameraService;
        _loc = loc;
    }

    public string TapToStartLabel => _loc["Preview.TapToStart"];

    public void StartLiveView()
    {
        _cts = new CancellationTokenSource();
        _ = RunLiveViewAsync(_cts.Token);
    }

    public void StopLiveView()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    [RelayCommand]
    private void Capture()
    {
        StopLiveView();
        _navigator.NavigateTo(KioskState.Countdown);
    }

    public void Dispose() => StopLiveView();

    private async Task RunLiveViewAsync(CancellationToken ct)
    {
        var camera = _cameraService.ActiveCamera;
        if (camera is null) return;

        try
        {
            await foreach (var frame in camera.GetLiveViewStreamAsync(ct).ConfigureAwait(false))
            {
                using var ms = new MemoryStream(frame.JpegData);
                // Update on UI thread
                CurrentFrame = new Bitmap(ms);
            }
        }
        catch (OperationCanceledException) { }
    }
}
