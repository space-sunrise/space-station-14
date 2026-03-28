namespace Content.Server._Sunrise.AssaultOps.Icarus;

[RegisterComponent]
public sealed partial class IcarusBeamComponent : Component
{
    /// <summary>
    ///     Beam moving speed.
    /// </summary>
    [DataField]
    public float Speed = 8f;

    /// <summary>
    ///     The beam will be automatically cleaned up after this time.
    /// </summary>
    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(200);

    /// <summary>
    ///     With this set to true, beam will automatically set the tiles under them to space.
    /// </summary>
    [DataField]
    public bool DestroyTiles = true;

    [DataField]
    public float DestroyRadius = 4f;

    [DataField]
    public float FlameRadius = 8f;

    public TimeSpan LifetimeEnd;
}
