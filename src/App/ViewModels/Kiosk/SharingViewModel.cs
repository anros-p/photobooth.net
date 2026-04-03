using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Kiosk;
using Photobooth.App.Localisation;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;
using Photobooth.Plugins;

namespace Photobooth.App.ViewModels.Kiosk;

public sealed partial class SharingViewModel : ViewModelBase
{
    private readonly KioskNavigator _navigator;
    private readonly PluginHost _pluginHost;
    private readonly IStringLocalizer _loc;

    [ObservableProperty]
    private string _emailAddress = string.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private bool _isEmailEnabled;

    [ObservableProperty]
    private bool _isSmsEnabled;

    [ObservableProperty]
    private bool _isQrEnabled;

    [ObservableProperty]
    private string? _qrCodePath;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    private string _composedImagePath = string.Empty;
    private Guid _sessionId;

    public string EmailPlaceholder => _loc["Sharing.EmailPlaceholder"];
    public string PhonePlaceholder => _loc["Sharing.PhonePlaceholder"];
    public string SendLabel => _loc["Sharing.Send"];
    public string QrInstruction => _loc["Sharing.QrInstruction"];

    public SharingViewModel(KioskNavigator navigator, PluginHost pluginHost, IStringLocalizer loc)
    {
        _navigator = navigator;
        _pluginHost = pluginHost;
        _loc = loc;
    }

    public void Load(string composedImagePath, Guid sessionId, Event activeEvent)
    {
        _composedImagePath = composedImagePath;
        _sessionId = sessionId;

        IsEmailEnabled = (activeEvent.EnabledShareChannels & ShareChannel.Email) != 0;
        IsSmsEnabled   = (activeEvent.EnabledShareChannels & ShareChannel.Sms)   != 0;
        IsQrEnabled    = (activeEvent.EnabledShareChannels & ShareChannel.QrCode) != 0;

        EmailAddress = string.Empty;
        PhoneNumber  = string.Empty;
        QrCodePath   = null;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SendEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(EmailAddress)) return;
        await ShareAsync(ShareChannel.Email, EmailAddress).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task SendSmsAsync()
    {
        if (string.IsNullOrWhiteSpace(PhoneNumber)) return;
        await ShareAsync(ShareChannel.Sms, PhoneNumber).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GenerateQrAsync() =>
        await ShareAsync(ShareChannel.QrCode, string.Empty).ConfigureAwait(false);

    [RelayCommand]
    private void Done() => _navigator.Reset();

    private async Task ShareAsync(ShareChannel channel, string recipient)
    {
        StatusMessage = _loc["Sharing.Sending"];

        var job = new ShareJob
        {
            SessionId = _sessionId,
            Channel = channel,
            FilePath = _composedImagePath,
            Recipient = recipient
        };

        var plugins = _pluginHost.GetSharePlugins(channel);
        if (plugins.Count == 0)
        {
            StatusMessage = _loc["Sharing.Error"];
            return;
        }

        try
        {
            var result = await plugins[0].ShareAsync(job).ConfigureAwait(false);

            if (channel == ShareChannel.QrCode && result.PublicUrl is not null)
                QrCodePath = result.PublicUrl;

            StatusMessage = _loc["Sharing.Sent"];
        }
        catch
        {
            StatusMessage = _loc["Sharing.Error"];
        }
    }
}
