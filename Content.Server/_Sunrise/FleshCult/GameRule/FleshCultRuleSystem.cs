using System.Linq;
using Content.Server.Antag;
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
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.FleshCult.GameRule;

public sealed class FleshCultRuleSystem : GameRuleSystem<FleshCultRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FleshCultRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);

        SubscribeLocalEvent<FleshHeartSystem.FleshHeartActivateEvent>(OnFleshHeartActivate);
        SubscribeLocalEvent<FleshHeartSystem.FleshHeartDestructionEvent>(OnFleshHeartDestruction);
        SubscribeLocalEvent<FleshHeartSystem.FleshHeartFinalEvent>(OnFleshHeartFinal);

        SubscribeLocalEvent<FleshCultRuleComponent, ObjectivesTextPrependEvent>(OnObjectivesTextPrepend);
    }

    private void OnObjectivesTextPrepend(EntityUid uid, FleshCultRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        if (!TryComp(comp.CultistsLeaderMind, out MindComponent? mind) && mind == null)
            return;
        _mindSystem.TryGetSession(comp.CultistsLeaderMind, out var session);
        args.Text += "\n" + Loc.GetString("flesh-cult-round-end-leader", ("name", mind.CharacterName)!, ("username", session!.Name));
    }

    private void OnFleshHeartActivate(FleshHeartSystem.FleshHeartActivateEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
            {
                continue;
            }

            if (ev.OwningStation == null)
            {
                return;
            }

            if (fleshCult.TargetStation == null)
            {
                return;
            }

            if (!TryComp(fleshCult.TargetStation, out StationDataComponent? data))
            {
                return;
            }

            foreach (var grid in data.Grids)
            {
                if (grid != ev.OwningStation)
                {
                    continue;
                }

                fleshCult.FleshHearts.Add(ev.FleshHeardUid, FleshHeartStatus.Active);
                fleshCult.FleshHeartActive = true;
                return;
            }
        }
    }

    private void OnFleshHeartDestruction(FleshHeartSystem.FleshHeartDestructionEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
            {
                continue;
            }

            if (ev.OwningStation == null)
            {
                return;
            }

            if (fleshCult.TargetStation == null)
            {
                return;
            }

            if (!TryComp(fleshCult.TargetStation, out StationDataComponent? data))
            {
                return;
            }

            foreach (var grid in data.Grids)
            {
                if (grid != ev.OwningStation)
                {
                    continue;
                }

                if (fleshCult.FleshHearts.ContainsKey(ev.FleshHeardUid))
                {
                    fleshCult.FleshHearts[ev.FleshHeardUid] = FleshHeartStatus.Destruction;
                }
                fleshCult.FleshHeartActive = false;
                return;
            }
        }
    }

    private void OnFleshHeartFinal(FleshHeartSystem.FleshHeartFinalEvent ev)
    {
        var query = EntityQueryEnumerator<FleshCultRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var fleshCult, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
            {
                continue;
            }

            if (ev.OwningStation == null)
            {
                return;
            }

            if (fleshCult.TargetStation == null)
            {
                return;
            }

            if (!TryComp(fleshCult.TargetStation, out StationDataComponent? data))
            {
                return;
            }
            foreach (var grid in data.Grids)
            {
                if (grid != ev.OwningStation)
                {
                    continue;
                }

                fleshCult.WinType = FleshCultRuleComponent.WinTypes.FleshHeartFinal;
                _roundEndSystem.EndRound();
                return;
            }
        }
    }

    private void AfterEntitySelected(Entity<FleshCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        MakeCultist(args.EntityUid, 15, ent.Comp);
    }

    public void MakeCultistAdmin(EntityUid target, FixedPoint2 startingPoints)
    {
        var fleshCultRule = StartGameRule();
        MakeCultist(target, startingPoints, fleshCultRule);
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

        if (component.FleshHeartActive)
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
