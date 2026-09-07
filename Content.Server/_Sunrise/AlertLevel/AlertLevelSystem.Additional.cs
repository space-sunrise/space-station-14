using Content.Server._Sunrise.StationEvents.Events;

#pragma warning disable IDE0130 // Пространство имён соответствует расширяемой upstream-системе.
namespace Content.Server.AlertLevel;

public sealed partial class AlertLevelSystem
{
    /// <summary>
    /// Проверяет, можно ли изменить дополнительный код станции.
    /// </summary>
    public bool CanSetAdditionalLevel(
        Entity<AlertLevelComponent?> station,
        string level,
        bool enabled,
        bool force = false)
    {
        if (!Resolve(station, ref station.Comp)
            || station.Comp.AlertLevels == null
            || !station.Comp.AlertLevels.Levels.TryGetValue(level, out var detail)
            || !detail.IsAdditional
            || station.Comp.ActiveAdditionalLevels.Contains(level) == enabled)
        {
            return false;
        }

        if (force)
            return true;

        if (!station.Comp.AlertLevels.Levels.TryGetValue(station.Comp.CurrentLevel, out var currentDetail))
            return false;

        if (!currentDetail.Selectable || currentDetail.DisableSelection)
            return false;

        foreach (var additionalLevel in station.Comp.ActiveAdditionalLevels)
        {
            if (additionalLevel == level)
                continue;

            if (station.Comp.AlertLevels.Levels.TryGetValue(additionalLevel, out var additionalDetail)
                && additionalDetail.DisableSelection)
            {
                return false;
            }
        }

        return detail.Selectable
            && !detail.DisableSelection
            && !station.Comp.IsLevelLocked;
    }

    /// <summary>
    /// Пытается явно включить или выключить дополнительный код станции.
    /// </summary>
    public bool TrySetAdditionalLevel(
        EntityUid station,
        string level,
        bool enabled,
        bool playSound,
        bool announce,
        bool force = false,
        AlertLevelComponent? component = null)
    {
        var stationEntity = new Entity<AlertLevelComponent?>(station, component);
        if (!CanSetAdditionalLevel(stationEntity, level, enabled, force))
            return false;

        Resolve(stationEntity, ref stationEntity.Comp);
        DoSetAdditionalLevel((station, stationEntity.Comp!), level, enabled, playSound, announce);
        return true;
    }

    /// <summary>
    /// Возвращает, действует ли указанный основной или дополнительный код.
    /// </summary>
    public bool IsLevelActive(Entity<AlertLevelComponent?> station, string level)
    {
        if (!Resolve(station, ref station.Comp))
            return false;

        return station.Comp.CurrentLevel == level || station.Comp.ActiveAdditionalLevels.Contains(level);
    }

    /// <summary>
    /// Возвращает основной код и все активные дополнительные коды в порядке прототипа.
    /// </summary>
    public List<string> GetActiveLevels(Entity<AlertLevelComponent?> station)
    {
        var result = new List<string>();
        if (!Resolve(station, ref station.Comp) || station.Comp.AlertLevels == null)
            return result;

        result.Add(station.Comp.CurrentLevel);
        foreach (var level in station.Comp.AlertLevels.Levels.Keys)
        {
            if (station.Comp.ActiveAdditionalLevels.Contains(level))
                result.Add(level);
        }

        return result;
    }

    private void DoSetAdditionalLevel(
        Entity<AlertLevelComponent> station,
        string level,
        bool enabled,
        bool playSound,
        bool announce)
    {
        var detail = station.Comp.AlertLevels!.Levels[level];

        if (enabled)
            station.Comp.ActiveAdditionalLevels.Add(level);
        else
            station.Comp.ActiveAdditionalLevels.Remove(level);

        if (announce)
            AnnounceAdditionalLevel(station, level, detail, enabled, playSound);

        if (enabled)
            ApplySpecialAlertLevelBehavior(station, level, detail);

        RaiseLocalEvent(new AdditionalAlertLevelChangedEvent(station, level, enabled));
    }

    private void AnnounceAdditionalLevel(
        EntityUid station,
        string level,
        AlertLevelDetail detail,
        bool enabled,
        bool playSound)
    {
        var name = level.ToLowerInvariant();
        if (Loc.TryGetString($"alert-level-{level}", out var localizedName))
            name = localizedName.ToLowerInvariant();

        string announcement;
        if (enabled)
        {
            announcement = detail.Announcement;
            if (Loc.TryGetString(detail.Announcement, out var localizedAnnouncement))
                announcement = localizedAnnouncement;

            announcement = Loc.GetString("alert-level-announcement", ("name", name), ("announcement", announcement));
        }
        else
        {
            announcement = Loc.GetString("alert-level-additional-disabled-announcement", ("name", name));
        }

        _chatSystem.DispatchStationAnnouncement(
            station,
            announcement,
            announcementSound: enabled && playSound ? detail.Sound : null,
            playDefault: enabled && playSound && detail.Sound == null,
            colorOverride: detail.Color,
            sender: MetaData(station).EntityName);
    }

    private void ApplySpecialAlertLevelBehavior(EntityUid station, string level, AlertLevelDetail detail)
    {
        if (detail.ForceEndRound)
            _roundEnd.EndRound();

        if (level != EpsilonAlertLevel)
            return;

        var eventEnt = _gameTicker.AddGameRule(EpsilonBorgLawChanges);
        var epsilonRule = EntityManager.System<EpsilonDeathSquadLawsetRule>();
        epsilonRule.StartEvent(eventEnt, station);
        _gameTicker.StartGameRule(eventEnt);
    }

    private static void PruneAdditionalLevels(Entity<AlertLevelComponent> station)
    {
        station.Comp.ActiveAdditionalLevels.RemoveWhere(level =>
            !station.Comp.AlertLevels!.Levels.TryGetValue(level, out var detail) || !detail.IsAdditional);
    }
}

/// <summary>
/// Вызывается после включения или выключения дополнительного кода станции.
/// </summary>
public sealed class AdditionalAlertLevelChangedEvent : EntityEventArgs
{
    public EntityUid Station { get; }
    public string AlertLevel { get; }
    public bool Enabled { get; }

    public AdditionalAlertLevelChangedEvent(EntityUid station, string alertLevel, bool enabled)
    {
        Station = station;
        AlertLevel = alertLevel;
        Enabled = enabled;
    }
}
