using System.Linq;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.RoundEnd;
using Content.Server.Station.Components;
using Content.Server.Store.Systems;
using Content.Shared._Sunrise.FleshCult;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.FleshCult.GameRule;

public sealed class FleshCultRuleSystem : GameRuleSystem<FleshCultRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    [ValidatePrototypeId<AntagPrototype>]
    private const string LeaderAntagProto = "FleshCultistLeader";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FleshCultRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<FleshCultRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
        SubscribeLocalEvent<FleshCultRuleComponent, AntagSelectionCompleteEvent>(OnAfterAntagSelectionComplete);
        SubscribeLocalEvent<FleshCultSystem.FleshHeartStatusChangeEvent>(OnFleshHeartStatusChange);
    }

    protected override void Started(EntityUid uid,
        FleshCultRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        var eligible = new List<Entity<StationEventEligibleComponent, NpcFactionMemberComponent>>();
        var eligibleQuery = EntityQueryEnumerator<StationEventEligibleComponent, NpcFactionMemberComponent>();
        while (eligibleQuery.MoveNext(out var eligibleUid, out var eligibleComp, out var member))
        {
            if (!_npcFaction.IsFactionHostile(component.Faction, (eligibleUid, member)))
                continue;

            eligible.Add((eligibleUid, eligibleComp, member));
        }

        if (eligible.Count == 0)
            return;

        component.TargetStation = RobustRandom.Pick(eligible);
    }

    protected override void Ended(EntityUid uid, FleshCultRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        foreach (var componentFleshHeart in component.FleshHearts)
        {
            QueueDel(componentFleshHeart.Key);
        }

        foreach (var cultist in component.Cultists)
        {
            RemComp<FleshCultistComponent>(cultist);
        }
        QueueDel(uid);
    }

    private void OnObjectivesTextPrepend(EntityUid uid, FleshCultRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        if (!TryComp(comp.CultistsLeaderMind, out MindComponent? mind) && mind == null)
            return;
        _mindSystem.TryGetSession(comp.CultistsLeaderMind, out var session);
        args.Text += "\n" + Loc.GetString("flesh-cult-round-end-leader", ("name", mind.CharacterName)!, ("username", session!.Name));
    }

    private void OnFleshHeartStatusChange(FleshCultSystem.FleshHeartStatusChangeEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            switch (ev.Status)
            {
                case FleshHeartStatus.Base:
                {
                    fleshCult.FleshHearts.Add(ev.FleshHeartUid, FleshHeartStatus.Base);
                    break;
                }
                case FleshHeartStatus.Active:
                {
                    if (fleshCult.FleshHearts.ContainsKey(ev.FleshHeartUid))
                        fleshCult.FleshHearts[ev.FleshHeartUid] = FleshHeartStatus.Active;
                    break;
                }
                case FleshHeartStatus.Destruction:
                {
                    if (fleshCult.FleshHearts.ContainsKey(ev.FleshHeartUid))
                        fleshCult.FleshHearts[ev.FleshHeartUid] = FleshHeartStatus.Destruction;
                    break;
                }
                case FleshHeartStatus.Final:
                {
                    if (fleshCult.FleshHearts.ContainsKey(ev.FleshHeartUid))
                        fleshCult.FleshHearts[ev.FleshHeartUid] = FleshHeartStatus.Final;
                    _roundEndSystem.EndRound();
                    break;
                }
            }
        }
    }

    private void AfterEntitySelected(Entity<FleshCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, 15, ent.Comp);
    }

    private void OnAfterAntagSelectionComplete(Entity<FleshCultRuleComponent> ent, ref AntagSelectionCompleteEvent args)
    {
        var leader = GetLeader(args.GameRule);
        if (leader == null || !_mindSystem.TryGetMind(leader.Value, out var mindId, out var mind))
            return;
        ent.Comp.CultistsLeaderMind = mindId;
    }

    private EntityUid? GetLeader(Entity<AntagSelectionComponent> antagSelection)
    {
        EntityUid? leader = null;
        foreach (var compSelectedMind in antagSelection.Comp.SelectedMinds)
        {
            if (!TryComp<MindComponent>(compSelectedMind.Item1, out var mindComp))
                continue;

            foreach (var roleInfo in _roles.MindGetAllRoleInfo((compSelectedMind.Item1, mindComp)))
            {
                if (roleInfo.Prototype != LeaderAntagProto || mindComp.CurrentEntity == null)
                    continue;

                leader = mindComp.CurrentEntity.Value;
            }
        }

        return leader;
    }

    public FleshCultRuleComponent StartGameRule()
    {
        var comp = EntityQuery<FleshCultRuleComponent>().FirstOrDefault();
        if (comp == null)
        {
            GameTicker.StartGameRule("FleshCult", out var ruleEntity);
            comp = Comp<FleshCultRuleComponent>(ruleEntity);
        }

        return comp;
    }

    public bool MakeCultist(EntityUid fleshCultist, FixedPoint2 startingPoints, FleshCultRuleComponent fleshCultRule)
    {
        if (!_mindSystem.TryGetMind(fleshCultist, out var mindId, out var mind))
            return false;

        if (mind.OwnedEntity is not { } entity)
        {
            Logger.ErrorS("preset", "Mind picked for cultist did not have an attached entity.");
            return false;
        }

        SendCultistBriefing(mindId, fleshCultRule.CultistsNames);

        if (_mindSystem.TryGetSession(mindId, out var session))
        {
            _audioSystem.PlayGlobal(fleshCultRule.AddedSound, session);
        }

        _npcFaction.RemoveFaction(entity, "NanoTrasen", false);
        _npcFaction.AddFaction(entity, "FleshHuman");

        var fleshCultistComponent = EnsureComp<FleshCultistComponent>(mind.OwnedEntity.Value);

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {fleshCultistComponent.StolenCurrencyPrototype, startingPoints} }, mind.OwnedEntity.Value);

        fleshCultRule.Cultists.Add(fleshCultist);

        return true;
    }

    private void SendCultistBriefing(EntityUid mind, List<string> cultistsNames)
    {
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;
        _chatManager.DispatchServerMessage(session, Loc.GetString("flesh-cult-role-greeting"));
        _chatManager.DispatchServerMessage(session, Loc.GetString("flesh-cult-role-cult-members", ("cultMembers", string.Join(", ", cultistsNames))));
    }

    private void SendCultistLeaderBriefing(EntityUid mind, List<string> cultistsNames)
    {
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;
        _chatManager.DispatchServerMessage(session, Loc.GetString("flesh-cult-role-greeting-leader"));
        _chatManager.DispatchServerMessage(session, Loc.GetString("flesh-cult-role-cult-members", ("cultMembers", string.Join(", ", cultistsNames))));
    }

    protected override void AppendRoundEndText(EntityUid uid,
        FleshCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var result = Loc.GetString("flesh-cult-round-end-count-create-flesh-hearts", ("heartsCount",
            component.FleshHearts.Count));

        var destroyHearts = 0;
        var activateHearts = 0;

        foreach (var (heartUid, heartStatus) in component.FleshHearts)
        {
            switch (heartStatus)
            {
                case FleshHeartStatus.Destruction:
                    destroyHearts++;
                    break;
                case FleshHeartStatus.Active:
                    activateHearts++;
                    break;
            }
        }

        if (component.FleshHearts.Count > 0)
        {
            if (activateHearts > 0)
            {
                result +=  "\n" + Loc.GetString("flesh-cult-round-end-count-activate-flesh-hearts", ("heartsCount",
                    activateHearts));
            }
            else
            {
                result += "\n" + Loc.GetString("flesh-cult-round-end-count-no-activate-flesh-hearts");
            }

            if (destroyHearts > 0)
            {
                result +=  "\n" + Loc.GetString("flesh-cult-round-end-count-destroy-flesh-hearts", ("heartsCount",
                    destroyHearts));
            }
            else
            {
                result += "\n" + Loc.GetString("flesh-cult-round-end-count-no-destroy-flesh-hearts");
            }
        }

        var fleshHeartActive = false;
        foreach (var fleshCultFleshHeart in component.FleshHearts)
        {
            if (fleshCultFleshHeart.Value is FleshHeartStatus.Active or FleshHeartStatus.Final)
                fleshHeartActive = true;
        }

        if (fleshHeartActive)
        {
            result += "\n" + Loc.GetString("flesh-cult-round-end-flesh-heart-succes");
        }
        else
        {
            result += "\n" + Loc.GetString("flesh-cult-round-end-flesh-heart-fail");
        }

        args.AddLine("\n" + result);
    }
}
