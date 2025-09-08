// The code responsible for DoAfter was taken from the rejected Wizden PR 30704. And the code for toxin filtration is from 29879.
using Content.Shared.DoAfter; // Sunrise-Edit
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Component that allows an entity instantly transfer liquids by interacting with objects that have solutions.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class HyposprayComponent : Component
{
    /// <summary>
    ///     Solution that will be used by hypospray for injections.
    /// </summary>
    [DataField]
    public string SolutionName = "hypospray";

    /// <summary>
    ///     Amount of the units that will be transfered.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    /// <summary>
    ///     Sound that will be played when injecting.
    /// </summary>
    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// Decides whether you can inject everything or just mobs.
    /// </summary>
    [AutoNetworkedField]
    [DataField(required: true)]
    public bool OnlyAffectsMobs = false;

    /// <summary>
    /// If this can draw from containers in mob-only mode.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public bool CanContainerDraw = true;

    /// <summary>
    /// Whether or not the hypospray is able to draw from containers or if it's a single use
    /// device that can only inject.
    /// </summary>
    [DataField]
    public bool InjectOnly = false;

    // Sunrise-Start

    /// <summary>
    /// Whether or not this hypospray will destroy poisons when drawing from a container.
    /// </summary>
    [DataField]
    public bool FilterPoison = false;

    /// <summary>
    ///  If set over 0, enables a doafter for the hypospray which must be completed for injection.
    /// </summary>
    [DataField]
    public float DoAfterTime = 0f;
}

[Serializable, NetSerializable]
public sealed partial class HyposprayDoAfterEvent : SimpleDoAfterEvent
{
    // Sunrise-End
}
