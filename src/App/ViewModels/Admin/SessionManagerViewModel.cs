using System.Collections.ObjectModel;
using System.IO.Compression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Store;
using Photobooth.Printing.Print;

namespace Photobooth.App.ViewModels.Admin;

public sealed partial class SessionManagerViewModel : ViewModelBase
{
    private readonly ISessionStore _sessionStore;
    private readonly PrintQueue? _printQueue;
    private Guid _activeEventId;

    [ObservableProperty] private SessionItem? _selectedSession;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<SessionItem> Sessions { get; } = [];

    public SessionManagerViewModel(ISessionStore sessionStore, PrintQueue? printQueue = null)
    {
        _sessionStore = sessionStore;
        _printQueue = printQueue;
    }

    public async Task LoadEventAsync(Guid eventId, CancellationToken ct = default)
    {
        _activeEventId = eventId;
        var all = await _sessionStore.GetByEventAsync(eventId, ct).ConfigureAwait(false);
        Sessions.Clear();
        foreach (var s in all.OrderByDescending(s => s.StartedAt))
            Sessions.Add(new SessionItem(s));
    }

    [RelayCommand]
    private async Task ReprintAsync(SessionItem? item)
    {
        if (item is null || _printQueue is null) return;
        if (!File.Exists(item.ComposedFilePath))
        {
            StatusMessage = "Image file not found.";
            return;
        }

        var opts = new PrintOptions { Copies = 1 };
        await _printQueue.EnqueueAsync(Guid.NewGuid(), item.ComposedFilePath, opts, 0, 0)
            .ConfigureAwait(false);
        StatusMessage = $"Reprinted session {item.Id:D}.";
    }

    [RelayCommand]
    private async Task DeleteAsync(SessionItem? item)
    {
        if (item is null) return;
        await _sessionStore.DeleteAsync(item.Id).ConfigureAwait(false);
        Sessions.Remove(item);
        if (SelectedSession == item) SelectedSession = null;
    }

    [RelayCommand]
    private async Task ExportZipAsync(string outputPath)
    {
        StatusMessage = "Exporting…";
        await Task.Run(() =>
        {
            using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);
            foreach (var session in Sessions)
            {
                if (File.Exists(session.ComposedFilePath))
                    zip.CreateEntryFromFile(session.ComposedFilePath,
                        $"{session.Id}/{Path.GetFileName(session.ComposedFilePath)}");
            }
        }).ConfigureAwait(false);
        StatusMessage = $"Exported {Sessions.Count} sessions to {Path.GetFileName(outputPath)}.";
    }
}

public sealed class SessionItem(Session session)
{
    public Guid Id { get; } = session.Id;
    public string ComposedFilePath { get; } = session.ComposedFilePath;
    public string StartedAtDisplay { get; } = session.StartedAt.LocalDateTime.ToString("g");
    public int CaptureCount { get; } = session.Captures.Count;
    public bool HasComposedImage { get; } = !string.IsNullOrEmpty(session.ComposedFilePath)
                                            && File.Exists(session.ComposedFilePath);
}
