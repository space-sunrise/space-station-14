using Content.Shared._Sunrise.CriminalRecords;
using Content.Shared._Sunrise.Laws;
using Content.Shared._Sunrise.CriminalRecords.Systems;
using Content.Shared.StationRecords;
using Content.Server._Sunrise.CriminalRecords.Components;
using Content.Server.StationRecords.Systems;
using Content.Server.Station.Systems;
using Content.Server._Sunrise.Laws.Systems;
using Content.Shared.Access.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CriminalRecords.Systems;

public sealed class SunriseCriminalRecordsSystem : SharedSunriseCriminalRecordsSystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly Robust.Shared.Timing.IGameTiming _timing = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly StationCorporateLawSystem _stationLaw = default!;

    private const int MaxLaws = 20;
    private const int MaxCircumstances = 10;
    private const int MaxNotesLength = 2048;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SunriseCriminalRecordsConsoleComponent>(SunriseCriminalRecordsConsoleKey.Key, subs =>
        {
            subs.Event<SunriseCriminalRecordsSelectRecordMessage>(OnSelectRecord);
            subs.Event<SunriseCriminalRecordsCreateCaseMessage>(OnCreateCase);
            subs.Event<SunriseCriminalRecordsUpdateCaseMessage>(OnUpdateCase);
            subs.Event<SunriseCriminalRecordsCloseCaseMessage>(OnCloseCase);
            subs.Event<SunriseCriminalRecordsSelectCaseMessage>(OnSelectCase);
            subs.Event<SunriseCriminalRecordsSetUIStateMessage>(OnSetUIState);
        });

        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, BoundUIOpenedEvent>(OnOpened);

        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, RecordModifiedEvent>(OnRecordEvent);
        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(OnRecordEvent);
        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, RecordRemovedEvent>(OnRecordEvent);
    }

    private void OnRecordEvent<T>(Entity<SunriseCriminalRecordsConsoleComponent> ent, ref T args)
    {
        UpdateUserInterface(ent.Owner, ent.Comp);
    }

    private void OnOpened(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnSelectRecord(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsSelectRecordMessage msg)
    {
        if (!CheckAccess(uid, msg.Actor))
            return;

        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (msg.RecordId == null)
        {
            component.SelectedKey = null;
        }
        else
        {
            var key = new StationRecordKey(msg.RecordId.Value, station);
            if (!_stationRecords.TryGetRecord<GeneralStationRecord>(key, out _))
                return;

            component.SelectedKey = key;
        }

        component.CurrentUIState = SunriseCriminalRecordsUIState.List;
        component.SelectedCaseId = null;
        UpdateUserInterface(uid, component);
    }

    private void OnSelectCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsSelectCaseMessage msg)
    {
        if (!CheckAccess(uid, msg.Actor))
            return;

        component.SelectedCaseId = msg.CaseId;
        component.CurrentUIState = SunriseCriminalRecordsUIState.Editor;
        UpdateUserInterface(uid, component);
    }

    private void OnCreateCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsCreateCaseMessage msg)
    {
        if (!CheckAccess(uid, msg.Actor))
            return;

        if (component.SelectedKey == null)
            return;

        if (TryComp<StationCriminalRecordsComponent>(component.SelectedKey.Value.OriginStation, out var records))
        {
            var cases = records.Records.GetValueOrDefault(component.SelectedKey.Value.Id, new List<CriminalCase>());
            var nextId = records.NextCaseIds.GetValueOrDefault(component.SelectedKey.Value.Id, 1u);

            var @case = new CriminalCase(nextId, _timing.CurTime)
            {
                OriginStation = GetNetEntity(component.SelectedKey.Value.OriginStation)
            };
            cases.Add(@case);
            records.Records[component.SelectedKey.Value.Id] = cases;
            records.NextCaseIds[component.SelectedKey.Value.Id] = nextId + 1;

            component.SelectedCaseId = nextId;
        }

        component.CurrentUIState = SunriseCriminalRecordsUIState.Editor;
        UpdateUserInterface(uid, component);
    }

    private void OnSetUIState(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsSetUIStateMessage msg)
    {
        if (!CheckAccess(uid, msg.Actor))
            return;

        component.CurrentUIState = msg.State;
        UpdateUserInterface(uid, component);
    }

    private void OnUpdateCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsUpdateCaseMessage msg)
    {
        if (!CheckAccess(uid, msg.Actor))
            return;

        if (component.SelectedKey == null || msg.CaseId != component.SelectedCaseId)
            return;

        if (TryComp<StationCriminalRecordsComponent>(component.SelectedKey.Value.OriginStation, out var records))
        {
            if (!records.Records.TryGetValue(component.SelectedKey.Value.Id, out var cases))
                return;

            var @case = cases.Find(c => c.Id == msg.CaseId);

            if (@case != null && @case.Status == CriminalCaseStatus.Open)
            {
                // Validate limits
                if (msg.Laws.Count > MaxLaws || msg.Circumstances.Count > MaxCircumstances)
                    return;

                if (msg.Notes?.Length > MaxNotesLength)
                    return;

                // Validate laws and circumstances against lawset
                var station = component.SelectedKey.Value.OriginStation;
                var validatedLaws = new List<ProtoId<CorporateLawPrototype>>();
                foreach (var lawId in msg.Laws)
                {
                    if (!_stationLaw.IsLawInEffectiveLawset(lawId, station))
                        return; // Reject message if any ID is invalid
                    validatedLaws.Add(lawId);
                }

                var validatedCircs = new List<ProtoId<CorporateLawPrototype>>();
                foreach (var circId in msg.Circumstances)
                {
                    if (!_stationLaw.IsCircumstanceInEffectiveLawset(circId, station))
                        return; // Reject message if any ID is invalid
                    validatedCircs.Add(circId);
                }

                @case.Laws = validatedLaws;
                @case.Circumstances = validatedCircs;
                @case.Notes = msg.Notes;
                @case.CalculatedSentence = CalculateSentence(@case, cases);
            }
        }

        UpdateUserInterface(uid, component);
    }

    private void OnCloseCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsCloseCaseMessage msg)
    {
        if (!CheckAccess(uid, msg.Actor))
            return;

        if (component.SelectedKey == null || msg.CaseId != component.SelectedCaseId)
            return;

        if (TryComp<StationCriminalRecordsComponent>(component.SelectedKey.Value.OriginStation, out var records))
        {
            if (records.Records.TryGetValue(component.SelectedKey.Value.Id, out var cases))
            {
                var @case = cases.Find(c => c.Id == msg.CaseId);

                if (@case != null && @case.Status == CriminalCaseStatus.Open)
                {
                    @case.Status = CriminalCaseStatus.Closed;
                    var lawset = _stationLaw.GetStationLawset(component.SelectedKey.Value.OriginStation);
                    @case.CalculatedSentence = CalculateSentence(@case, cases);
                }
            }
        }

        component.CurrentUIState = SunriseCriminalRecordsUIState.List;
        component.SelectedCaseId = null;
        UpdateUserInterface(uid, component);
    }

    private bool CheckAccess(EntityUid console, EntityUid? user)
    {
        if (user == null)
            return false;

        return _accessReader.IsAllowed(user.Value, console);
    }


    private void UpdateUserInterface(EntityUid uid, SunriseCriminalRecordsConsoleComponent component)
    {
        var station = _station.GetOwningStation(uid);
        if (!TryComp<StationRecordsComponent>(station, out var stationRecordsComp))
            return;

        var records = _stationRecords.BuildListing((station.Value, stationRecordsComp), null);

        string? selectedName = null;
        string? jobTitle = null;
        string? jobIcon = null;
        int? age = null;
        string? gender = null;
        string? species = null;
        string? fingerprints = null;
        string? dna = null;
        List<CriminalCase> cases = new();

        if (component.SelectedKey != null)
        {
            if (_stationRecords.TryGetRecord<GeneralStationRecord>(component.SelectedKey.Value, out var general))
            {
                selectedName = general.Name;
                jobTitle = general.JobTitle;
                jobIcon = general.JobIcon;
                age = general.Age;
                gender = general.Gender.ToString();
                species = general.Species;
                fingerprints = general.Fingerprint;
                dna = general.DNA;
            }

            if (TryComp<StationCriminalRecordsComponent>(station.Value, out var criminalRecords))
            {
                if (criminalRecords.Records.TryGetValue(component.SelectedKey.Value.Id, out var personCases))
                {
                    cases = personCases;
                }
            }
        }

        var state = new SunriseCriminalRecordsConsoleState(
            records,
            selectedName,
            cases,
            component.SelectedKey?.Id,
            component.SelectedCaseId,
            component.CurrentUIState,
            jobTitle,
            jobIcon,
            age,
            gender,
            species,
            fingerprints,
            dna);
        _ui.SetUiState(uid, SunriseCriminalRecordsConsoleKey.Key, state);
    }

}
