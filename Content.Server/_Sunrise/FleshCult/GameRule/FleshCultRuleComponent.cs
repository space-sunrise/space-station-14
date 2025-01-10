using Content.Shared._Sunrise.FleshCult;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.FleshCult.GameRule;

[RegisterComponent, Access(typeof(FleshCultRuleSystem))]
public sealed partial class FleshCultRuleComponent : Component
{
    public EntityUid? CultistsLeaderMind;

    public SoundSpecifier AddedSound = new SoundPathSpecifier(
        "/Audio/_Sunrise/FleshCult/flesh_culstis_greeting.ogg");

    [DataField("fleshCultistPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string FleshCultistPrototypeId = "FleshCultist";

    [DataField("fleshCultistLeaderPrototypeID", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string FleshCultistLeaderPrototypeId = "FleshCultistLeader";

    [DataField("faction", customTypeSerializer: typeof(PrototypeIdSerializer<NpcFactionPrototype>), required: true)]
    public string Faction = default!;

    public List<EntityUid> Cultists = new();
    public int TotalCultists => Cultists.Count;

    public readonly List<string> CultistsNames = new();

    public Dictionary<EntityUid, FleshHeartStatus> FleshHearts = new();

    public EntityUid? TargetStation;
}
