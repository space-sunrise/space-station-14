using Content.Shared._Sunrise.FleshCult;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.FleshCult.GameRule;

[RegisterComponent, Access(typeof(FleshCultRuleSystem))]
public sealed partial class FleshCultRuleComponent : Component
{
    public EntityUid CultistsLeaderMind = new();

    public SoundSpecifier AddedSound = new SoundPathSpecifier(
        "/Audio/_Sunrise/FleshCult/flesh_culstis_greeting.ogg");

    public Dictionary<string, EntityUid> Cultists = new();

    [DataField("fleshCultistPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string FleshCultistPrototypeId = "FleshCultist";

    [DataField("fleshCultistLeaderPrototypeID", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string FleshCultistLeaderPrototypeId = "FleshCultistLeader";

    [DataField("faction", customTypeSerializer: typeof(PrototypeIdSerializer<NpcFactionPrototype>), required: true)]
    public string Faction = default!;

    public int TotalCultists => Cultists.Count;

    public readonly List<string> CultistsNames = new();

    public WinTypes WinType = WinTypes.Fail;

    public bool FleshHeartActive = false;

    public Dictionary<EntityUid, FleshHeartStatus> FleshHearts = new();

    public EntityUid? TargetStation;

    [DataField]
    public List<string> StarterItems = new() { "SyringeCarolNT", "SyringeCarolNT", "SyringeCarolNT" };

    public List<string> SpeciesWhitelist = new()
    {
        "Human",
        "Reptilian",
        "Dwarf",
        "Vulpkanin",
        "Felinid",
        "Moth",
        "Swine",
        "Arachnid"
    };

    public enum WinTypes
    {
        FleshHeartFinal,
        AllCultistsDead,
        Fail
    }

    public TimeSpan AnnounceAt = TimeSpan.Zero;
    public Dictionary<ICommonSession, HumanoidCharacterProfile> StartCandidates = new();
}
