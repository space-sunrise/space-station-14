using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Jobs;

/// <summary>
/// Серверная система для альтернативных названий должностей.
/// Читает выбранный альтернативный титул из профиля игрока и применяет его
/// к ID-карте и записи в манифесте экипажа при спавне.
/// </summary>
public sealed class AlternativeJobTitleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedIdCardSystem _card = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        // Обновляем запись в манифесте экипажа после создания
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnAfterGeneralRecordCreated);
        // Обновляем ID-карту после спавна
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    /// <summary>
    /// Возвращает локализованный альтернативный титул для должности из профиля,
    /// или null если титул не выбран или невалиден.
    /// </summary>
    private string? GetAlternativeTitle(HumanoidCharacterProfile profile, string jobId)
    {
        if (!profile.JobAlternativeTitles.TryGetValue(jobId, out var altTitleLocId))
            return null;

        if (!_prototype.TryIndex<JobPrototype>(jobId, out var jobProto))
            return null;

        if (!jobProto.AlternativeTitles.Contains(altTitleLocId))
            return null;

        return Loc.GetString(altTitleLocId);
    }

    private void OnAfterGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        if (string.IsNullOrEmpty(ev.Record.JobPrototype))
            return;

        var title = GetAlternativeTitle(ev.Profile, ev.Record.JobPrototype);
        if (title == null)
            return;

        ev.Record.JobTitle = title;
        _records.Synchronize(ev.Key);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null)
            return;

        var title = GetAlternativeTitle(ev.Profile, ev.JobId);
        if (title == null)
            return;

        // Находим ID-карту игрока
        if (!_inventory.TryGetSlotEntity(ev.Mob, "id", out var idUid))
            return;

        var cardId = idUid.Value;
        if (TryComp<PdaComponent>(idUid, out var pdaComponent) && pdaComponent.ContainedId != null)
            cardId = pdaComponent.ContainedId.Value;

        if (!TryComp<IdCardComponent>(cardId, out _))
            return;

        _card.TryChangeJobTitle(cardId, title);
    }
}
