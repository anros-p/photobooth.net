using Photobooth.App.ViewModels.Admin;
using Photobooth.Drivers.Models;
using Photobooth.Drivers.Store;

namespace Photobooth.Tests.Admin;

public sealed class EventListViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ev_{Guid.NewGuid():N}");
    private readonly JsonEventStore _store;

    public EventListViewModelTests()
    {
        Directory.CreateDirectory(_dir);
        _store = new JsonEventStore(_dir);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private EventListViewModel MakeVm() => new(_store);

    [Fact]
    public async Task Refresh_LoadsPersistedEvents()
    {
        await _store.SaveAsync(new Event { Name = "Alpha" });
        await _store.SaveAsync(new Event { Name = "Beta" });

        var vm = MakeVm();
        await vm.RefreshAsync();

        Assert.Equal(2, vm.Events.Count);
    }

    [Fact]
    public async Task Create_AddsEventToStore()
    {
        var vm = MakeVm();
        await vm.CreateCommand.ExecuteAsync(null);

        var all = await _store.GetAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task Duplicate_CreatesNewRecord()
    {
        var vm = MakeVm();
        await vm.CreateCommand.ExecuteAsync(null);
        await vm.RefreshAsync();

        var item = vm.Events[0];
        await vm.DuplicateCommand.ExecuteAsync(item);

        var all = await _store.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Delete_RemovesFromStoreAndList()
    {
        var vm = MakeVm();
        await vm.CreateCommand.ExecuteAsync(null);
        await vm.RefreshAsync();

        var item = vm.Events[0];
        await vm.DeleteCommand.ExecuteAsync(item);

        Assert.Empty(vm.Events);
        Assert.Empty(await _store.GetAllAsync());
    }

    [Fact]
    public async Task SetActive_RaisesActiveEventChanged()
    {
        await _store.SaveAsync(new Event { Name = "Demo" });

        var vm = MakeVm();
        await vm.RefreshAsync();

        Event? activated = null;
        vm.ActiveEventChanged += (_, e) => activated = e;

        await vm.SetActiveCommand.ExecuteAsync(vm.Events[0]);

        Assert.NotNull(activated);
        Assert.Equal("Demo", activated!.Name);
    }
}
