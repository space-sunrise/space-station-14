using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared._Sunrise.Weapons.Ranged;
[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class ConsumableAmmoComponent : Component
{
    /// <summary>
    /// нынешнее количество зарядов
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public int CurrentCharges;

    /// <summary>
    /// максимальное количество зарядов
    /// </summary>
    [DataField, ViewVariables]
    public int MaxCharges = 30;

    /// <summary>
    /// предметы, которые можно использовать для пополения зарядов с мультипликатором
    /// в качестве int, умножающим количеством получаемых зарядов на себя
    /// </summary>
    [DataField, ViewVariables]
    public Dictionary<EntProtoId, int> LoadableItems = new Dictionary<EntProtoId, int>();

    /// <summary>
    /// количество зарядов, требуемых для выстрела
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public int ChargesPerShot = 1;

    /// <summary>
    /// отношение затрат материала на один заряд
    /// </summary>
    [DataField, ViewVariables]
    public float ItemsPerCharge = 1f;

    /// <summary>
    /// проджектайл которым стреляет предмет
    /// </summary>
    [DataField, ViewVariables]
    public EntProtoId ProjectilePrototypeId;

    /// <summary>
    /// звук загрузки зарядов
    /// </summary>
    [DataField, ViewVariables]
    public SoundSpecifier? LoadSound;

    /// <summary>
    /// звук при попытке выстрелить без зарядов
    /// </summary>
    [DataField, ViewVariables]
    public SoundSpecifier? EmptySound;

    /// <summary>
    /// нужно чтобы не спамило звуком и фразой "нет зарядов!" при попытке стрелять без них,
    /// отношения к работоспособности не имеет
    /// </summary>
    [DataField]
    public bool PopupShownOnEmpty = false;
}
