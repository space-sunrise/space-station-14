using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Delivery;
using Content.Shared.FingerprintReader;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.StationRecords;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes; // Sunrise-add
using System.Diagnostics.CodeAnalysis; // Sunrise-add
using Content.Shared.Roles; // Sunrise-add

namespace Content.Server.Delivery;

/// <summary>
/// System for managing deliveries spawned by the mail teleporter.
/// This covers for mail spawning, as well as granting cargo money.
/// </summary>
public sealed partial class DeliverySystem : SharedDeliverySystem
{
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly FingerprintReaderSystem _fingerprintReader = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    // Sunrise-start
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;

    /// <summary>
    /// Список с департаментами, которым нельзя доставлять посылки
    /// </summary>
    private List<string> _bannedDepartmentsRaw = new List<string>()
    {
        "Silicon",
    };

    // Будем использовать хэшсет для ускорения поиска
    private HashSet< ProtoId<JobPrototype>> _bannedRoles = new HashSet< ProtoId<JobPrototype>>();
    // Sunrise-end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeliveryComponent, MapInitEvent>(OnMapInit);

        InitializeSpawning();

        IndexBannedDepartments(); // Sunrise-add
    }

    // Sunrise-start
    /// <summary>
    /// Индексируем работы во всех департаментах, которым мы запретили отправлять посылки
    /// Вызывается 1 раз
    /// </summary>
    private void IndexBannedDepartments()
    {
        foreach (var bannedDepartment in _bannedDepartmentsRaw)
        {
            if (!_prototypeMan.TryIndex<DepartmentPrototype>(bannedDepartment, out var prototype))
                continue;
            foreach (var role in prototype.Roles)
            {
                _bannedRoles.Add(role);
            }
        }
    }

    /// <summary>
    /// Название говорит само за себя, но эта функция пытается найти валидную цель для доставки посылки
    /// </summary>
    /// <param name="stationId">Станция, по которой проводить поиск</param>
    /// <param name="record">Профиль найденной цели. Null если не найдена корректная цель</param>
    /// <returns></returns>
    private bool TryGetUnbannedRecord(EntityUid stationId, [NotNullWhen(true)] out GeneralStationRecord? record)
    {
        var attempts = 0;
        while (_records.TryGetRandomRecord(stationId, out record) &&
               record != null &&
               _bannedRoles.Contains(record.JobPrototype))
        {
            attempts++;
            if (attempts >= 10)
                return false;
        }

        return record != null;
    }
    // Sunrise-end

    private void OnMapInit(Entity<DeliveryComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.Container);

        var stationId = _station.GetStationInMap(Transform(ent).MapID);

        if (stationId == null)
            return;

        // Sunrise-start
        // _records.TryGetRandomRecord<GeneralStationRecord>(stationId.Value, out var entry);
        //
        // if (entry == null)
        //     return;
        if (!TryGetUnbannedRecord(stationId.Value, out var entry))
        // Sunrise-end
            return;

        ent.Comp.RecipientName = entry.Name;
        ent.Comp.RecipientJobTitle = entry.JobTitle;
        ent.Comp.RecipientStation = stationId.Value;

        _appearance.SetData(ent, DeliveryVisuals.JobIcon, entry.JobIcon);

        _label.Label(ent, ent.Comp.RecipientName);

        if (TryComp<FingerprintReaderComponent>(ent, out var reader) && entry.Fingerprint != null)
        {
            _fingerprintReader.AddAllowedFingerprint((ent.Owner, reader), entry.Fingerprint);
        }

        Dirty(ent);
    }

    protected override void GrantSpesoReward(Entity<DeliveryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!TryComp<StationBankAccountComponent>(ent.Comp.RecipientStation, out var account))
            return;

        _cargo.UpdateBankAccount(
            (ent.Comp.RecipientStation.Value, account),
            ent.Comp.SpesoReward,
            _cargo.CreateAccountDistribution((ent.Comp.RecipientStation.Value, account)));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateSpawner(frameTime);
    }
}
