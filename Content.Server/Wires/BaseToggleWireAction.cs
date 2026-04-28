namespace Content.Server.Wires;

/// <summary>
///     Utility class meant to be implemented. This is to
///     toggle a value whenever a wire is cut, mended,
///     or pulsed.
/// </summary>
public abstract partial class BaseToggleWireAction : BaseWireAction
{
    /// <summary>
    ///     Toggles the value on the given entity. An implementor
    ///     is expected to handle the value toggle appropriately.
    /// </summary>
    public abstract void ToggleValue(EntityUid owner, bool setting);
    /// <summary>
    ///     Gets the value on the given entity. An implementor
    ///     is expected to handle the value getter properly.
    /// </summary>
    public abstract bool GetValue(EntityUid owner);
    /// <summary>
    ///     Timeout key for the wire, if it is pulsed.
    ///     If this is null, there will be no value revert
    ///     after a given delay, otherwise, the value will
    ///     be set to the opposite of what it currently is
    ///     (according to GetValue)
    /// </summary>
    public virtual object? TimeoutKey { get; } = null;
    public virtual int Delay { get; } = 30;

    public override bool Cut(EntityUid user, Wire wire)
    {
        base.Cut(user, wire);
        ToggleValue(wire, false);

        if (TimeoutKey != null)
        {
            WiresSystem.TryCancelWireAction(wire, TimeoutKey);
        }

        return true;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        base.Mend(user, wire);
        ToggleValue(wire, true);

        return true;
    }

    public override void Pulse(EntityUid user, Wire wire)
    {
        base.Pulse(user, wire);
        ToggleValue(wire, !GetValue(wire));

        if (TimeoutKey != null)
        {
            WiresSystem.StartWireAction(wire, Delay, TimeoutKey, new TimedWireEvent(AwaitPulseCancel, wire));
        }
    }

    public override void Update(Wire wire)
    {
        if (TimeoutKey != null && !IsPowered(wire))
        {
            WiresSystem.TryCancelWireAction(wire, TimeoutKey);
        }
    }

    private void AwaitPulseCancel(Wire wire)
    {
        if (!wire.IsCut)
        {
            ToggleValue(wire, !GetValue(wire));
        }
    }
}
