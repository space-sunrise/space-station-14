using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._Sunrise.CriminalRecords;
using Content.Shared._Sunrise.CriminalRecords.Components;
using Content.Shared.Access.Components;
using Content.Shared.StationRecords;
using Content.Shared.DeviceLinking;
using Content.Server.DeviceLinking.Systems;
using Content.Server._Sunrise.CriminalRecords.Components;
using Content.Shared._Sunrise.Laws;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.CriminalRecords.Systems;

public sealed partial class PrisonerManagementConsoleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly PrisonLockerSystem _locker = default!;
    [Dependency] private readonly PrisonTimerSystem _timer = default!;
    [Dependency] private readonly PrisonCellDoorSystem _door = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    private const int NumCells = 10;
    private readonly List<int> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrisonerManagementConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PrisonerManagementConsoleComponent, BoundUIOpenedEvent>(OnOpened);

        Subs.BuiEvents<PrisonerManagementConsoleComponent>(PrisonerManagementConsoleKey.Key, subs =>
        {
            subs.Event<PrisonerManagementStartIncarcerationMessage>(OnStartIncarceration);
        });
    }

    private void OnComponentInit(EntityUid uid, PrisonerManagementConsoleComponent component, ComponentInit args)
    {
        var lockPorts = new List<ProtoId<SourcePortPrototype>>();
        var unlockPorts = new List<ProtoId<SourcePortPrototype>>();

        for (int i = 0; i < NumCells; i++)
        {
            lockPorts.Add($"Cell{i + 1}Lock");
            unlockPorts.Add($"Cell{i + 1}Unlock");
        }

        _deviceLink.EnsureSourcePorts(uid, lockPorts.ToArray());
        _deviceLink.EnsureSourcePorts(uid, unlockPorts.ToArray());
    }


    private void OnOpened(EntityUid uid, PrisonerManagementConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnStartIncarceration(EntityUid uid, PrisonerManagementConsoleComponent component, PrisonerManagementStartIncarcerationMessage msg)
    {
        if (!CheckAccess(uid, msg.Actor))
            return;

        if (_station.GetOwningStation(uid) is not { } station)
            return;

        var key = new StationRecordKey(msg.RecordId, station);
        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
            return;

        if (TryComp<StationCriminalRecordsComponent>(station, out var sunriseRecord))
        {
            var cases = sunriseRecord.Records.GetValueOrDefault(msg.RecordId, new List<CriminalCase>());
            var @case = cases.Find(c => c.Id == msg.CaseId);

            if (@case == null || @case.Status != CriminalCaseStatus.Closed)
                return;

            if (msg.CellIndex is < -1 or >= NumCells)
                return;

            if (msg.CellIndex >= 0 && component.ActiveIncarcerations.ContainsKey(msg.CellIndex))
                return;

            var accessId = "PrisonerAccess_" + Guid.NewGuid().ToString().Substring(0, 8);
            var incarceration = new ActiveIncarceration
            {
                RecordKey = key,
                CaseId = msg.CaseId,
                PrisonerAccessId = accessId,
                StartTime = _timing.CurTime,
                SentenceMinutes = @case.CalculatedSentence
            };

            // Use a unique negative index for permanent prisoners to allow multiple at once
            var cellIndex = msg.CellIndex;
            if (cellIndex < 0)
            {
                cellIndex = -1;
                while (component.ActiveIncarcerations.ContainsKey(cellIndex))
                {
                    cellIndex--;
                }
            }

            component.ActiveIncarcerations[cellIndex] = incarceration;
            @case.Status = CriminalCaseStatus.Incarcerated;
            @case.IncarcerationStartTime = _timing.CurTime;

            // 1. Spawn ID Card
            var idCard = Spawn("PlanetPrisonerIDCard", Transform(uid).Coordinates);
            _idCard.TryChangeFullName(idCard, generalRecord.Name);
            _idCard.TryChangeJobTitle(idCard, Loc.GetString("job-prisoner-title"));

            var access = EnsureComp<AccessComponent>(idCard);
            access.Tags.Add(new ProtoId<AccessLevelPrototype>(accessId));
            Dirty(idCard, access);


            // 2. Send Device Signals (Only if we have a real cell)
            if (cellIndex >= 0)
                SendCellSignals(uid, cellIndex, true, accessId, TimeSpan.FromMinutes(@case.CalculatedSentence));

            Dirty(station, sunriseRecord);
            UpdateUserInterface(uid, component);
        }
    }

    private void SendCellSignals(EntityUid uid, int cellIndex, bool isLock, string? accessId = null, TimeSpan? duration = null)
    {
        var cellNumber = cellIndex + 1;
        var cellLabel = Loc.GetString("prison-timer-cell-label", ("number", cellNumber));

        var lockPort = $"Cell{cellNumber}Lock";
        var unlockPort = $"Cell{cellNumber}Unlock";

        _deviceLink.InvokePort(uid, isLock ? lockPort : unlockPort);

        var sinks = _deviceLink.GetLinkedSinks(uid, isLock ? lockPort : unlockPort);
        foreach (var sink in sinks)
        {
            if (isLock)
            {
                if (accessId != null)
                {
                    if (HasComp<PrisonLockerComponent>(sink))
                        _locker.LockLocker(sink, accessId);
                    else if (HasComp<PrisonCellDoorComponent>(sink))
                        _door.LockDoor(sink);

                    if (duration != null && HasComp<PrisonTimerComponent>(sink))
                        _timer.SetTimer(sink, cellLabel, duration.Value);
                }
            }
            else
            {
                if (HasComp<PrisonCellDoorComponent>(sink))
                    _door.UnlockDoor(sink);

                if (HasComp<PrisonTimerComponent>(sink))
                    _timer.ResetTimer(sink, cellLabel);
            }
        }
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PrisonerManagementConsoleComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var changed = false;
            _toRemove.Clear();
            var threshold = GetPermanentThreshold();
            foreach (var entry in component.ActiveIncarcerations)
            {
                var cellIdx = entry.Key;
                var incar = entry.Value;

                // Permanent prisoners (threshold or negative cell index) stay indefinitely
                if (incar.SentenceMinutes >= threshold || cellIdx < 0)
                    continue;

                if (curTime >= incar.StartTime + TimeSpan.FromMinutes(incar.SentenceMinutes))
                {
                    FinishIncarceration(uid, component, cellIdx, incar);
                    _toRemove.Add(cellIdx);
                    changed = true;
                }
            }

            foreach (var idx in _toRemove)
            {
                component.ActiveIncarcerations.Remove(idx);
            }

            if (changed)
                UpdateUserInterface(uid, component);
        }
    }

    private int GetPermanentThreshold()
    {
        if (_proto.TryIndex<CorporateLawsetPrototype>("StandardCorporateLaw", out var lawset))
            return lawset.PermanentSentenceThreshold;
        return 50;
    }

    private void FinishIncarceration(EntityUid uid, PrisonerManagementConsoleComponent component, int cellIndex, ActiveIncarceration incar)
    {
        if (TryComp<StationCriminalRecordsComponent>(incar.RecordKey.OriginStation, out var sunrise))
        {
            if (sunrise.Records.TryGetValue(incar.RecordKey.Id, out var cases))
            {
                var @case = cases.Find(c => c.Id == incar.CaseId);
                if (@case != null)
                {
                    @case.Status = CriminalCaseStatus.Finished;
                    Dirty(incar.RecordKey.OriginStation, sunrise);
                }
            }
        }

        if (cellIndex >= 0)
            SendCellSignals(uid, cellIndex, false, incar.PrisonerAccessId);
    }

    private bool CheckAccess(EntityUid console, EntityUid? user)
    {
        if (user == null)
            return false;

        return _accessReader.IsAllowed(user.Value, console);
    }

    private void UpdateUserInterface(EntityUid uid, PrisonerManagementConsoleComponent component)
    {
        if (_station.GetOwningStation(uid) is not { } station)
            return;

        var waiting = new List<PrisonerRecordInfo>();
        var finished = new List<PrisonerRecordInfo>();
        var inProgress = new List<IncarcerationInfo>();
        var cellOccupied = new Dictionary<int, bool>();
        var cellEquipped = new Dictionary<int, bool>();

        for (int i = 0; i < NumCells; i++)
        {
            cellOccupied[i] = component.ActiveIncarcerations.ContainsKey(i);

            // Check equipment status
            var cellNumber = i + 1;
            var lockPort = $"Cell{cellNumber}Lock";
            var unlockPort = $"Cell{cellNumber}Unlock";

            bool hasLocker = false;
            bool hasDoor = false;
            bool hasTimer = false;

            var sinks = _deviceLink.GetLinkedSinks(uid, lockPort).Concat(_deviceLink.GetLinkedSinks(uid, unlockPort)).Distinct();
            foreach (var sink in sinks)
            {
                if (HasComp<PrisonLockerComponent>(sink)) hasLocker = true;
                if (HasComp<PrisonCellDoorComponent>(sink)) hasDoor = true;
                if (HasComp<PrisonTimerComponent>(sink)) hasTimer = true;
            }

            cellEquipped[i] = hasLocker && hasDoor && hasTimer;
        }

        var allRecords = _stationRecords.GetRecordsOfType<GeneralStationRecord>(station);
        TryComp<StationCriminalRecordsComponent>(station, out var sunrise);

        foreach (var (id, general) in allRecords)
        {
            if (sunrise == null || !sunrise.Records.TryGetValue(id, out var cases))
                continue;

            foreach (var @case in cases)
            {
                if (@case.Status == CriminalCaseStatus.Closed)
                {
                    waiting.Add(new PrisonerRecordInfo(id, general.Name, general.JobTitle ?? "", @case.Id, @case.CalculatedSentence));
                }
                else if (@case.Status == CriminalCaseStatus.Finished)
                {
                    finished.Add(new PrisonerRecordInfo(id, general.Name, general.JobTitle ?? "", @case.Id, @case.CalculatedSentence));
                }
            }
        }

        foreach (var entry in component.ActiveIncarcerations)
        {
            var idx = entry.Key;
            var incar = entry.Value;

            if (_stationRecords.TryGetRecord<GeneralStationRecord>(incar.RecordKey, out var general))
            {
                inProgress.Add(new IncarcerationInfo(incar.RecordKey.Id, general.Name, incar.CaseId, idx, incar.StartTime, incar.SentenceMinutes));
            }
        }

        var state = new PrisonerManagementConsoleState(waiting, inProgress, finished, cellOccupied, cellEquipped, GetPermanentThreshold());
        _ui.SetUiState(uid, PrisonerManagementConsoleKey.Key, state);
    }
}
