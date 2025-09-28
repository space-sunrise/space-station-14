using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shower.Components;

/// <summary>
/// Shower that can be toggled on and off with water animation, sound, and steam effects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowerComponent : Component
{
    /// <summary>
    /// Whether the shower is currently running.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsOn = false;

    /// <summary>
    /// Sound to play when shower is running.
    /// </summary>
    [DataField]
    public SoundSpecifier WaterSound = new SoundPathSpecifier("/Audio/Effects/Fluids/shower.ogg");

    /// <summary>
    /// Sound to play when toggling shower on/off.
    /// </summary>
    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");

    /// <summary>
    /// How much water to put in the puddle under the shower (in units).
    /// </summary>
    [DataField]
    public float PuddleWaterAmount = 15f;

    /// <summary>
    /// How often to clean reagents from entities standing on the shower tile (in seconds).
    /// </summary>
    [DataField]
    public float CleaningInterval = 1f;

    /// <summary>
    /// Time accumulator for cleaning interval.
    /// </summary>
    public float CleaningAccumulator = 0f;

    /// <summary>
    /// Time accumulator for steam spawning.
    /// </summary>
    public float SteamAccumulator = 0f;

    /// <summary>
    /// Interval between steam spawns (3 seconds).
    /// </summary>
    [DataField]
    public float SteamInterval = 3f;

    /// <summary>
    /// Entity playing the water sound (for stopping it when shower turns off).
    /// </summary>
    public EntityUid? PlayingSound;
}

[Serializable, NetSerializable]
public enum ShowerVisuals : byte
{
    IsOn,
    WaterAnimation,
    SteamEffect,
}

[Serializable, NetSerializable]
public enum ShowerState : byte
{
    Off,
    On,
}
