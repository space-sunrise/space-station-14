using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Silicons.Borgs;

/// <summary>
/// Stores the selected gender model for a borg chassis.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedBorgGenderSystem))]
public sealed partial class BorgGenderComponent : Component
{
    /// <summary>
    /// Gender variant selected for this specific borg body.
    /// </summary>
    [DataField, AutoNetworkedField]
    public BorgGender SelectedGender = BorgGender.Male;
}

/// <summary>
/// Gender variants supported by borg body sprites.
/// </summary>
[Serializable, NetSerializable]
public enum BorgGender : byte
{
    Male,
    Female,
}

/// <summary>
/// BUI key for changing borg gender.
/// </summary>
[Serializable, NetSerializable]
public enum BorgGenderUiKey : byte
{
    Key,
}
