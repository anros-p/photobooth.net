using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.Admin;
using Photobooth.App.ViewModels.Admin;

namespace Photobooth.App.ViewModels;

public sealed partial class AdminViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEventsSection))]
    [NotifyPropertyChangedFor(nameof(IsSessionsSection))]
    [NotifyPropertyChangedFor(nameof(IsHardwareSection))]
    [NotifyPropertyChangedFor(nameof(IsSettingsSection))]
    private AdminSection _currentSection = AdminSection.Events;

    public bool IsEventsSection   => CurrentSection == AdminSection.Events;
    public bool IsSessionsSection => CurrentSection == AdminSection.Sessions;
    public bool IsHardwareSection => CurrentSection == AdminSection.Hardware;
    public bool IsSettingsSection => CurrentSection == AdminSection.Settings;

    public EventListViewModel     EventList     { get; }
    public EventEditorViewModel   EventEditor   { get; }
    public SessionManagerViewModel Sessions     { get; }
    public HardwareStatusViewModel Hardware     { get; }
    public AppSettingsViewModel   AppSettings   { get; }

    public event EventHandler? SwitchToKioskRequested;

    public AdminViewModel(
        EventListViewModel eventList,
        EventEditorViewModel eventEditor,
        SessionManagerViewModel sessions,
        HardwareStatusViewModel hardware,
        AppSettingsViewModel appSettings)
    {
        EventList   = eventList;
        EventEditor = eventEditor;
        Sessions    = sessions;
        Hardware    = hardware;
        AppSettings = appSettings;

        EventList.EditRequested += (_, evt) =>
        {
            EventEditor.LoadEvent(evt);
            CurrentSection = AdminSection.Events;
        };

        EventList.ActiveEventChanged += async (_, evt) =>
        {
            await Sessions.LoadEventAsync(evt.Id).ConfigureAwait(false);
            CurrentSection = AdminSection.Sessions;
        };

        AppSettings.SwitchToKioskRequested += (_, _) =>
            SwitchToKioskRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand] private void NavigateEvents()   => CurrentSection = AdminSection.Events;
    [RelayCommand] private void NavigateSessions() => CurrentSection = AdminSection.Sessions;
    [RelayCommand] private void NavigateHardware() => CurrentSection = AdminSection.Hardware;
    [RelayCommand] private void NavigateSettings() => CurrentSection = AdminSection.Settings;

    public void Dispose() => Hardware.Dispose();
}
