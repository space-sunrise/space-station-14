// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Clothing;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// This is used for items that change your speed when they are held.
/// </summary>
/// <remarks>
/// This is separate from <see cref="ClothingSpeedModifierComponent"/> because things like boots increase/decrease speed when worn, but
/// shouldn't do that when just held in hand.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SpeedModifierOnSystem))]
public sealed partial class SpeedModifierOnComponent : Component
{
    /// <summary>
    /// A multiplier applied to the walk speed.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float WalkModifier = 0.6f;

    /// <summary>
    /// A multiplier applied to the sprint speed.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float SprintModifier = 0.6f;

    [DataField] public bool TurnedOff;
}
