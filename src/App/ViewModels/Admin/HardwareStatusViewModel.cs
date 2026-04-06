using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Services;
using Photobooth.Printing.Print;

namespace Photobooth.App.ViewModels.Admin;

public sealed partial class HardwareStatusViewModel : ViewModelBase, IDisposable
{
    private readonly CameraService _cameraService;
    private readonly IPrintService? _printService;
    private readonly string _dataDirectory;
    private readonly Timer _autoRefresh;

    [ObservableProperty] private string _cameraStatus = "Unknown";
    [ObservableProperty] private string _cameraModel = "—";
    [ObservableProperty] private string _printerStatus = "—";
    [ObservableProperty] private string _diskFree = "—";
    [ObservableProperty] private bool _isRefreshing;

    public HardwareStatusViewModel(
        CameraService cameraService,
        IPrintService? printService,
        string dataDirectory)
    {
        _cameraService = cameraService;
        _printService = printService;
        _dataDirectory = dataDirectory;

        _autoRefresh = new Timer(_ => _ = RefreshAsync(), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            RefreshCamera();
            await RefreshPrinterAsync().ConfigureAwait(false);
            RefreshDisk();
        }
        finally { IsRefreshing = false; }
    }

    public void Dispose() => _autoRefresh.Dispose();

    private void RefreshCamera()
    {
        var cam = _cameraService.ActiveCamera;
        if (cam is null)
        {
            CameraStatus = "Not connected";
            CameraModel = "—";
            return;
        }
        CameraStatus = cam.IsConnected ? "Connected" : "Disconnected";
        CameraModel = cam.Info.Model;
    }

    private async Task RefreshPrinterAsync()
    {
        if (_printService is null)
        {
            PrinterStatus = "No print service";
            return;
        }

        try
        {
            var printers = await _printService.GetAvailablePrintersAsync().ConfigureAwait(false);
            var defaultPrinter = printers.FirstOrDefault(p => p.IsDefault) ?? printers.FirstOrDefault();
            PrinterStatus = defaultPrinter is null
                ? "No printers found"
                : $"{defaultPrinter.Name} — {(defaultPrinter.IsOnline ? "Ready" : "Offline")}";
        }
        catch (Exception ex)
        {
            PrinterStatus = $"Error: {ex.Message}";
        }
    }

    private void RefreshDisk()
    {
        try
        {
            Directory.CreateDirectory(_dataDirectory);
            var drive = new DriveInfo(Path.GetPathRoot(_dataDirectory)!);
            var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            DiskFree = $"{freeGb:F1} GB free";
        }
        catch
        {
            DiskFree = "Unknown";
        }
    }
}
