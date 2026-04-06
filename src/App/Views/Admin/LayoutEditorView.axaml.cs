using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Photobooth.App.ViewModels.Admin;

namespace Photobooth.App.Views.Admin;

/// <summary>
/// Code-behind for the layout editor canvas.
/// Handles pointer events for drag-to-reposition and click-to-select photo slots.
/// </summary>
public partial class LayoutEditorView : UserControl
{
    private Point _dragStart;
    private SlotEditorItem? _dragging;

    public LayoutEditorView()
    {
        AvaloniaXamlLoader.Load(this);

        var canvas = this.FindControl<Canvas>("EditorCanvas");
        if (canvas is null) return;

        canvas.PointerPressed  += OnPointerPressed;
        canvas.PointerMoved    += OnPointerMoved;
        canvas.PointerReleased += OnPointerReleased;
    }

    private LayoutEditorViewModel? Vm => DataContext as LayoutEditorViewModel;

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var vm = Vm;
        if (vm is null) return;

        var pos = e.GetPosition(sender as Visual);
        _dragStart = pos;

        // Hit-test: find topmost slot under pointer
        var hit = vm.Slots.LastOrDefault(s =>
            pos.X >= s.X && pos.X <= s.X + s.Width &&
            pos.Y >= s.Y && pos.Y <= s.Y + s.Height);

        vm.SelectSlot(hit);
        _dragging = hit;
        e.Pointer.Capture(sender as IInputElement);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging is null) return;
        var pos = e.GetPosition(sender as Visual);
        var dx = pos.X - _dragStart.X;
        var dy = pos.Y - _dragStart.Y;
        Vm?.MoveSelectedSlot(dx, dy);
        _dragStart = pos;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = null;
        e.Pointer.Capture(null);
    }
}
