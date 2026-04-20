using System.Linq;
using Content.Shared._Sunrise.CriminalRecords;
using Content.Shared._Sunrise.CriminalRecords.Components;
using Content.Shared._Sunrise.CriminalRecords.Systems;
using Content.Shared.StationRecords;
using Content.Server._Sunrise.CriminalRecords.Components;
using Content.Server.StationRecords.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.Laws;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CriminalRecords.Systems;

public sealed class SunriseCriminalRecordsSystem : SharedSunriseCriminalRecordsSystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly Robust.Shared.Timing.IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, SunriseCriminalRecordsSelectRecordMessage>(OnSelectRecord);
        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, SunriseCriminalRecordsCreateCaseMessage>(OnCreateCase);
        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, SunriseCriminalRecordsUpdateCaseMessage>(OnUpdateCase);
        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, SunriseCriminalRecordsCloseCaseMessage>(OnCloseCase);
        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, SunriseCriminalRecordsSelectCaseMessage>(OnSelectCase);
        SubscribeLocalEvent<SunriseCriminalRecordsConsoleComponent, SunriseCriminalRecordsSetUIStateMessage>(OnSetUIState);
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
        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (msg.RecordId == null)
            component.SelectedKey = null;
        else
            component.SelectedKey = new StationRecordKey(msg.RecordId.Value, station);

        component.CurrentUIState = SunriseCriminalRecordsUIState.List;
        component.SelectedCaseId = null;
        UpdateUserInterface(uid, component);
    }

    private void OnSelectCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsSelectCaseMessage msg)
    {
        component.SelectedCaseId = msg.CaseId;
        component.CurrentUIState = SunriseCriminalRecordsUIState.Editor;
        UpdateUserInterface(uid, component);
    }

    private void OnCreateCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsCreateCaseMessage msg)
    {
        if (component.SelectedKey == null)
            return;

        if (TryComp<StationCriminalRecordsComponent>(component.SelectedKey.Value.OriginStation, out var records))
        {
            var cases = records.Records.GetValueOrDefault(component.SelectedKey.Value.Id, new List<CriminalCase>());
            var nextId = records.NextCaseIds.GetValueOrDefault(component.SelectedKey.Value.Id, 1u);

            var @case = new CriminalCase(nextId, _timing.CurTime);
            cases.Add(@case);
            records.Records[component.SelectedKey.Value.Id] = cases;
            records.NextCaseIds[component.SelectedKey.Value.Id] = nextId + 1;

            component.SelectedCaseId = nextId;

            Dirty(component.SelectedKey.Value.OriginStation, records);
        }

        component.CurrentUIState = SunriseCriminalRecordsUIState.Editor;
        UpdateUserInterface(uid, component);
    }

    private void OnSetUIState(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsSetUIStateMessage msg)
    {
        component.CurrentUIState = msg.State;
        UpdateUserInterface(uid, component);
    }

    private void OnUpdateCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsUpdateCaseMessage msg)
    {
        if (component.SelectedKey == null)
            return;

        if (TryComp<StationCriminalRecordsComponent>(component.SelectedKey.Value.OriginStation, out var records))
        {
            var cases = records.Records.GetValueOrDefault(component.SelectedKey.Value.Id, new List<CriminalCase>());
            var @case = component.SelectedCaseId == null
                ? cases.FindLast(c => c.Status == CriminalCaseStatus.Open)
                : cases.Find(c => c.Id == component.SelectedCaseId);

            if (@case != null && @case.Status == CriminalCaseStatus.Open)
            {
                @case.Laws = msg.Laws;
                @case.Circumstances = msg.Circumstances;
                @case.Notes = msg.Notes;
                @case.CalculatedSentence = CalculateSentence(@case, cases);
                Dirty(component.SelectedKey.Value.OriginStation, records);
            }
        }

        UpdateUserInterface(uid, component);
    }

    private void OnCloseCase(EntityUid uid, SunriseCriminalRecordsConsoleComponent component, SunriseCriminalRecordsCloseCaseMessage msg)
    {
        if (component.SelectedKey == null)
            return;

        if (TryComp<StationCriminalRecordsComponent>(component.SelectedKey.Value.OriginStation, out var records))
        {
            if (records.Records.TryGetValue(component.SelectedKey.Value.Id, out var cases))
            {
                var @case = component.SelectedCaseId == null
                    ? cases.FindLast(c => c.Status == CriminalCaseStatus.Open)
                    : cases.Find(c => c.Id == component.SelectedCaseId);

                if (@case != null && @case.Status == CriminalCaseStatus.Open)
                {
                    @case.Status = CriminalCaseStatus.Closed;
                    @case.CalculatedSentence = CalculateSentence(@case, cases);
                    Dirty(component.SelectedKey.Value.OriginStation, records);
                }
            }
        }

        component.CurrentUIState = SunriseCriminalRecordsUIState.List;
        component.SelectedCaseId = null;
        UpdateUserInterface(uid, component);
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

    private int CalculateSentence(CriminalCase @case, List<CriminalCase> allCases)
    {
        if (!_proto.TryIndex<CorporateLawsetPrototype>("StandardCorporateLaw", out var lawset))
            return 0;

        // 1. Group articles by section and find maximum for each
        var sectionMaxes = new Dictionary<string, int>();
        int heaviestArticleBase = 0;

        foreach (var lawId in @case.Laws)
        {
            if (!_proto.TryIndex<CorporateLawPrototype>(lawId, out var law))
                continue;

            heaviestArticleBase = Math.Max(heaviestArticleBase, law.BaseSentence);

            // Find section
            string sectionId = "unknown";
            foreach (var s in lawset.Articles)
            {
                if (!_proto.TryIndex<CorporateLawSectionPrototype>(s, out var section) || !section.Entries.Contains(lawId))
                    continue;
                sectionId = s;
                break;
            }

            if (!sectionMaxes.TryGetValue(sectionId, out var currentMax) || law.BaseSentence > currentMax)
                sectionMaxes[sectionId] = law.BaseSentence;
        }

        // 2. Sum up section maxes and apply cap
        int baseSum = sectionMaxes.Values.Sum();
        float cap = heaviestArticleBase * 1.5f;
        int cappedBase = (int) Math.Min(baseSum, cap);

        // 3. Multipliers (Circumstances & Recidivism)
        float multiplierModifier = 0.0f; // Additive part for recidivism
        float multiplierFactor = 1.0f;   // Multiplicative part for circumstances

        // Circumstances
        foreach (var circId in @case.Circumstances)
        {
            if (_proto.TryIndex<CorporateLawPrototype>(circId, out var law))
                multiplierFactor *= law.SentenceMultiplier;
        }

        // Recidivism: +15% per unique repeating article
        var pastLaws = allCases
            .Where(c => c.Id != @case.Id)
            .SelectMany(c => c.Laws)
            .ToHashSet();

        foreach (var lawId in @case.Laws.Distinct())
        {
            if (pastLaws.Contains(lawId))
                multiplierModifier += 0.15f;
        }

        return (int) Math.Round(cappedBase * multiplierFactor * (1.0f + multiplierModifier));
    }
}
