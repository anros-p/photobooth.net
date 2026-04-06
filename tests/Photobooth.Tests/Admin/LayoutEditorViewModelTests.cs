using Photobooth.App.ViewModels.Admin;
using Photobooth.Drivers.Models;

namespace Photobooth.Tests.Admin;

public sealed class LayoutEditorViewModelTests
{
    [Fact]
    public void LoadTemplate_PopulatesSlots()
    {
        var vm = new LayoutEditorViewModel();
        var template = new LayoutTemplate
        {
            CanvasWidth = 1800, CanvasHeight = 1200,
            PhotoSlots = [new PhotoSlot { X = 10, Y = 20, Width = 300, Height = 200 }]
        };

        vm.LoadTemplate(template);

        Assert.Single(vm.Slots);
        Assert.Equal(10, vm.Slots[0].X);
        Assert.Equal(1800, vm.CanvasWidth);
    }

    [Fact]
    public void AddSlot_IncreasesSlotCount()
    {
        var vm = new LayoutEditorViewModel();
        vm.AddSlotCommand.Execute(null);
        Assert.Single(vm.Slots);
    }

    [Fact]
    public void RemoveSelected_RemovesSlot()
    {
        var vm = new LayoutEditorViewModel();
        vm.AddSlotCommand.Execute(null);
        vm.RemoveSelectedCommand.Execute(null);
        Assert.Empty(vm.Slots);
    }

    [Fact]
    public void SelectSlot_UpdatesIsSelected()
    {
        var vm = new LayoutEditorViewModel();
        vm.AddSlotCommand.Execute(null);
        vm.AddSlotCommand.Execute(null);

        var first  = vm.Slots[0];
        var second = vm.Slots[1];

        vm.SelectSlot(first);
        Assert.True(first.IsSelected);
        Assert.False(second.IsSelected);

        vm.SelectSlot(second);
        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
    }

    [Fact]
    public void MoveSelectedSlot_UpdatesXY()
    {
        var vm = new LayoutEditorViewModel();
        vm.AddSlotCommand.Execute(null);
        var slot = vm.SelectedSlot!;
        var originalX = slot.X;
        var originalY = slot.Y;

        vm.MoveSelectedSlot(15, 25);

        Assert.Equal(originalX + 15, slot.X);
        Assert.Equal(originalY + 25, slot.Y);
    }

    [Fact]
    public void BuildTemplate_RoundTrips()
    {
        var vm = new LayoutEditorViewModel();
        vm.AddSlotCommand.Execute(null);

        var built = vm.BuildTemplate();

        Assert.Single(built.PhotoSlots);
    }
}
