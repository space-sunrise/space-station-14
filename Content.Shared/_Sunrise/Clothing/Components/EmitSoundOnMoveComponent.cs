using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Clothing.Components;

/// <summary>
///  Указывает, что предмет одежды издает звук при движении.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitSoundOnMoveComponent : Component
{
    /// <summary>
    /// Звук, который будет проигрываться.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("sound", required: true), AutoNetworkedField]
    public SoundSpecifier SoundCollection = new SoundCollectionSpecifier("ChurchBell"); // Placeholder value

    /// <summary>
    /// Требуется ли гравитация для работы предмета.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("requiresGravity"), AutoNetworkedField]
    public bool RequiresGravity = true;

    /// <summary>
    /// Координаты, где был проигран прошлый звук.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates LastPosition = EntityCoordinates.Invalid;

    /// <summary>
    ///   Расстояние, пройденное с момента воспроизведения звука.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float SoundDistance = 0f;

    /// <summary>
    ///   Надет ли этот предмет в корректный слот инвентаря.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsValidSlot = true;
}
