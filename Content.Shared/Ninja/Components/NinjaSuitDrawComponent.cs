using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for ninja equipment that draws power from the ninja suit's battery instead of its own.
/// Similar to PowerCellDrawComponent but uses the ninja suit as power source.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedNinjaSuitDrawSystem))]
public sealed partial class NinjaSuitDrawComponent : Component
{
    #region Prediction

    /// <summary>
    /// Whether there is any charge available to draw from the suit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanDraw;

    /// <summary>
    /// Whether there is sufficient charge to use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanUse;

    #endregion

    /// <summary>
    /// Whether drawing is enabled.
    /// Having no ninja suit or battery will still disable it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// How much the entity draws while enabled (in Watts).
    /// Set to 0 if you just wish to check for power upon activation.
    /// </summary>
    [DataField]
    public float DrawRate = 1f;

    /// <summary>
    /// How much power is used whenever the entity is "used" (in Joules).
    /// This is used to ensure the entity won't activate again without a minimum use power.
    /// </summary>
    [DataField]
    public float UseRate;

    /// <summary>
    /// When the next automatic power draw will occur
    /// </summary>
    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// How long to wait between power drawing.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}