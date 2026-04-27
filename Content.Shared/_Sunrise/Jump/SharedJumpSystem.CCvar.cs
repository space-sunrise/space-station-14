namespace Content.Shared._Sunrise.Jump;

public abstract partial class SharedJumpSystem
{
    private bool _enabled;

    private bool _bunnyHopEnabled;
    private TimeSpan _bunnyHopSpeedBoostWindow;
    private float _bunnyHopMinSpeedThreshold;

    private void OnClientOptionJumpSound(ClientOptionDisableJumpSoundEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Disable)
            _ignoredRecipients.Add(args.SenderSession);
        else
            _ignoredRecipients.Remove(args.SenderSession);
    }

    private void OnJumpEnableChanged(bool enable)
    {
        _enabled = enable;
    }

    private void OnBunnyHopEnableChanged(bool enable)
    {
        _bunnyHopEnabled = enable;
    }

    private void OnBunnyHopMinSpeedThresholdChanged(float value)
    {
        _bunnyHopMinSpeedThreshold = value;
    }

    private void OnBunnyHopSpeedBoostWindowChanged(float value)
    {
        _bunnyHopSpeedBoostWindow = TimeSpan.FromSeconds(value);
    }
}
