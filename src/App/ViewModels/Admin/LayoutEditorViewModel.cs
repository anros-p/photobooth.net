using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Photobooth.App.ViewModels;
using Photobooth.Drivers.Models;

namespace Photobooth.App.ViewModels.Admin;

public sealed partial class LayoutEditorViewModel : ViewModelBase
{
    [ObservableProperty] private string _backgroundImagePath = string.Empty;
    [ObservableProperty] private double _canvasWidth = 1800;
    [ObservableProperty] private double _canvasHeight = 1200;
    [ObservableProperty] private SlotEditorItem? _selectedSlot;

    public ObservableCollection<SlotEditorItem> Slots { get; } = [];

    public void LoadTemplate(LayoutTemplate template)
    {
        BackgroundImagePath = template.BackgroundImagePath;
        CanvasWidth = template.CanvasWidth;
        CanvasHeight = template.CanvasHeight;

        Slots.Clear();
        foreach (var slot in template.PhotoSlots)
            Slots.Add(new SlotEditorItem(slot));
    }

    public LayoutTemplate BuildTemplate() => new()
    {
        BackgroundImagePath = BackgroundImagePath,
        CanvasWidth = (int)CanvasWidth,
        CanvasHeight = (int)CanvasHeight,
        PhotoSlots = Slots.Select(s => s.ToPhotoSlot()).ToList()
    };

    [RelayCommand]
    private void AddSlot()
    {
        var slot = new SlotEditorItem
        {
            X = 50,
            Y = 50,
            Width = 400,
            Height = 300
        };
        Slots.Add(slot);
        SelectSlot(slot);
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedSlot is null) return;
        Slots.Remove(SelectedSlot);
        SelectedSlot = null;
    }

    public void SelectSlot(SlotEditorItem? slot)
    {
        if (SelectedSlot is not null) SelectedSlot.IsSelected = false;
        SelectedSlot = slot;
        if (SelectedSlot is not null) SelectedSlot.IsSelected = true;
    }

    public void MoveSelectedSlot(double dx, double dy)
    {
        if (SelectedSlot is null) return;
        SelectedSlot.X = Math.Max(0, SelectedSlot.X + dx);
        SelectedSlot.Y = Math.Max(0, SelectedSlot.Y + dy);
    }

    [RelayCommand]
    private void SetBackground(string path)
    {
        if (File.Exists(path))
            BackgroundImagePath = path;
    }
}
