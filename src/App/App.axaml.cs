using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;
using Photobooth.App.Views;
using Photobooth.Drivers.Camera.Simulated;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Services;
using Photobooth.Imaging.Composition;
using Photobooth.Plugins;

namespace Photobooth.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Photobooth");

            var locService = new LocalisationService();
            var navigator = new KioskNavigator();
            var cameraService = new CameraService([new SimulatedCameraDiscovery()]);
            var compositor = new ImageCompositor();
            var pluginHost = new PluginHost([]);

            // Start with a default event; in a real scenario this would be loaded from the store
            var activeEvent = new Event
            {
                Name = "Demo Event",
                CountdownSeconds = 3,
                PrintingEnabled = false,
                EnabledShareChannels = ShareChannel.None,
                AvailableLanguages = ["en", "fr"]
            };

            var vm = new KioskViewModel(
                navigator,
                cameraService,
                compositor,
                locService,
                pluginHost,
                printQueue: null,
                activeEvent,
                sessionDirectory: Path.Combine(dataDir, "sessions"));

            // Connect first available camera on startup (fire-and-forget)
            _ = ConnectCameraAsync(cameraService);

            desktop.MainWindow = new KioskWindow { DataContext = vm };
            desktop.Exit += (_, _) => vm.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task ConnectCameraAsync(CameraService cameraService)
    {
        try
        {
            var cameras = await cameraService.DetectCamerasAsync().ConfigureAwait(false);
            if (cameras.Count > 0)
                await cameraService.ConnectAsync(cameras[0]).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Camera connect failed: {ex.Message}");
        }
    }
}
