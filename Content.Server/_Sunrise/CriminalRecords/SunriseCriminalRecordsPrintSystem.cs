using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._Sunrise.CriminalRecords;
using Content.Shared._Sunrise.Humanoid;
using Content.Shared._Sunrise.Records;
using Content.Shared.CriminalRecords;
using Content.Shared.CriminalRecords.Components;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.CriminalRecords.Systems;

/// <summary>
/// Sunrise: обработка печати охранного досье из консоли криминальных записей.
/// </summary>
public sealed partial class SunriseCriminalRecordsPrintSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly JobSystem _job = default!;

    private TimeSpan _nextPrintTime = TimeSpan.Zero;
    private static readonly TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        Subs.BuiEvents<CriminalRecordsConsoleComponent>(CriminalRecordsConsoleKey.Key, subs =>
        {
            subs.Event<SunrisePrintCriminalRecord>(OnPrint);
        });
    }

    private void OnPrint(Entity<CriminalRecordsConsoleComponent> ent, ref SunrisePrintCriminalRecord args)
    {
        var user = args.Actor;

        if (_timing.CurTime < _nextPrintTime)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent, user);
            return;
        }

        var owning = _station.GetOwningStation(ent.Owner);
        if (owning == null)
            return;

        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(
                new StationRecordKey(args.Id, owning.Value), out var record))
            return;

        var printed = Spawn("Paper", Transform(ent).Coordinates);
        _hands.PickupOrDrop(user, printed, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
            return;

        _metaData.SetEntityName(printed,
            Loc.GetString("printed-security-records-document-name", ("name", record.Name)));

        var text = Loc.GetString(
            "printed-security-records-content",
            ("name", record.Name),
            ("fullname", GetText(record.FullName)),
            ("job", GetJobName(record.JobPrototype)),
            ("department", GetDepartmentName(record.JobPrototype)),
            ("dob", GetText(record.DateOfBirth)),
            ("securityrecord", StructuredRecordFormatter.FormatSecurity(record.SecurityRecord, Loc.GetString,
                HumanoidBodyMetrics.FormatHeight(Loc, _prototype, record.Species, record.HumanoidProfile),
                HumanoidBodyMetrics.FormatWeight(Loc, _prototype, record.Species, record.HumanoidProfile)))
        );

        _paper.SetContent((printed, paperComp), text);
        _audio.PlayPvs(
            new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg"),
            ent,
            AudioParams.Default.WithVariation(0.25f).WithVolume(4f).WithRolloffFactor(2.8f).WithMaxDistance(4.5f));

        _nextPrintTime = _timing.CurTime + PrintCooldown;
    }

    private string GetText(string text)
        => string.IsNullOrWhiteSpace(text) ? Loc.GetString("printed-station-records-unrecognized") : text;

    private string GetJobName(ProtoId<JobPrototype> job)
        => _prototype.TryIndex(job, out var proto) ? proto.LocalizedName : GetText(string.Empty);

    private string GetDepartmentName(ProtoId<JobPrototype> job)
        => _job.TryGetDepartment(job, out var dept) ? Loc.GetString(dept.Name) : GetText(string.Empty);
}
