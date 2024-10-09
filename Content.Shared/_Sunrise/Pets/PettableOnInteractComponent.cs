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
    [AutoNetworkedField]
    public EntityUid? Master;

    /// <summary>
    /// Звук, который воспроизводится после успешного приручения
    /// </summary>
    [DataField]
    public SoundCollectionSpecifier? PettingSuccessfulSound;

    /// <summary>
    /// Доступные кнопки в меню управления питомца
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<PetControlPrototype>> AvailableControls = new();

    /// <summary>
    /// Доступные питомцу приказы
    /// </summary>
    [DataField(required: true)]
    public HashSet<PetOrderType> AllowedOrders = new HashSet<PetOrderType> {PetOrderType.Follow, PetOrderType.Stay};
}

/// <summary>
/// Типы приказов для питомца
/// </summary>
[Serializable, NetSerializable]
public enum PetOrderType : byte
{
    Stay,
    Follow,
    Attack,
}
