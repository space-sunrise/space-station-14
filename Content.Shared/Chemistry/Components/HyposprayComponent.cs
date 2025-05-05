// The code responsible for DoAfter was taken from the rejected Wizden PR 30704. And the code for toxin filtration is from 29879.
using Content.Shared.DoAfter; // Sunrise-Edit
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HyposprayComponent : Component
{
    [DataField]
    public string SolutionName = "hypospray";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    /// <summary>
    /// Decides whether you can inject everything or just mobs.
    /// When you can only affect mobs, you're capable of drawing from beakers.
    /// </summary>
    [AutoNetworkedField]
    [DataField(required: true)]
    public bool OnlyAffectsMobs = false;

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