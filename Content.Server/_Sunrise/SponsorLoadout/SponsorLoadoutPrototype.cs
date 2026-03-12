using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server._Sunrise.SponsorLoadout;

[Prototype]
public sealed partial class SponsorLoadoutPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Equipment;

    [DataField("whitelistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? WhitelistJobs { get; private set; }

    [DataField("blacklistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? BlacklistJobs { get; private set; }

    [DataField("speciesRestriction")]
    public List<string>? SpeciesRestrictions { get; private set; }
}
