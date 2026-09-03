using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Mech.Components;

/// <summary>
/// Изменяет скорость меха, пока предмет установлен в его слот питания.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechSpeedModifierComponent : Component
{
    /// <summary>
    /// Множитель скорости шага.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float WalkModifier = 1f;

    /// <summary>
    /// Множитель скорости бега.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float SprintModifier = 1f;
}
