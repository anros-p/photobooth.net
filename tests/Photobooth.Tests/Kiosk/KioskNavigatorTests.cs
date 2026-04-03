using Photobooth.App.Kiosk;

namespace Photobooth.Tests.Kiosk;

public sealed class KioskNavigatorTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var nav = new KioskNavigator();
        Assert.Equal(KioskState.Idle, nav.CurrentState);
    }

    [Fact]
    public void NavigateTo_ChangesState()
    {
        var nav = new KioskNavigator();
        nav.NavigateTo(KioskState.Preview);
        Assert.Equal(KioskState.Preview, nav.CurrentState);
    }

    [Fact]
    public void NavigateTo_SameState_DoesNotRaiseEvent()
    {
        var nav = new KioskNavigator();
        var raised = 0;
        nav.StateChanged += (_, _) => raised++;

        nav.NavigateTo(KioskState.Idle); // same as initial
        Assert.Equal(0, raised);
    }

    [Fact]
    public void NavigateTo_RaisesStateChanged()
    {
        var nav = new KioskNavigator();
        KioskState? received = null;
        nav.StateChanged += (_, s) => received = s;

        nav.NavigateTo(KioskState.Preview);

        Assert.Equal(KioskState.Preview, received);
    }

    [Theory]
    [InlineData(KioskState.Idle,      KioskState.Preview)]
    [InlineData(KioskState.Preview,   KioskState.Countdown)]
    [InlineData(KioskState.Countdown, KioskState.Capturing)]
    [InlineData(KioskState.Capturing, KioskState.Review)]
    [InlineData(KioskState.Review,    KioskState.Sharing)]
    [InlineData(KioskState.Sharing,   KioskState.Idle)]
    public void Advance_TransitionsToExpectedState(KioskState from, KioskState expected)
    {
        var nav = new KioskNavigator();
        nav.NavigateTo(from);
        nav.Advance();
        Assert.Equal(expected, nav.CurrentState);
    }

    [Fact]
    public void Reset_ReturnsToIdle()
    {
        var nav = new KioskNavigator();
        nav.NavigateTo(KioskState.Review);
        nav.Reset();
        Assert.Equal(KioskState.Idle, nav.CurrentState);
    }
}
