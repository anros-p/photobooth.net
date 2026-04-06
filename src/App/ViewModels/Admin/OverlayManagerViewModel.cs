using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Store;

namespace Photobooth.App.ViewModels.Admin;

public sealed partial class OverlayManagerViewModel : ViewModelBase
{
    private readonly IEventStore _store;
    private readonly string _assetsDirectory;
    private Event? _activeEvent;

    [ObservableProperty] private OverlayAssetItem? _selectedAsset;
    [ObservableProperty] private string _newCategory = string.Empty;

    public ObservableCollection<OverlayAssetItem> Assets { get; } = [];

    public OverlayManagerViewModel(IEventStore store, string assetsDirectory)
    {
        _store = store;
        _assetsDirectory = assetsDirectory;
        Directory.CreateDirectory(assetsDirectory);
    }

    public void LoadEvent(Event evt)
    {
        _activeEvent = evt;
        Assets.Clear();
        foreach (var asset in evt.OverlayAssets)
            Assets.Add(new OverlayAssetItem(asset));
    }

    [RelayCommand]
    private async Task ImportAsync(string filePath)
    {
        if (_activeEvent is null || !File.Exists(filePath)) return;

        var dest = Path.Combine(_assetsDirectory, Path.GetFileName(filePath));
        File.Copy(filePath, dest, overwrite: true);

        var asset = new OverlayAsset
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            FilePath = dest,
            Category = string.IsNullOrWhiteSpace(NewCategory) ? "General" : NewCategory
        };

        var updatedAssets = _activeEvent.OverlayAssets.Append(asset).ToList();
        _activeEvent = _activeEvent with { OverlayAssets = updatedAssets };
        await _store.SaveAsync(_activeEvent).ConfigureAwait(false);
        Assets.Add(new OverlayAssetItem(asset));
    }

    [RelayCommand]
    private async Task DeleteAsync(OverlayAssetItem? item)
    {
        if (_activeEvent is null || item is null) return;

        var updatedAssets = _activeEvent.OverlayAssets
            .Where(a => a.FilePath != item.FilePath)
            .ToList();
        _activeEvent = _activeEvent with { OverlayAssets = updatedAssets };
        await _store.SaveAsync(_activeEvent).ConfigureAwait(false);
        Assets.Remove(item);
    }
}

public sealed class OverlayAssetItem(OverlayAsset asset)
{
    public string Name { get; } = asset.Name;
    public string FilePath { get; } = asset.FilePath;
    public string Category { get; } = asset.Category;
}
