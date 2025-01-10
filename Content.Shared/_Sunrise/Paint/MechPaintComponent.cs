using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Paint;

/// <summary>
/// Entity when used on another entity will paint target entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMechPaintSystem))]
public sealed partial class MechPaintComponent : Component
{
    /// <summary>
    /// Noise made when paint applied.
    /// </summary>
    [DataField]
    public SoundSpecifier Spray = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    /// <summary>
    /// This paint was used?
    /// </summary>
    [DataField]
    public bool Used = false;

    /// <summary>
    /// How long the doafter will take.
    /// </summary>
    [DataField]
    public int Delay = 2;
    
    /// <summary>
    /// What mech are paint?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Paint states
    /// </summary>
    #region Visualizer States
    [DataField]
    public string BaseState;
    [DataField]
    public string OpenState;
    [DataField]
    public string BrokenState;
    #endregion
}
