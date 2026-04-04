using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Admin;
using Photobooth.App.ViewModels;

namespace Photobooth.App.ViewModels.Admin;

public sealed partial class AppSettingsViewModel : ViewModelBase
{
    private readonly AppSettingsStore _store;

    [ObservableProperty] private string _dataDirectory = string.Empty;
    [ObservableProperty] private string _cameraDriverOverride = "auto";
    [ObservableProperty] private string _smtpHost = string.Empty;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string _smtpUsername = string.Empty;
    [ObservableProperty] private string _smtpPassword = string.Empty;
    [ObservableProperty] private string _smtpFromAddress = string.Empty;
    [ObservableProperty] private string _smsGatewayUrl = string.Empty;
    [ObservableProperty] private string _smsAccountSid = string.Empty;
    [ObservableProperty] private string _smsAuthToken = string.Empty;
    [ObservableProperty] private string _smsFromNumber = string.Empty;
    [ObservableProperty] private string _hostingEndpoint = string.Empty;
    [ObservableProperty] private string _hostingApiKey = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public IReadOnlyList<string> CameraDriverOptions { get; } =
        ["auto", "canon", "nikon", "gphoto2", "simulated"];

    public event EventHandler? SwitchToKioskRequested;

    public AppSettingsViewModel(AppSettingsStore store) => _store = store;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var s = await _store.LoadAsync().ConfigureAwait(false);
        DataDirectory = s.DataDirectory;
        CameraDriverOverride = s.CameraDriverOverride;
        SmtpHost = s.SmtpHost;
        SmtpPort = s.SmtpPort;
        SmtpUsername = s.SmtpUsername;
        SmtpPassword = s.SmtpPassword;
        SmtpFromAddress = s.SmtpFromAddress;
        SmsGatewayUrl = s.SmsGatewayUrl;
        SmsAccountSid = s.SmsAccountSid;
        SmsAuthToken = s.SmsAuthToken;
        SmsFromNumber = s.SmsFromNumber;
        HostingEndpoint = s.HostingEndpoint;
        HostingApiKey = s.HostingApiKey;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = new AppSettings
        {
            DataDirectory = DataDirectory,
            CameraDriverOverride = CameraDriverOverride,
            SmtpHost = SmtpHost,
            SmtpPort = SmtpPort,
            SmtpUsername = SmtpUsername,
            SmtpPassword = SmtpPassword,
            SmtpFromAddress = SmtpFromAddress,
            SmsGatewayUrl = SmsGatewayUrl,
            SmsAccountSid = SmsAccountSid,
            SmsAuthToken = SmsAuthToken,
            SmsFromNumber = SmsFromNumber,
            HostingEndpoint = HostingEndpoint,
            HostingApiKey = HostingApiKey
        };

        await _store.SaveAsync(settings).ConfigureAwait(false);
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private void SwitchToKiosk() => SwitchToKioskRequested?.Invoke(this, EventArgs.Empty);
}
