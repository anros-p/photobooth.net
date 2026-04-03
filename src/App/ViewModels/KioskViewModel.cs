using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels.Kiosk;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Services;
using Photobooth.Imaging.Composition;
using Photobooth.Plugins;
using Photobooth.Printing.Print;

namespace Photobooth.App.ViewModels;

/// <summary>
/// Root kiosk ViewModel. Owns the navigator and all screen ViewModels.
/// Binds the <see cref="CurrentScreen"/> to the kiosk window's content area.
/// </summary>
public sealed partial class KioskViewModel : ViewModelBase, IDisposable
{
    private readonly KioskNavigator _navigator;
    private readonly LocalisationService _locService;

    // Screen VMs
    private readonly IdleViewModel _idle;
    private readonly PreviewViewModel _preview;
    private readonly CountdownViewModel _countdown;
    private readonly CapturingViewModel _capturing;
    private readonly ReviewViewModel _review;
    private readonly SharingViewModel _sharing;
    private readonly LanguageSelectorViewModel _languageSelector;

    // Session state
    private Event? _activeEvent;
    private Guid _currentSessionId = Guid.NewGuid();
    private string _sessionDirectory = string.Empty;
    private string _composedImagePath = string.Empty;

    [ObservableProperty]
    private ViewModelBase _currentScreen;

    [ObservableProperty]
    private bool _isLanguageSelectorOpen;

    public KioskViewModel(
        KioskNavigator navigator,
        CameraService cameraService,
        ImageCompositor compositor,
        LocalisationService locService,
        PluginHost pluginHost,
        PrintQueue? printQueue,
        Event? activeEvent,
        string sessionDirectory)
    {
        _navigator = navigator;
        _locService = locService;
        _activeEvent = activeEvent;
        _sessionDirectory = sessionDirectory;

        _idle = new IdleViewModel(navigator, locService);
        _preview = new PreviewViewModel(navigator, cameraService, locService);
        _countdown = new CountdownViewModel(navigator, locService);
        _capturing = new CapturingViewModel(navigator, cameraService, compositor, locService);
        _review = new ReviewViewModel(navigator, printQueue, locService);
        _sharing = new SharingViewModel(navigator, pluginHost, locService);
        _languageSelector = new LanguageSelectorViewModel(locService);

        _languageSelector.CloseRequested += (_, _) => IsLanguageSelectorOpen = false;
        _locService.LanguageChanged += OnLanguageChanged;

        _navigator.StateChanged += OnStateChanged;
        _currentScreen = _idle;

        if (_activeEvent?.AvailableLanguages is { Count: > 0 } langs)
            _languageSelector.SetAvailableLanguages(langs);
    }

    [RelayCommand]
    private void ToggleLanguageSelector() =>
        IsLanguageSelectorOpen = !IsLanguageSelectorOpen;

    public LanguageSelectorViewModel LanguageSelector => _languageSelector;

    public void Dispose()
    {
        _navigator.StateChanged -= OnStateChanged;
        _locService.LanguageChanged -= OnLanguageChanged;
        _preview.Dispose();
        _review.Dispose();
    }

    private void OnStateChanged(object? sender, KioskState state)
    {
        IsLanguageSelectorOpen = false;

        CurrentScreen = state switch
        {
            KioskState.Idle      => _idle,
            KioskState.Preview   => StartPreview(),
            KioskState.Countdown => StartCountdown(),
            KioskState.Capturing => StartCapturing(),
            KioskState.Review    => LoadReview(),
            KioskState.Sharing   => LoadSharing(),
            _                    => _idle
        };
    }

    private ViewModelBase StartPreview()
    {
        _currentSessionId = Guid.NewGuid();
        _preview.StartLiveView();
        return _preview;
    }

    private ViewModelBase StartCountdown()
    {
        int seconds = _activeEvent?.CountdownSeconds ?? 3;
        int slots = _activeEvent?.Layout?.PhotoSlots.Count ?? 1;
        _ = _countdown.StartAsync(seconds, slot: 1, totalSlots: slots, CancellationToken.None);
        return _countdown;
    }

    private ViewModelBase StartCapturing()
    {
        if (_activeEvent is null) return _idle;

        var dir = Path.Combine(_sessionDirectory, _currentSessionId.ToString("N"));
        Directory.CreateDirectory(dir);

        _ = _capturing.ExecuteAsync(
            _activeEvent, dir,
            composedPath => _composedImagePath = composedPath,
            CancellationToken.None);

        return _capturing;
    }

    private ViewModelBase LoadReview()
    {
        if (!string.IsNullOrEmpty(_composedImagePath) && _activeEvent is not null)
            _review.Load(_composedImagePath, _activeEvent);
        return _review;
    }

    private ViewModelBase LoadSharing()
    {
        if (_activeEvent is not null)
            _sharing.Load(_composedImagePath, _currentSessionId, _activeEvent);
        return _sharing;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        // Notify all label properties that depend on the localiser
        _idle.RefreshAllProperties();
        _preview.RefreshAllProperties();
        _countdown.RefreshAllProperties();
        _capturing.RefreshAllProperties();
        _review.RefreshAllProperties();
        _sharing.RefreshAllProperties();
        _languageSelector.RefreshAllProperties();
    }
}
