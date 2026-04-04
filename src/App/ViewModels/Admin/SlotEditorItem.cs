using CommunityToolkit.Mvvm.ComponentModel;
using Photobooth.Drivers.Models;

namespace Photobooth.App.ViewModels.Admin;

/// <summary>Mutable wrapper around a <see cref="PhotoSlot"/> used by the layout editor.</summary>
public sealed partial class SlotEditorItem : ObservableObject
{
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
    [ObservableProperty] private double _rotation;
    [ObservableProperty] private bool _isSelected;

    public SlotEditorItem() { }

    public SlotEditorItem(PhotoSlot slot)
    {
        _x = slot.X;
        _y = slot.Y;
        _width = slot.Width;
        _height = slot.Height;
        _rotation = slot.Rotation;
    }

    public PhotoSlot ToPhotoSlot() => new()
    {
        X = (int)X,
        Y = (int)Y,
        Width = (int)Width,
        Height = (int)Height,
        Rotation = Rotation
    };
}
