using Content.Server.StationRecords.Systems;
using Content.Shared.Radio;
using Content.Shared.StationRecords;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.StationRecords.Components;

[RegisterComponent, Access(typeof(GeneralStationRecordConsoleSystem))]
public sealed partial class GeneralStationRecordConsoleComponent : Component
{
    /// <summary>
    /// Selected crewmember record id.
    /// Station always uses the station that owns the console.
    /// </summary>
    [DataField]
    public uint? ActiveKey;

    /// <summary>
    /// Qualities to filter a search by.
    /// </summary>
    [DataField]
    public StationRecordsFilter? Filter;

    /// <summary>
    /// Whether this Records Console is able to delete entries.
    /// </summary>
    [DataField]
    public bool CanDeleteEntries;

    // Sunrise added start - возможность редактировать отпечатки через емаг
    [DataField]
    public bool CanRedactSensitiveData;

    [DataField]
    public bool HasAccess;

    [DataField]
    public bool Silent;

    [DataField]
    public bool SkipAccessCheck;

    [DataField]
    public SoundSpecifier SuccessfulSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public SoundSpecifier FailedSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    public TimeSpan NextPrintTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId Paper = "Paper";

    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    [DataField]
    public List<ProtoId<RadioChannelPrototype>> AnnouncementChannels = ["Command", "Security"];
    // Sunrise added end
}
