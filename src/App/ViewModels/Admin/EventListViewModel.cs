using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Store;

namespace Photobooth.App.ViewModels.Admin;

public sealed partial class EventListViewModel : ViewModelBase
{
    private readonly IEventStore _store;

    [ObservableProperty] private EventListItem? _selectedEvent;
    [ObservableProperty] private EventListItem? _activeEvent;

    public ObservableCollection<EventListItem> Events { get; } = [];

    /// <summary>Raised when the operator requests to edit an event.</summary>
    public event EventHandler<Event>? EditRequested;

    /// <summary>Raised when an event is set as active (to load into kiosk).</summary>
    public event EventHandler<Event>? ActiveEventChanged;

    public EventListViewModel(IEventStore store) => _store = store;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var all = await _store.GetAllAsync().ConfigureAwait(false);
        Events.Clear();
        foreach (var evt in all.OrderByDescending(e => e.CreatedAt))
            Events.Add(new EventListItem(evt));
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        var evt = new Event { Name = "New Event" };
        await _store.SaveAsync(evt).ConfigureAwait(false);
        var item = new EventListItem(evt);
        Events.Insert(0, item);
        SelectedEvent = item;
        EditRequested?.Invoke(this, evt);
    }

    [RelayCommand]
    private async Task DuplicateAsync(EventListItem? item)
    {
        if (item is null) return;
        var src = await _store.GetByIdAsync(item.Id).ConfigureAwait(false);
        if (src is null) return;

        var copy = src with { Id = Guid.NewGuid(), Name = src.Name + " (Copy)", CreatedAt = DateTimeOffset.UtcNow };
        await _store.SaveAsync(copy).ConfigureAwait(false);
        Events.Insert(0, new EventListItem(copy));
    }

    [RelayCommand]
    private async Task DeleteAsync(EventListItem? item)
    {
        if (item is null) return;
        await _store.DeleteAsync(item.Id).ConfigureAwait(false);
        Events.Remove(item);
        if (SelectedEvent == item) SelectedEvent = null;
        if (ActiveEvent == item) ActiveEvent = null;
    }

    [RelayCommand]
    private async Task SetActiveAsync(EventListItem? item)
    {
        if (item is null) return;
        var evt = await _store.GetByIdAsync(item.Id).ConfigureAwait(false);
        if (evt is null) return;
        ActiveEvent = item;
        ActiveEventChanged?.Invoke(this, evt);
    }

    [RelayCommand]
    private async Task EditAsync(EventListItem? item)
    {
        if (item is null) return;
        var evt = await _store.GetByIdAsync(item.Id).ConfigureAwait(false);
        if (evt is null) return;
        EditRequested?.Invoke(this, evt);
    }
}

public sealed class EventListItem(Event evt)
{
    public Guid Id { get; } = evt.Id;
    public string Name { get; } = evt.Name;
    public DateTimeOffset CreatedAt { get; } = evt.CreatedAt;
    public string CreatedAtDisplay { get; } = evt.CreatedAt.LocalDateTime.ToString("g");
    public CaptureMode CaptureMode { get; } = evt.CaptureMode;
}
