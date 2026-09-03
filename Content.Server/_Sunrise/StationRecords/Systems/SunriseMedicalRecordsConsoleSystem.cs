using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Roles.Jobs;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._Sunrise.Humanoid;
using Content.Shared._Sunrise.Records;
using Content.Shared._Sunrise.StationRecords;
using Content.Shared.Access.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.StationRecords;

public sealed class SunriseMedicalRecordsConsoleSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly JobSystem _job = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;

    private TimeSpan _nextPrintTime = TimeSpan.Zero;
    private static readonly TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    public override void Initialize()
    {
        SubscribeLocalEvent<SunriseMedicalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUi);
        SubscribeLocalEvent<SunriseMedicalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUi);

        Subs.BuiEvents<SunriseMedicalRecordsConsoleComponent>(SunriseMedicalRecordsConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
            subs.Event<SunrisePrintMedicalRecord>(OnPrint);
            subs.Event<SunriseSaveMedicalRecord>(OnSave);
        });
    }

    private void UpdateUi<T>(Entity<SunriseMedicalRecordsConsoleComponent> ent, ref T args)
        => UpdateUserInterface(ent);

    private void OnUiOpened(Entity<SunriseMedicalRecordsConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        ent.Comp.HasAccess = _access.IsAllowed(args.Actor, ent);
        UpdateUserInterface(ent);
    }

    private void OnKeySelected(Entity<SunriseMedicalRecordsConsoleComponent> ent, ref SelectStationRecord args)
    {
        ent.Comp.ActiveKey = args.SelectedKey;
        UpdateUserInterface(ent);
    }

    private void OnFiltersChanged(Entity<SunriseMedicalRecordsConsoleComponent> ent, ref SetStationRecordFilter args)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != args.Type || ent.Comp.Filter.Value != args.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(args.Type, args.Value);
            UpdateUserInterface(ent);
        }
    }

    private void OnSave(Entity<SunriseMedicalRecordsConsoleComponent> ent, ref SunriseSaveMedicalRecord args)
    {
        if (!ent.Comp.HasAccess)
        {
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg"), ent);
            return;
        }

        var owning = _station.GetOwningStation(ent.Owner);
        if (owning == null)
            return;

        var key = new StationRecordKey(args.Id, owning.Value);

        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record))
            return;

        // Sunrise-Records: клиент теперь редактирует только поле заметок структурированного досье,
        // остальные поля заполняются в лобби при создании персонажа и отображаются только для чтения.
        var medical = StructuredCharacterRecords.ReadMedical(record.MedicalRecord);
        medical.Notes = args.MedicalRecord;
        var updated = record with { MedicalRecord = StructuredCharacterRecords.WriteMedical(medical) };
        _stationRecords.AddRecordEntry(key, updated);
        _stationRecords.Synchronize(owning.Value);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg"), ent);
        UpdateUserInterface(ent);
    }

    private void OnPrint(Entity<SunriseMedicalRecordsConsoleComponent> ent, ref SunrisePrintMedicalRecord args)
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
            Loc.GetString("printed-medical-records-document-name", ("name", record.Name)));

        var text = Loc.GetString(
            "printed-medical-records-content",
            ("name", record.Name),
            ("fullname", GetText(record.FullName)),
            ("job", GetJobName(record.JobPrototype)),
            ("department", GetDepartmentName(record.JobPrototype)),
            ("age", record.Age),
            ("dob", GetText(record.DateOfBirth)),
            ("species", GetSpeciesName(record.Species)),
            ("medicalrecord", StructuredRecordFormatter.FormatMedical(record.MedicalRecord, Loc.GetString,
                HumanoidBodyMetrics.FormatHeight(Loc, _prototype, record.Species, record.HumanoidProfile),
                HumanoidBodyMetrics.FormatWeight(Loc, _prototype, record.Species, record.HumanoidProfile),
                RecordTraitSummary.FormatDisabilities(Loc, _prototype, record.HumanoidProfile)))
        );

        _paper.SetContent((printed, paperComp), text);
        _audio.PlayPvs(
            new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg"),
            ent,
            AudioParams.Default.WithVariation(0.25f).WithVolume(4f).WithRolloffFactor(2.8f).WithMaxDistance(4.5f));

        _nextPrintTime = _timing.CurTime + PrintCooldown;
    }

    private void UpdateUserInterface(Entity<SunriseMedicalRecordsConsoleComponent> ent)
    {
        var owning = _station.GetOwningStation(ent.Owner);
        if (owning == null)
        {
            _ui.SetUiState(ent.Owner, SunriseMedicalRecordsConsoleKey.Key,
                new SunriseMedicalRecordsConsoleState());
            return;
        }

        if (!TryComp<StationRecordsComponent>(owning, out var records))
        {
            _ui.SetUiState(ent.Owner, SunriseMedicalRecordsConsoleKey.Key,
                new SunriseMedicalRecordsConsoleState());
            return;
        }

        var listing = _stationRecords.BuildListing((owning.Value, records), ent.Comp.Filter);

        GeneralStationRecord? selected = null;
        if (ent.Comp.ActiveKey is { } key)
            _stationRecords.TryGetRecord(new StationRecordKey(key, owning.Value), out selected);

        _ui.SetUiState(ent.Owner, SunriseMedicalRecordsConsoleKey.Key,
            new SunriseMedicalRecordsConsoleState(
                ent.Comp.ActiveKey,
                selected,
                listing,
                ent.Comp.Filter,
                ent.Comp.HasAccess));
    }

    private string GetText(string text)
        => string.IsNullOrWhiteSpace(text) ? Loc.GetString("printed-station-records-unrecognized") : text;

    private string GetJobName(ProtoId<JobPrototype> job)
        => _prototype.TryIndex(job, out var proto) ? proto.LocalizedName : GetText(string.Empty);

    private string GetDepartmentName(ProtoId<JobPrototype> job)
        => _job.TryGetDepartment(job, out var dept) ? Loc.GetString(dept.Name) : GetText(string.Empty);

    private string GetSpeciesName(ProtoId<SpeciesPrototype> species)
        => _prototype.TryIndex(species, out var proto) ? Loc.GetString(proto.Name) : GetText(string.Empty);
}
