namespace Content.Server._Sunrise.AssaultOps.Icarus;

[RegisterComponent]
public sealed partial class IcarusBeamComponent : Component
{
    /// <summary>
    ///     Beam moving speed.
    /// </summary>
    [DataField]
    public float Speed = 25f;

    /// <summary>
    ///     The beam will be automatically cleaned up after this time.
    /// </summary>
    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(100);

    /// <summary>
    ///     With this set to true, beam will automatically set the tiles under them to space.
    /// </summary>
    [DataField]
    public bool DestroyTiles = false;

    [DataField]
    public float DestroyRadius = 5f;

    [DataField]
    public float FlameRadius = 16f;

    public TimeSpan LifetimeEnd;
}
