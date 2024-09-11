using Content.Shared._Sunrise.Pets.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Pets;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPettingSystem))]
public sealed partial class PettableOnInteractComponent : Component
{
    /// <summary>
    /// Владелец питомца. Для изменения использовать PettingSystem.SetMaster()
    /// </summary>
    public EntityUid? Master;

    /// <summary>
    /// Звук, который воспроизводится после успешного приручения
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier? PettingSuccessfulSound;

    public PetOrderType CurrentOrder = PetOrderType.Follow;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<PetControlPrototype>> AvailableControls = new();
}

/// <summary>
/// Типы приказов для питомца
/// </summary>
[Serializable, NetSerializable]
public enum PetOrderType : byte
{
    Stay,
    Follow,
}
