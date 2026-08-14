using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared._Sunrise.StatsBoard;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cargo.Components;
using Content.Shared.Clumsy;
using Content.Shared.Construction;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Doors.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Fluids;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Localization;
using Content.Shared.Damage.Systems;
using Robust.Shared.Toolshed.Commands.Values;
using Robust.Shared.Utility;
using System.Text;

namespace Content.Server.StatsBoard;

public sealed class StatsBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private (EntityUid? killer, EntityUid? victim, TimeSpan time) _firstMurder = (null, null, TimeSpan.Zero);
    private EntityUid? _hamsterKiller;
    private int _jointCreated;
    private (EntityUid? clown, TimeSpan? time) _clownCuffed = (null, null);
    private readonly Dictionary<EntityUid, SharedStatisticEntry> _statisticEntries = new();
    private static readonly ProtoId<TagPrototype> HamsterTag = "Hamster";
    private static readonly ProtoId<TagPrototype> MouseTag = "Mouse";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActorComponent, DamageChangedEvent>(OnDamageModify);
        SubscribeLocalEvent<ActorComponent, SlippedEvent>(OnSlippedEvent);
        SubscribeLocalEvent<ActorComponent, CreamedEvent>(OnCreamedEvent);
        SubscribeLocalEvent<ActorComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<ActorComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ActorComponent, DoorEmaggedEvent>(OnDoorEmagged);
        SubscribeLocalEvent<ActorComponent, ElectrocutedEvent>(OnElectrocuted);
        SubscribeLocalEvent<ActorComponent, SubtractCashEvent>(OnItemPurchasedEvent);
        SubscribeLocalEvent<ActorComponent, CuffedEvent>(OnCuffedEvent);
        SubscribeLocalEvent<ActorComponent, ItemConstructionCreated>(OnCraftedEvent);
        SubscribeLocalEvent<ActorComponent, AbsorberPudleEvent>(OnAbsorbedPuddleEvent);
        SubscribeLocalEvent<ActorComponent, MindAddedMessage>(OnMindAdded);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var deltaTime = TimeSpan.FromSeconds(frameTime);
        var query = EntityQueryEnumerator<ActorComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!_statisticEntries.TryGetValue(uid, out var stats)) continue;

            if (TryComp(uid, out TransformComponent? xform) && xform.GridUid == null && HasComp<HumanoidAppearanceComponent>(uid))
                stats.SpaceTime += deltaTime;

            if (TryComp<CuffableComponent>(uid, out var cuffed) && !cuffed.CanStillInteract)
                stats.CuffedTime += deltaTime;

            if (HasComp<SleepingComponent>(uid))
                stats.SleepTime += deltaTime;
        }
    }
    public SharedStatisticEntry ConvertToSharedStatisticEntry(SharedStatisticEntry entry)
    {
        return new SharedStatisticEntry(entry.Name, entry.FirstActor)
        {
            TotalTakeDamage = entry.TotalTakeDamage,
            TotalTakeHeal = entry.TotalTakeHeal,
            TotalInflictedDamage = entry.TotalInflictedDamage,
            TotalInflictedHeal = entry.TotalInflictedHeal,
            SlippedCount = entry.SlippedCount,
            CreamedCount = entry.CreamedCount,
            DoorEmagedCount = entry.DoorEmagedCount,
            ElectrocutedCount = entry.ElectrocutedCount,
            CuffedCount = entry.CuffedCount,
            AbsorbedPuddleCount = entry.AbsorbedPuddleCount,
            SpentTk = entry.SpentTk,
            DeadCount = entry.DeadCount,
            HumanoidKillCount = entry.HumanoidKillCount,
            KilledMouseCount = entry.KilledMouseCount,
            CuffedTime = entry.CuffedTime,
            SpaceTime = entry.SpaceTime,
            SleepTime = entry.SleepTime,
            IsInteractedCaptainCard = entry.IsInteractedCaptainCard,
        };
    }
    private string FormatPlayerLine(string locId, EntityUid uid, params (string, object)[] extraArgs)
    {
        var username = TryGetUsername(uid);
        var name = TryGetName(uid);
        var usernameTag = username != null ? $" ([color=gray]{username}[/color])" : "";

        var allArgs = new (string, object)[2 + extraArgs.Length];
        allArgs[0] = ("name", name);
        allArgs[1] = ("username", usernameTag);

        for (var i = 0; i < extraArgs.Length; i++) allArgs[i + 2] = extraArgs[i];

        return Loc.GetString(locId, allArgs);
    }
    private string? TryGetUsername(EntityUid uid)
    {
        if (!_mindSystem.TryGetMind(uid, out _, out var mind)) return null;
        if (!_player.TryGetSessionById(mind.UserId, out var session)) return null;
        return session.Name;
    }
    private string TryGetName(EntityUid uid)
    {
        if (_statisticEntries.TryGetValue(uid, out var entry))
            return entry.Name;
        if (TryComp(uid, out MetaDataComponent? metaData))
            return metaData.EntityName;
        return Loc.GetString("statsentry-unknown-entity");
    }
    private StationBankAccountComponent? GetBankAccount(EntityUid? uid)
    {
        if (uid == null)
            return null;
        return TryComp<StationBankAccountComponent>(uid.Value, out var bankAccount) ? bankAccount : null;
    }
    public SharedStatisticEntry[] GetStatisticEntries()
    {
        return _statisticEntries.Values.ToArray();
    }
    public string GetRoundStats()
    {
        var totalSlipped = 0;
        var totalCreampied = 0;
        var totalDamage = 0;
        var totalHeal = 0;
        var totalDoorEmaged = 0;
        var maxSlippedCount = 0;
        var maxDeadCount = 0;
        var maxDoorEmagedCount = 0;
        var totalKilledMice = 0;
        var totalAbsorbedPuddle = 0;
        var maxKillsMice = 0;
        var totalCaptainCardInteracted = 0;
        var totalElectrocutedCount = 0;
        var minSpentTk = int.MaxValue;
        var maxHumKillCount = 0;
        var totalCuffedCount = 0;
        var maxTakeDamage = 0;
        var maxInflictedHeal = 0;
        var maxInflictedDamage = 0;
        var maxPuddleAbsorb = 0;

        EntityUid? mostSlippedCharacter = null;
        EntityUid? mostDeadCharacter = null;
        EntityUid? mostDoorEmagedCharacter = null;
        EntityUid? mostKillsMiceCharacter = null;
        EntityUid? playerWithMinSpentTk = null;
        EntityUid? playerWithMaxHumKills = null;
        EntityUid? playerWithMaxDamage = null;
        EntityUid? playerWithLongestCuffedTime = null;
        EntityUid? playerWithLongestSpaceTime = null;
        EntityUid? playerWithLongestSleepTime = null;
        EntityUid? playerWithMostInflictedHeal = null;
        EntityUid? playerWithMostInflictedDamage = null;
        EntityUid? playerWithMostPuddleAbsorb = null;

        var totalSleepTime = TimeSpan.Zero;
        var maxCuffedTime = TimeSpan.Zero;
        var maxSpaceTime = TimeSpan.Zero;
        var maxSleepTime = TimeSpan.Zero;

        var station = _station.GetStations().FirstOrDefault();
        var bank = GetBankAccount(station);

        var sb = new StringBuilder(4096);

        string? mostPopularSpecies = null;
        Dictionary<string, int> roundSpecies = new();

        foreach (var (uid, data) in _statisticEntries)
        {
            if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearanceComponent))
                continue;
            var speciesProto = _prototypeManager.Index<SpeciesPrototype>(humanoidAppearanceComponent.Species);
            var speciesName = _prototypeManager
                .Index<SpeciesPrototype>(humanoidAppearanceComponent.Species)
                .Name;
            roundSpecies[speciesName] = roundSpecies.GetValueOrDefault(speciesName) + 1;

            if (data.IsInteractedCaptainCard)
                totalCaptainCardInteracted += 1;
            if (data.SpentTk != null && data.SpentTk < minSpentTk)
                (minSpentTk, playerWithMinSpentTk) = (data.SpentTk.Value, uid);
            if (data.SlippedCount > maxSlippedCount)
                (maxSlippedCount, mostSlippedCharacter) = (data.SlippedCount, uid);
            if (data.DoorEmagedCount > maxDoorEmagedCount)
                (maxDoorEmagedCount, mostDoorEmagedCharacter) = (data.DoorEmagedCount, uid);
            if (data.DeadCount > maxDeadCount)
                (maxDeadCount, mostDeadCharacter) = (data.DeadCount, uid);
            if (data.KilledMouseCount > maxKillsMice)
                (maxKillsMice, mostKillsMiceCharacter) = (data.KilledMouseCount, uid);
            if (data.HumanoidKillCount > maxHumKillCount)
                (maxHumKillCount, playerWithMaxHumKills) = (data.HumanoidKillCount, uid);
            if (data.TotalTakeDamage > maxTakeDamage)
                (maxTakeDamage, playerWithMaxDamage) = (data.TotalTakeDamage, uid);
            if (data.CuffedTime > maxCuffedTime)
                (maxCuffedTime, playerWithLongestCuffedTime) = (data.CuffedTime, uid);
            if (data.SleepTime > maxSleepTime)
                (maxSpaceTime, playerWithLongestSleepTime) = (data.SleepTime, uid);
            if (data.SpaceTime > maxSpaceTime)
                (maxSpaceTime, playerWithLongestSpaceTime) = (data.SpaceTime, uid);
            if (data.TotalInflictedHeal > maxInflictedHeal)
                (maxInflictedHeal, playerWithMostInflictedHeal) = (data.TotalInflictedHeal, uid);
            if (data.TotalInflictedDamage > maxInflictedDamage)
                (maxInflictedDamage, playerWithMostInflictedDamage) = (data.TotalInflictedDamage, uid);
            if (data.AbsorbedPuddleCount > maxPuddleAbsorb)
                (maxPuddleAbsorb, playerWithMostPuddleAbsorb) = (data.AbsorbedPuddleCount, uid);
        }

        sb.AppendLine(Loc.GetString("statsentry-species-entry-name"));
        foreach (var (species, count) in roundSpecies)
            sb.AppendLine(Loc.GetString("statsentry-species-entry",
                ("name", Loc.GetString(species)),
                ("count", count)));

        if (mostPopularSpecies != null)
            sb.AppendLine(Loc.GetString("statsentry-mst-pop-species",
                ("name", Loc.GetString(mostPopularSpecies))));

        if (bank != null)
        {
            sb.AppendLine(Loc.GetString("statsentry-bank-balance-total",
            ("balance", bank.Accounts.Values.Sum())));

            foreach (var (account, balance) in bank.Accounts)
                sb.AppendLine(Loc.GetString("statsentry-bank-balance-account",
                    ("account", Loc.GetString(account)),
                    ("balance", balance)));
        }

        if (_firstMurder.victim != null)
        {
            sb.AppendLine(FormatPlayerLine("statsentry-firth-murder", _firstMurder.victim.Value));
            sb.AppendLine(Loc.GetString("statsentry-firth-murder-time",
                ("time", _firstMurder.time.ToString("hh\\:mm\\:ss"))));

            if (_firstMurder.killer != null)
                sb.AppendLine(FormatPlayerLine("statsentry-firth-murder-killer", _firstMurder.killer.Value));
            else
                sb.AppendLine(Loc.GetString("statsentry-firth-murder-killer-none"));
        }
        if (totalSlipped >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-slipped", ("count", totalSlipped)));

        if (mostSlippedCharacter != null && maxSlippedCount > 1)
            sb.AppendLine(FormatPlayerLine("statsentry-most-slipped", mostSlippedCharacter.Value, ("count", maxSlippedCount)));

        if (totalCreampied >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-creampied", ("total", totalCreampied)));

        if (mostDeadCharacter != null && maxDeadCount > 1)
            sb.AppendLine(FormatPlayerLine("statsentry-most-dead", mostDeadCharacter.Value, ("count", maxDeadCount)));

        if (totalDoorEmaged >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-door-emaged", ("count", totalDoorEmaged)));

        if (mostDoorEmagedCharacter != null)
            sb.AppendLine(FormatPlayerLine("statsentry-most-door-emaged-character", mostDoorEmagedCharacter.Value, ("count", maxDoorEmagedCount)));

        if (_jointCreated >= 1)
            sb.AppendLine(Loc.GetString("statsentry-joint-created", ("count", _jointCreated)));

        if (totalKilledMice >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-killed-mice", ("count", totalKilledMice)));

        if (mostKillsMiceCharacter != null && maxKillsMice > 1)
            sb.AppendLine(FormatPlayerLine("statsentry-most-kills-mice-character", mostKillsMiceCharacter.Value, ("count", maxKillsMice)));

        if (_hamsterKiller != null)
            sb.AppendLine(FormatPlayerLine("statsentry-hamster-killer", _hamsterKiller.Value));

        if (totalCuffedCount >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-cuffed-count", ("count", totalCuffedCount)));

        if (playerWithLongestCuffedTime != null)
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-longest-cuffed-time", playerWithLongestCuffedTime.Value, ("time", maxCuffedTime.ToString("hh\\:mm\\:ss"))));

        if (totalSleepTime > TimeSpan.Zero)
            sb.AppendLine(Loc.GetString("statsentry-total-sleep-time", ("time", totalSleepTime.ToString("hh\\:mm\\:ss"))));

        if (playerWithLongestSleepTime != null)
        {
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-longest-sleep-time", playerWithLongestSleepTime.Value));
            sb.AppendLine(Loc.GetString("statsentry-player-with-longest-sleep-time-time", ("time", maxSleepTime.ToString("hh\\:mm\\:ss"))));
        }

        if (playerWithLongestSpaceTime != null)
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-longest-space-time", playerWithLongestSpaceTime.Value, ("time", maxSpaceTime.ToString("hh\\:mm\\:ss"))));

        if (_clownCuffed.clown != null && _clownCuffed.time != null)
            sb.AppendLine(FormatPlayerLine("statsentry-clown-cuffed", _clownCuffed.clown.Value, ("time", _clownCuffed.time.Value.ToString("hh\\:mm\\:ss"))));

        if (totalHeal >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-heal", ("count", totalHeal)));

        if (playerWithMostInflictedHeal != null)
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-most-infected-heal", playerWithMostInflictedHeal.Value, ("count", maxInflictedHeal)));

        if (totalDamage >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-damage", ("count", totalDamage)));

        if (playerWithMostInflictedDamage != null)
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-most-infected-damage", playerWithMostInflictedDamage.Value, ("count", maxInflictedDamage)));

        if (playerWithMinSpentTk != null)
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-min-spent-tk", playerWithMinSpentTk.Value, ("count", minSpentTk)));

        if (playerWithMaxHumKills != null && maxHumKillCount > 1)
        {
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-max-hum-kills", playerWithMaxHumKills.Value));
            sb.AppendLine(Loc.GetString("statsentry-player-with-max-hum-kills-count", ("count", maxHumKillCount)));
        }

        if (playerWithMaxDamage != null)
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-max-damage", playerWithMaxDamage.Value, ("count", maxTakeDamage)));

        if (totalAbsorbedPuddle >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-absorbed-puddle", ("count", totalAbsorbedPuddle)));

        if (playerWithMostPuddleAbsorb != null && maxPuddleAbsorb > 1)
            sb.AppendLine(FormatPlayerLine("statsentry-player-with-most-puddle-absorb", playerWithMostPuddleAbsorb.Value, ("count", maxPuddleAbsorb)));

        if (totalCaptainCardInteracted >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-captain-card-interacted", ("count", totalCaptainCardInteracted)));

        if (totalElectrocutedCount >= 1)
            sb.AppendLine(Loc.GetString("statsentry-total-electrocuted-count", ("count", totalElectrocutedCount)));

        return sb.ToString();
    }
    public void CleanEntries()
    {
        _firstMurder = (null, null, TimeSpan.Zero);
        _hamsterKiller = null;
        _jointCreated = 0;
        _clownCuffed = (null, TimeSpan.Zero);
        _statisticEntries.Clear();
    }
    private void OnMindAdded(EntityUid uid, ActorComponent comp, MindAddedMessage ev)
    {
        if (ev.Mind.Comp.UserId is null || _statisticEntries.ContainsKey(uid) || HasComp<GhostComponent>(uid))
            return;

        _statisticEntries[uid] = new SharedStatisticEntry(MetaData(uid).EntityName, ev.Mind.Comp.UserId.Value);
    }
    private void OnAbsorbedPuddleEvent(EntityUid uid, ActorComponent comp, ref AbsorberPudleEvent ev)
    {
        if (_statisticEntries.TryGetValue(uid, out var value)) value.AbsorbedPuddleCount++;
    }
    private void OnCraftedEvent(EntityUid uid, ActorComponent comp, ref ItemConstructionCreated ev)
    {
        if (!_statisticEntries.ContainsKey(uid)) return;
        if (TryComp(ev.Item, out MetaDataComponent? meta) && meta.EntityPrototype?.ID is "Blunt" or "Joint") _jointCreated++;
    }

    private void OnCuffedEvent(EntityUid uid, ActorComponent comp, ref CuffedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value)) return;

        value.CuffedCount++;

        if (_clownCuffed.clown is null && HasComp<ClumsyComponent>(uid))
            _clownCuffed = (uid, _gameTiming.CurTime - _gameTicker.RoundStartTimeSpan);
    }

    private void OnItemPurchasedEvent(EntityUid uid, ActorComponent comp, ref SubtractCashEvent ev)
    {
        if (ev.Currency != "Telecrystal" || !_statisticEntries.TryGetValue(uid, out var value)) return;

        value.SpentTk = (value.SpentTk ?? 0) + ev.Cost.Int();
    }

    private void OnElectrocuted(EntityUid uid, ActorComponent comp, ElectrocutedEvent ev)
    {
        if (_statisticEntries.TryGetValue(uid, out var value))
            value.ElectrocutedCount++;
    }

    private void OnDoorEmagged(EntityUid uid, ActorComponent comp, ref DoorEmaggedEvent ev)
    {
        if (_statisticEntries.TryGetValue(uid, out var value))
            value.DoorEmagedCount++;
    }

    private void OnInteractionAttempt(EntityUid uid, ActorComponent comp, InteractionAttemptEvent args)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value)
            || value.IsInteractedCaptainCard || args.Target is not { } target)
            return;
        if (MetaData(args.Target.Value).EntityPrototype == null)
            return;
        if (HasComp<ItemComponent>(target) && MetaData(target).EntityPrototype?.ID == "CaptainIDCard")
            value.IsInteractedCaptainCard = true;
    }

    private void OnCreamedEvent(EntityUid uid, ActorComponent comp, ref CreamedEvent ev)
    {
        if (_statisticEntries.TryGetValue(uid, out var value))
            value.CreamedCount++;
    }

    private void OnMobStateChanged(EntityUid uid, ActorComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !_statisticEntries.TryGetValue(uid, out var value))
            return;

        value.DeadCount++;

        var origin = args.Origin;
        var isHumanoid = HasComp<HumanoidAppearanceComponent>(uid);

        if (_firstMurder.victim is null && isHumanoid)
        {
            var timeSinceRoundStart = _gameTiming.CurTime - _gameTicker.RoundStartTimeSpan;
            _firstMurder = (uid, origin, timeSinceRoundStart);
        }

        if (origin is null) return;

        if (_hamsterKiller is null && _tagSystem.HasTag(uid, HamsterTag))
            _hamsterKiller = origin.Value;

        if (_statisticEntries.TryGetValue(origin.Value, out var originEntry))
        {
            if (_tagSystem.HasTag(uid, MouseTag)) originEntry.KilledMouseCount++;

            if (isHumanoid) originEntry.HumanoidKillCount++;
        }
    }

    private void OnSlippedEvent(EntityUid uid, ActorComponent comp, ref SlippedEvent ev)
    {
        if (_statisticEntries.TryGetValue(uid, out var value) && HasComp<HumanoidAppearanceComponent>(uid))
            value.SlippedCount++;
    }

    private void OnDamageModify(EntityUid uid, ActorComponent comp, DamageChangedEvent ev)
    {
        ApplyDamageStats(uid, ev, isTaker: true);

        if (ev.Origin is { } origin)
            ApplyDamageStats(origin, ev, isTaker: false);
    }

    private void ApplyDamageStats(EntityUid uid, DamageChangedEvent ev, bool isTaker)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value) || ev.DamageDelta is null)
            return;

        var amount = ev.DamageDelta.GetTotal().Int();

        if (isTaker)
            if (ev.DamageIncreased) value.TotalTakeDamage += amount;
            else value.TotalTakeHeal += Math.Abs(amount);
        else
            if (ev.DamageIncreased) value.TotalInflictedDamage += amount;
            else value.TotalInflictedHeal += Math.Abs(amount);
    }
}
