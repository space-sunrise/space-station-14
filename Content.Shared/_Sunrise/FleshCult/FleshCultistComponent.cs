using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.FleshCult;

[RegisterComponent, NetworkedComponent]
public sealed partial class FleshCultistComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public FixedPoint2 Hunger = 100;

    [ViewVariables(VVAccess.ReadWrite), DataField("hungerСonsumption")]
    public FixedPoint2 HungerСonsumption = -0.025; // 100 hunger in 60 minutes

    [ViewVariables(VVAccess.ReadWrite), DataField("maxHunger")]
    public FixedPoint2 MaxHunger = 100;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionFleshCultistShop = "FleshCultistShop";

    [DataField]
    public EntityUid? ActionFleshCultistShopEntity;

    [DataField("stolenCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenCurrencyPrototype = "StolenMutationPoint";

    [ViewVariables(VVAccess.ReadWrite),
     DataField("fleshMutationMobId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FleshMutationMobId = "MobFleshPudge";

    public SoundSpecifier BuySuccesSound = new SoundPathSpecifier(
        "/Audio/_Sunrise/FleshCult/flesh_cultist_buy_succes.ogg");

    [ViewVariables] public float Accumulator = 0;

    [ViewVariables] public float AccumulatorStarveNotify = 0;

    [DataField("fleshStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "FleshFaction";

    [DataField]
    public ProtoId<AlertPrototype> MutationPointAlert = "MutationPoint";

    [DataField]
    public SoundSpecifier SoundMutation = new SoundPathSpecifier("/Audio/_Sunrise/FleshCult/flesh_cultist_mutation.ogg");
}
