using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Store;

namespace Photobooth.App.ViewModels.Admin;

public sealed partial class EventEditorViewModel : ViewModelBase
{
    private readonly IEventStore _store;
    private Guid _eventId = Guid.NewGuid();

    // General tab
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _defaultLanguage = "en";
    [ObservableProperty] private string _screensaverMediaPath = string.Empty;
    [ObservableProperty] private int _screensaverIdleSeconds = 60;

    // Capture tab
    [ObservableProperty] private CaptureMode _captureMode = CaptureMode.Still;
    [ObservableProperty] private int _countdownSeconds = 3;
    [ObservableProperty] private int _gifFrameCount = 10;
    [ObservableProperty] private int _gifFrameRate = 10;

    // Printing tab
    [ObservableProperty] private bool _printingEnabled;
    [ObservableProperty] private int _maxPrints;

    // Sharing tab
    [ObservableProperty] private bool _shareEmail;
    [ObservableProperty] private bool _shareSms;
    [ObservableProperty] private bool _shareQr;
    [ObservableProperty] private bool _shareMicrosite;
    [ObservableProperty] private bool _galleryAccessEnabled;
    [ObservableProperty] private string _galleryAccessCode = string.Empty;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public LayoutEditorViewModel LayoutEditor { get; } = new();
    public ObservableCollection<string> AvailableLanguages { get; } = ["en"];
    public IReadOnlyList<CaptureMode> CaptureModes { get; } =
        Enum.GetValues<CaptureMode>().ToList();

    public event EventHandler? Saved;

    public EventEditorViewModel(IEventStore store) => _store = store;

    public void LoadEvent(Event evt)
    {
        _eventId = evt.Id;
        Name = evt.Name;
        DefaultLanguage = evt.DefaultLanguage;
        ScreensaverMediaPath = evt.ScreensaverMediaPath;
        ScreensaverIdleSeconds = evt.ScreensaverIdleSeconds;
        CaptureMode = evt.CaptureMode;
        CountdownSeconds = evt.CountdownSeconds;
        GifFrameCount = evt.GifFrameCount;
        GifFrameRate = evt.GifFrameRate;
        PrintingEnabled = evt.PrintingEnabled;
        MaxPrints = evt.MaxPrints;
        ShareEmail = (evt.EnabledShareChannels & ShareChannel.Email) != 0;
        ShareSms = (evt.EnabledShareChannels & ShareChannel.Sms) != 0;
        ShareQr = (evt.EnabledShareChannels & ShareChannel.QrCode) != 0;
        ShareMicrosite = (evt.EnabledShareChannels & ShareChannel.Microsite) != 0;
        GalleryAccessEnabled = evt.GalleryAccessEnabled;
        GalleryAccessCode = evt.GalleryAccessCode;

        AvailableLanguages.Clear();
        foreach (var lang in evt.AvailableLanguages) AvailableLanguages.Add(lang);

        if (evt.Layout is not null)
            LayoutEditor.LoadTemplate(evt.Layout);
    }

    public Event BuildEvent() => new()
    {
        Id = _eventId,
        Name = Name,
        DefaultLanguage = DefaultLanguage,
        ScreensaverMediaPath = ScreensaverMediaPath,
        ScreensaverIdleSeconds = ScreensaverIdleSeconds,
        CaptureMode = CaptureMode,
        CountdownSeconds = CountdownSeconds,
        GifFrameCount = GifFrameCount,
        GifFrameRate = GifFrameRate,
        PrintingEnabled = PrintingEnabled,
        MaxPrints = MaxPrints,
        EnabledShareChannels = BuildShareChannels(),
        GalleryAccessEnabled = GalleryAccessEnabled,
        GalleryAccessCode = GalleryAccessCode,
        AvailableLanguages = [.. AvailableLanguages],
        Layout = LayoutEditor.Slots.Count > 0 ? LayoutEditor.BuildTemplate() : null
    };

    [RelayCommand]
    private async Task SaveAsync()
    {
        var evt = BuildEvent();
        await _store.SaveAsync(evt).ConfigureAwait(false);
        StatusMessage = "Saved.";
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private ShareChannel BuildShareChannels()
    {
        var ch = ShareChannel.None;
        if (ShareEmail) ch |= ShareChannel.Email;
        if (ShareSms) ch |= ShareChannel.Sms;
        if (ShareQr) ch |= ShareChannel.QrCode;
        if (ShareMicrosite) ch |= ShareChannel.Microsite;
        return ch;
    }
}
