namespace Photobooth.App.Kiosk;

/// <summary>
/// Manages kiosk screen transitions.
/// Raises <see cref="StateChanged"/> on every navigation.
/// </summary>
public sealed class KioskNavigator
{
    public KioskState CurrentState { get; private set; } = KioskState.Idle;

    public event EventHandler<KioskState>? StateChanged;

    public void NavigateTo(KioskState state)
    {
        if (state == CurrentState) return;
        CurrentState = state;
        StateChanged?.Invoke(this, state);
    }

    /// <summary>Advances to the next logical state in the capture flow.</summary>
    public void Advance()
    {
        var next = CurrentState switch
        {
            KioskState.Idle      => KioskState.Preview,
            KioskState.Preview   => KioskState.Countdown,
            KioskState.Countdown => KioskState.Capturing,
            KioskState.Capturing => KioskState.Review,
            KioskState.Review    => KioskState.Sharing,
            KioskState.Sharing   => KioskState.Idle,
            _                    => KioskState.Idle
        };
        NavigateTo(next);
    }

    /// <summary>Resets to <see cref="KioskState.Idle"/>.</summary>
    public void Reset() => NavigateTo(KioskState.Idle);
}
