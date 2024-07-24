using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared._Sunrise.StatsBoard;
using Content.Shared.Bed.Sleep;
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
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.StatsBoard;

public sealed class StatsBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private (EntityUid? killer, EntityUid? victim, TimeSpan time) _firstMurder = (null, null, TimeSpan.Zero);
    private EntityUid? _hamsterKiller;
    private int _jointCreated;
    private (EntityUid? clown, TimeSpan? time) _clownCuffed = (null, null);
    private readonly Dictionary<EntityUid, StatisticEntry> _statisticEntries = new();

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

    private void OnMindAdded(EntityUid uid, ActorComponent comp, MindAddedMessage ev)
    {
        if (_statisticEntries.ContainsKey(uid) || ev.Mind.Comp.Session == null || HasComp<GhostComponent>(uid))
            return;

        var value = new StatisticEntry(MetaData(uid).EntityName, ev.Mind.Comp.Session.UserId);
        _statisticEntries.Add(uid, value);
    }

    public void CleanEntries()
    {
        _firstMurder = (null, null, TimeSpan.Zero);
        _hamsterKiller = null;
        _jointCreated = 0;
        _clownCuffed = (null, TimeSpan.Zero);
        _statisticEntries.Clear();
    }

    private void OnAbsorbedPuddleEvent(EntityUid uid, ActorComponent comp, ref AbsorberPudleEvent ev)
    {
        if (!_mindSystem.TryGetMind(comp.PlayerSession, out var mindId, out var mind))
            return;

        if (_statisticEntries.TryGetValue(uid, out var value))
        {
            value.AbsorbedPuddleCount += 1;
        }
    }

    private void OnCraftedEvent(EntityUid uid, ActorComponent comp, ref ItemConstructionCreated ev)
    {
        if (!_mindSystem.TryGetMind(comp.PlayerSession, out var mindId, out var mind))
            return;

        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (!TryComp<MetaDataComponent>(ev.Item, out var metaDataComponent))
            return;
        if (metaDataComponent.EntityPrototype == null)
            return;
        switch (metaDataComponent.EntityPrototype.ID)
        {
            case "Blunt":
            case "Joint":
                _jointCreated += 1;
                break;
        }
    }

    private void OnCuffedEvent(EntityUid uid, ActorComponent comp, ref CuffedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        _statisticEntries[uid].CuffedCount += 1;
        if (_clownCuffed.clown != null)
            return;
        if (!HasComp<ClumsyComponent>(uid))
            return;
        _clownCuffed.clown = uid;
        _clownCuffed.time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
    }

    private void OnItemPurchasedEvent(EntityUid uid, ActorComponent comp, ref SubtractCashEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (ev.Currency != "Telecrystal")
            return;
        if (_statisticEntries[uid].SpentTk == null)
        {
            _statisticEntries[uid].SpentTk = ev.Cost.Int();
        }
        else
        {
            _statisticEntries[uid].SpentTk += ev.Cost.Int();
        }
    }

    private void OnElectrocuted(EntityUid uid, ActorComponent comp, ElectrocutedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        _statisticEntries[uid].ElectrocutedCount += 1;
    }

    private void OnDoorEmagged(EntityUid uid, ActorComponent comp, ref DoorEmaggedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        _statisticEntries[uid].DoorEmagedCount += 1;
    }

    private void OnInteractionAttempt(EntityUid uid, ActorComponent comp, InteractionAttemptEvent args)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (!HasComp<ItemComponent>(args.Target))
            return;
        if (MetaData(args.Target.Value).EntityPrototype == null)
            return;
        var entityPrototype = MetaData(args.Target.Value).EntityPrototype;
        if (entityPrototype is not { ID: "CaptainIDCard" })
            return;
        if (_statisticEntries[uid].IsInteractedCaptainCard)
            return;
        _statisticEntries[uid].IsInteractedCaptainCard = true;
    }

    private void OnCreamedEvent(EntityUid uid, ActorComponent comp, ref CreamedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        _statisticEntries[uid].CreamedCount += 1;
    }

    private void OnMobStateChanged(EntityUid uid, ActorComponent comp, MobStateChangedEvent args)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        switch (args.NewMobState)
        {
            case MobState.Dead:
            {
                _statisticEntries[uid].DeadCount += 1;

                EntityUid? origin = null;
                if (args.Origin != null)
                {
                    origin = args.Origin.Value;
                }

                if (_firstMurder.victim == null && HasComp<HumanoidAppearanceComponent>(uid))
                {
                    _firstMurder.victim = uid;
                    _firstMurder.killer = origin;
                    _firstMurder.time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                    Logger.Info($"First Murder. CurTime: {_gameTiming.CurTime}, RoundStartTimeSpan: {_gameTicker.RoundStartTimeSpan}, Substract: {_gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan)}");
                }

                if (origin != null)
                {
                    if (_hamsterKiller == null && _tagSystem.HasTag(uid, "Hamster"))
                    {
                        _hamsterKiller = origin.Value;
                    }

                    if (_tagSystem.HasTag(uid, "Mouse"))
                    {
                        _statisticEntries[origin.Value].KilledMouseCount += 1;
                    }

                    if (HasComp<HumanoidAppearanceComponent>(uid))
                        _statisticEntries[origin.Value].HumanoidKillCount += 1;
                }

                break;
            }
        }
    }

    private void OnDamageModify(EntityUid uid, ActorComponent comp, DamageChangedEvent ev)
    {
        DamageGetModify(uid, ev);

        if (ev.Origin != null)
            DamageTakeModify(ev.Origin.Value, ev);
    }

    private void DamageTakeModify(EntityUid uid, DamageChangedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (ev.DamageDelta == null)
            return;

        if (ev.DamageIncreased)
        {
            value.TotalInflictedDamage += ev.DamageDelta.GetTotal().Int();
        }
        else
        {
            value.TotalInflictedHeal += Math.Abs(ev.DamageDelta.GetTotal().Int());
        }
    }

    private void DamageGetModify(EntityUid uid, DamageChangedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (ev.DamageDelta == null)
            return;

        if (ev.DamageIncreased)
        {
            value.TotalTakeDamage += ev.DamageDelta.GetTotal().Int();
        }
        else
        {
            value.TotalTakeHeal += Math.Abs(ev.DamageDelta.GetTotal().Int());
        }
    }

    private void OnSlippedEvent(EntityUid uid, ActorComponent comp, ref SlippedEvent ev)
    {
        if (!_statisticEntries.TryGetValue(uid, out var value))
            return;

        if (HasComp<HumanoidAppearanceComponent>(uid))
            _statisticEntries[uid].SlippedCount += 1;
    }

    private StationBankAccountComponent? GetBankAccount(EntityUid? uid)
    {
        if (uid != null && TryComp<StationBankAccountComponent>(uid, out var bankAccount))
        {
            return bankAccount;
        }
        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var statsQuery = EntityQueryEnumerator<ActorComponent>();
        while (statsQuery.MoveNext(out var ent, out var comp))
        {
            if (!_statisticEntries.TryGetValue(ent, out var value))
                return;

            if (TryComp<TransformComponent>(ent, out var transformComponent) &&
                transformComponent.GridUid == null && HasComp<HumanoidAppearanceComponent>(ent))
                _statisticEntries[ent].SpaceTime += TimeSpan.FromSeconds(frameTime);

            if (TryComp<CuffableComponent>(ent, out var cuffableComponent) &&
                !cuffableComponent.CanStillInteract)
                _statisticEntries[ent].CuffedTime += TimeSpan.FromSeconds(frameTime);

            if (HasComp<SleepingComponent>(ent))
                _statisticEntries[ent].SleepTime += TimeSpan.FromSeconds(frameTime);
        }
    }

    public StatisticEntry[] GetStatisticEntries()
    {
        return _statisticEntries.Values.ToArray();
    }

    public string GetRoundStats()
    {
        var result = "";
        var totalSlipped = 0;
        var totalCreampied = 0;
        var totalDamage = 0;
        var totalHeal = 0;
        var totalDoorEmaged = 0;
        var maxSlippedCount = 0;
        var maxDeadCount = 0;
        var maxSpeciesCount = 0;
        var maxDoorEmagedCount = 0;
        var totalKilledMice = 0;
        var totalAbsorbedPuddle = 0;
        var maxKillsMice = 0;
        var totalCaptainCardInteracted = 0;
        var totalElectrocutedCount = 0;
        var totalSleepTime = TimeSpan.Zero;
        var minSpentTk = int.MaxValue;
        var maxHumKillCount = 0;
        var totalCuffedCount = 0;
        var maxTakeDamage = 0;
        var maxInflictedHeal = 0;
        var maxInflictedDamage = 0;
        var maxPuddleAbsorb = 0;
        var maxCuffedTime = TimeSpan.Zero;
        var maxSpaceTime = TimeSpan.Zero;
        var maxSleepTime = TimeSpan.Zero;
        string? mostPopularSpecies = null;
        Dictionary<string, int> roundSpecies = new();
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

        foreach (var (uid, data) in _statisticEntries)
        {
            if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearanceComponent))
            {
                var speciesProto = _prototypeManager.Index<SpeciesPrototype>(humanoidAppearanceComponent.Species);

                if (roundSpecies.TryGetValue(speciesProto.Name, out var count))
                {
                    roundSpecies[speciesProto.Name] = count + 1;
                }
                else
                {
                    roundSpecies.Add(speciesProto.Name, 1);
                }
            }

            totalDoorEmaged += data.DoorEmagedCount;
            totalSlipped += data.SlippedCount;
            totalCreampied += data.CreamedCount;
            totalDamage += data.TotalTakeDamage;
            totalHeal += data.TotalTakeHeal;
            totalCuffedCount += data.CuffedCount;
            totalKilledMice += data.KilledMouseCount;
            totalSleepTime += data.SleepTime;
            totalAbsorbedPuddle += data.AbsorbedPuddleCount;
            totalElectrocutedCount += data.ElectrocutedCount;

            if (data.SlippedCount > maxSlippedCount)
            {
                maxSlippedCount = data.SlippedCount;
                mostSlippedCharacter = uid;
            }

            if (data.DoorEmagedCount > maxDoorEmagedCount)
            {
                maxDoorEmagedCount = data.DoorEmagedCount;
                mostDoorEmagedCharacter = uid;
            }

            if (data.DeadCount > maxDeadCount)
            {
                maxDeadCount = data.DeadCount;
                mostDeadCharacter = uid;
            }

            if (data.KilledMouseCount > maxKillsMice)
            {
                maxKillsMice = data.KilledMouseCount;
                mostKillsMiceCharacter = uid;
            }

            if (data.IsInteractedCaptainCard)
            {
                totalCaptainCardInteracted += 1;
            }

            if (data.SpentTk != null && data.SpentTk < minSpentTk)
            {
                minSpentTk = data.SpentTk.Value;
                playerWithMinSpentTk = uid;
            }

            if (data.HumanoidKillCount > maxHumKillCount)
            {
                maxHumKillCount = data.HumanoidKillCount;
                playerWithMaxHumKills = uid;
            }

            if (data.TotalTakeDamage > maxTakeDamage)
            {
                maxTakeDamage = data.TotalTakeDamage;
                playerWithMaxDamage = uid;
            }

            if (data.CuffedTime > maxCuffedTime)
            {
                maxCuffedTime = data.CuffedTime;
                playerWithLongestCuffedTime = uid;
            }

            if (data.SleepTime > maxSleepTime)
            {
                maxSleepTime = data.SleepTime;
                playerWithLongestSleepTime = uid;
            }

            if (data.SpaceTime > maxSpaceTime)
            {
                maxSpaceTime = data.SpaceTime;
                playerWithLongestSpaceTime = uid;
            }

            if (data.TotalInflictedHeal > maxInflictedHeal)
            {
                maxInflictedHeal = data.TotalInflictedHeal;
                playerWithMostInflictedHeal = uid;
            }

            if (data.TotalInflictedDamage > maxInflictedDamage)
            {
                maxInflictedDamage = data.TotalInflictedDamage;
                playerWithMostInflictedDamage = uid;
            }

            if (data.AbsorbedPuddleCount > maxPuddleAbsorb)
            {
                maxPuddleAbsorb = data.AbsorbedPuddleCount;
                playerWithMostPuddleAbsorb = uid;
            }
        }

        result += "На станции были представители таких рас:";
        foreach (var speciesEntry in roundSpecies)
        {
            var species = speciesEntry.Key;
            var count = speciesEntry.Value;

            if (count > maxSpeciesCount)
            {
                maxSpeciesCount = count;
                mostPopularSpecies = species;
            }

            result += $"\n[bold][color=white]{Loc.GetString(species)}[/color][/bold] в количестве [color=white]{count}[/color].";
        }

        if (mostPopularSpecies != null)
        {
            result += $"\nСамой распространённой расой стал [color=white]{Loc.GetString(mostPopularSpecies)}[/color].";
        }

        var station = _station.GetStations().FirstOrDefault();
        var bank = GetBankAccount(station);

        if (bank != null)
            result += $"\nПод конец смены баланс карго составил [color=white]{bank.Balance}[/color] кредитов.";

        if (_firstMurder.victim != null)
        {
            var victimUsername = TryGetUsername(_firstMurder.victim.Value);
            var victimName = TryGetName(_firstMurder.victim.Value,
                _statisticEntries[_firstMurder.victim.Value].Name);
            var victimUsernameColor = victimUsername != null ? $" ([color=gray]{victimUsername}[/color])" : "";
            result += $"\nПервая жертва станции - [color=white]{victimName}[/color]{victimUsernameColor}.";
            result += $"\nВремя смерти - [color=yellow]{_firstMurder.time.ToString("hh\\:mm\\:ss")}[/color].";
            if (_firstMurder.killer != null)
            {
                var killerUsername = TryGetUsername(_firstMurder.killer.Value);
                var killerName = TryGetName(_firstMurder.killer.Value,
                    _statisticEntries[_firstMurder.killer.Value].Name);
                var killerUsernameColor = killerUsername != null ? $" ([color=gray]{killerUsername}[/color])" : "";
                result +=
                    $"\nУбийца - [color=white]{killerName}[/color]{killerUsernameColor}.";
            }
            else
            {
                result += "\nСмерть наступила при неизвестных обстоятельствах.";
            }
        }

        if (totalSlipped >= 1)
        {
            result += $"\nИгроки в этой смене поскользнулись [color=white]{totalSlipped}[/color] раз.";
        }

        if (mostSlippedCharacter != null && maxSlippedCount > 1)
        {
            var username = TryGetUsername(mostSlippedCharacter.Value);
            var name = TryGetName(mostSlippedCharacter.Value,
                _statisticEntries[mostSlippedCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всех раз поскользнулся [color=white]{name}[/color]{usernameColor} - [color=white]{maxSlippedCount}[/color].";
        }

        if (totalCreampied >= 1)
        {
            result += $"\nВсего кремировано игроков: {totalCreampied}.";
        }

        if (mostDeadCharacter != null && maxDeadCount > 1)
        {
            var username = TryGetUsername(mostDeadCharacter.Value);
            var name = TryGetName(mostDeadCharacter.Value,
                _statisticEntries[mostDeadCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего раз умирал [color=white]{name}[/color]{usernameColor}, а именно [color=white]{maxDeadCount}[/color] раз.";
        }

        if (totalDoorEmaged >= 1)
        {
            result += $"\nШлюзы были емагнуты [color=white]{totalDoorEmaged}[/color] раз.";
        }

        if (mostDoorEmagedCharacter != null)
        {
            var username = TryGetUsername(mostDoorEmagedCharacter.Value);
            var name = TryGetName(mostDoorEmagedCharacter.Value,
                _statisticEntries[mostDoorEmagedCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего шлюзов емагнул - [color=white]{name}[/color]{usernameColor} - [color=white]{maxDoorEmagedCount}[/color] раз.";
        }

        if (_jointCreated >= 1)
        {
            result += $"\nБыло скручено [color=white]{_jointCreated}[/color] косяков.";
        }

        if (totalKilledMice >= 1)
        {
            result += $"\nБыло убито [color=white]{totalKilledMice}[/color] мышей.";
        }

        if (mostKillsMiceCharacter != null && maxKillsMice > 1)
        {
            var username = TryGetUsername(mostKillsMiceCharacter.Value);
            var name = TryGetName(mostKillsMiceCharacter.Value,
                _statisticEntries[mostKillsMiceCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += $"\n{name}[/color]{usernameColor} устроил геноцид, убив [color=white]{maxKillsMice}[/color] мышей.";
        }

        if (_hamsterKiller != null)
        {
            var username = TryGetUsername(_hamsterKiller.Value);
            var name = TryGetName(_hamsterKiller.Value,
                _statisticEntries[_hamsterKiller.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nУбийцей гамлета был [color=white]{name}[/color]{usernameColor}.";
        }

        if (totalCuffedCount >= 1)
        {
            result += $"\nИгроки были закованы [color=white]{totalCuffedCount}[/color] раз.";
        }

        if (playerWithLongestCuffedTime != null)
        {
            var username = TryGetUsername(playerWithLongestCuffedTime.Value);
            var name = TryGetName(playerWithLongestCuffedTime.Value,
                _statisticEntries[playerWithLongestCuffedTime.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего времени в наручниках провёл [color=white]{name}[/color]{usernameColor} - [color=yellow]{maxCuffedTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (totalSleepTime > TimeSpan.Zero)
        {
            result += $"\nОбщее время сна игроков составило [color=yellow]{totalSleepTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (playerWithLongestSleepTime != null)
        {
            var username = TryGetUsername(playerWithLongestSleepTime.Value);
            var name = TryGetName(playerWithLongestSleepTime.Value,
                _statisticEntries[playerWithLongestSleepTime.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += $"\nГлавной соней станции оказался [color=white]{name}[/color]{usernameColor}.";
            result += $"\nОн спал на протяжении [color=yellow]{maxSleepTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (playerWithLongestSpaceTime != null)
        {
            var username = TryGetUsername(playerWithLongestSpaceTime.Value);
            var name = TryGetName(playerWithLongestSpaceTime.Value,
                _statisticEntries[playerWithLongestSpaceTime.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего времени в космосе провел [color=white]{name}[/color]{usernameColor} - [color=yellow]{maxSpaceTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (_clownCuffed.clown != null && _clownCuffed.time != null)
        {
            var username = TryGetUsername(_clownCuffed.clown.Value);
            var name = TryGetName(_clownCuffed.clown.Value,
                _statisticEntries[_clownCuffed.clown.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nКлоун [color=white]{name}[/color]{usernameColor} был закован всего спустя [color=yellow]{_clownCuffed.time.Value.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (totalHeal >= 1)
        {
            result += $"\nВсего игроками было излечено [color=white]{totalHeal}[/color] урона.";
        }

        if (playerWithMostInflictedHeal != null)
        {
            var username = TryGetUsername(playerWithMostInflictedHeal.Value);
            var name = TryGetName(playerWithMostInflictedHeal.Value,
                _statisticEntries[playerWithMostInflictedHeal.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего урона игрокам вылечил [color=white]{name}[/color]{usernameColor} - [color=white]{maxInflictedHeal}[/color].";
        }

        if (totalDamage >= 1)
        {
            result += $"\nВсего игроками было получено [color=white]{totalDamage}[/color] урона.";
        }

        if (playerWithMostInflictedDamage != null)
        {
            var username = TryGetUsername(playerWithMostInflictedDamage.Value);
            var name = TryGetName(playerWithMostInflictedDamage.Value,
                _statisticEntries[playerWithMostInflictedDamage.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего урона нанес [color=white]{name}[/color]{usernameColor} - [color=white]{maxInflictedDamage}[/color].";
        }

        if (playerWithMinSpentTk != null)
        {
            var username = TryGetUsername(playerWithMinSpentTk.Value);
            var name = TryGetName(playerWithMinSpentTk.Value,
                _statisticEntries[playerWithMinSpentTk.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nМеньше всего телекристалов потратил [color=white]{name}[/color]{usernameColor} - [color=white]{minSpentTk}[/color]ТК.";
        }

        if (playerWithMaxHumKills != null && maxHumKillCount > 1)
        {
            var username = TryGetUsername(playerWithMaxHumKills.Value);
            var name = TryGetName(playerWithMaxHumKills.Value,
                _statisticEntries[playerWithMaxHumKills.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += $"\nНастоящим маньяком в этой смене был [color=white]{name}[/color]{usernameColor}.";
            result += $"\nОн убил [color=white]{maxHumKillCount}[/color] гуманоидов.";
        }

        if (playerWithMaxDamage != null)
        {
            var username = TryGetUsername(playerWithMaxDamage.Value);
            var name = TryGetName(playerWithMaxDamage.Value,
                _statisticEntries[playerWithMaxDamage.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего урона получил [color=white]{name}[/color]{usernameColor} - [color=white]{maxTakeDamage}[/color]. Вот бедняга.";
        }

        if (totalAbsorbedPuddle >= 1)
        {
            result += $"\nИгроками было убрано [color=white]{totalAbsorbedPuddle}[/color] луж.";
        }

        if (playerWithMostPuddleAbsorb != null && maxPuddleAbsorb > 1)
        {
            var username = TryGetUsername(playerWithMostPuddleAbsorb.Value);
            var name = TryGetName(playerWithMostPuddleAbsorb.Value,
                _statisticEntries[playerWithMostPuddleAbsorb.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего луж было убрано благодаря [color=white]{name}[/color]{usernameColor} - [color=white]{maxPuddleAbsorb}[/color].";
        }

        if (totalCaptainCardInteracted >= 1)
        {
            result += $"\nКарта капитана побывала у [color=white]{totalCaptainCardInteracted}[/color] игроков.";
        }

        if (totalElectrocutedCount >= 1)
        {
            result += $"\nИгроки были шокированы [color=white]{totalElectrocutedCount}[/color] раз.";
        }

        result += "\n";

        return result;
    }

    private string? TryGetUsername(EntityUid uid)
    {
        string? username = null;

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            username = mind.Session?.Name;
        }

        return username;
    }

    private string TryGetName(EntityUid uid, string savedName)
    {
        return TryComp<MetaDataComponent>(uid, out var metaDataComponent) ? metaDataComponent.EntityName : savedName;
    }
}
