using System.Linq;
using Content.Server.Antag;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Clumsy;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using CultMemberComponent = Content.Shared._Sunrise.BloodCult.Components.CultMemberComponent;

namespace Content.Server._Sunrise.BloodCult.GameRule;

public sealed class BloodCultRuleSystem : GameRuleSystem<BloodCultRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StorageSystem _storageSystem = default!;
    [Dependency] private readonly NpcFactionSystem _factionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("preset");

        SubscribeLocalEvent<CultNarsieSummoned>(OnNarsieSummon);

        SubscribeLocalEvent<BloodCultistComponent, ComponentInit>(OnCultistComponentInit);
        SubscribeLocalEvent<BloodCultistComponent, ComponentRemove>(OnCultistComponentRemoved);
        SubscribeLocalEvent<BloodCultistComponent, MobStateChangedEvent>(OnCultistsStateChanged);

        SubscribeLocalEvent<BloodCultRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<BloodCultRuleComponent, AntagSelectionCompleteEvent>(OnAfterAntagSelectionComplete);
    }

    protected override void Added(EntityUid uid, BloodCultRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
        //SetCodewords(component, args.RuleEntity);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        BloodCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        var winText = Loc.GetString($"cult-cond-{component.WinCondition.ToString().ToLower()}");
        args.AddLine(winText);

        args.AddLine(Loc.GetString("cultists-list-start"));

        var antags = _antagSelection.GetAntagIdentifiers(uid);

        foreach (var (_, sessionData, name) in antags)
        {
            var lising = Loc.GetString("cultists-list-name", ("name", name), ("user", sessionData.UserName));
            args.AddLine(lising);
        }
    }

    private void AfterEntitySelected(Entity<BloodCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        Log.Debug($"AfterAntagEntitySelected {ToPrettyString(ent)}");
        MakeCultist(args.EntityUid, ent.Comp);
    }

    private void OnCultistsStateChanged(EntityUid uid, BloodCultistComponent component, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
        {
            CheckRoundShouldEnd();
        }
    }

    public BloodCultRuleComponent? GetRule()
    {
        var rule = EntityQuery<BloodCultRuleComponent>().FirstOrDefault();
        return rule;
    }

    private void OnAfterAntagSelectionComplete(Entity<BloodCultRuleComponent> ent, ref AntagSelectionCompleteEvent args)
    {
        var selectedCultist = new List<EntityUid>();
        foreach (var selectedMind in args.GameRule.Comp.SelectedMinds)
        {
            selectedCultist.Add(selectedMind.Item1);
        }

        var potentialTargets = FindPotentialTargets(selectedCultist);

        var numTargets = MathHelper.Clamp(selectedCultist.Count / ent.Comp.TargetsPerPlayer, 1, ent.Comp.MaxTargets);

        var selectedVictims = new List<EntityUid>();

        for (var i = 0; i < numTargets && potentialTargets.Count > 0; i++)
        {
            var index = _random.Next(potentialTargets.Count);
            var selectedVictim = potentialTargets[index];
            potentialTargets.RemoveAt(index);
            selectedVictims.Add(selectedVictim.Mind!.Value);
        }

        ent.Comp.CultTargets.AddRange(selectedVictims);
    }

    public List<MindComponent> GetTargets()
    {
        var querry = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();

        var targetMinds = new List<MindComponent>();

        while (querry.MoveNext(out _, out var cultRuleComponent, out _))
        {
            foreach (var cultTarget in cultRuleComponent.CultTargets)
            {
                if (_mindSystem.TryGetMind(cultTarget, out var mindId, out var mind))
                    targetMinds.Add(mind);
            }
        }

        return targetMinds;
    }

    public bool CanSummonNarsie()
    {
        var querry = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();

        while (querry.MoveNext(out _, out var cultRuleComponent, out _))
        {
            var cultists = new List<EntityUid>();
            var cultisQuery = EntityQueryEnumerator<BloodCultistComponent>();
            while (cultisQuery.MoveNext(out var cultistUid, out _))
            {
                cultists.Add(cultistUid);
            }
            var constructs = new List<EntityUid>();
            var constructQuery = EntityQueryEnumerator<ConstructComponent>();
            while (constructQuery.MoveNext(out var constructUid, out _))
            {
                constructs.Add(constructUid);
            }
            var enoughCultists = cultists.Count + constructs.Count > cultRuleComponent.CultMembersForSummonGod;

            if (!enoughCultists)
            {
                return false;
            }

            var targetsKilled = true;

            var targets = GetTargets();
            foreach (var mindComponent in targets)
            {
                targetsKilled = _mindSystem.IsCharacterDeadIc(mindComponent);
            }

            if (targetsKilled)
                return true;
        }

        return false;
    }

    private void CheckRoundShouldEnd()
    {
        var querry = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();
        var aliveCultistsCount = 0;

        while (querry.MoveNext(out _, out var cultRuleComponent, out _))
        {
            var cultisQuery = EntityQueryEnumerator<BloodCultistComponent>();
            while (cultisQuery.MoveNext(out var cultistUid, out _))
            {
                if (!TryComp<MobStateComponent>(cultistUid, out var mobState))
                    continue;

                if (_mobStateSystem.IsAlive(cultistUid, mobState))
                {
                    aliveCultistsCount++;
                }
            }

            if (aliveCultistsCount != 0)
                continue;

            cultRuleComponent.WinCondition = CultWinCondition.CultFailure;
            _roundEndSystem.EndRound();
        }
    }

    private void OnCultistComponentInit(EntityUid uid, BloodCultistComponent component, ComponentInit args)
    {
        var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var ruleEnt, out var cultRuleComponent, out _))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEnt))
                continue;

            UpdateCultistsAppearance(cultRuleComponent);
        }
    }

    private void OnCultistComponentRemoved(EntityUid uid, BloodCultistComponent component, ComponentRemove args)
    {
        var query = EntityQueryEnumerator<BloodCultRuleComponent, GameRuleComponent>();

        while (query.MoveNext(out var ruleEnt, out var cultRuleComponent, out _))
        {
            if (!GameTicker.IsGameRuleAdded(ruleEnt))
                continue;

            RemoveCultistAppearance(uid);

            CheckRoundShouldEnd();
        }
    }

    private void RemoveCultistAppearance(EntityUid cultist)
    {
        if (TryComp<HumanoidAppearanceComponent>(cultist, out var appearanceComponent))
        {
            //Потому что я так сказал
            appearanceComponent.EyeColor = Color.White;
            Dirty(cultist, appearanceComponent);
        }

        RemComp<PentagramComponent>(cultist);
    }

    private void UpdateCultistsAppearance(BloodCultRuleComponent bloodCultRuleComponent)
    {
        var cultists = new List<EntityUid>();
        var cultisQuery = EntityQueryEnumerator<BloodCultistComponent>();
        while (cultisQuery.MoveNext(out var cultistUid, out _))
        {
            cultists.Add(cultistUid);
        }
        var constructs = new List<EntityUid>();
        var constructQuery = EntityQueryEnumerator<ConstructComponent>();
        while (constructQuery.MoveNext(out var constructUid, out _))
        {
            constructs.Add(constructUid);
        }

        var totalCultMembers = cultists.Count + constructs.Count;
        if (totalCultMembers < BloodCultRuleComponent.ReadEyeThreshold)
            return;

        foreach (var cultist in cultists)
        {
            if (TryComp<HumanoidAppearanceComponent>(cultist, out var appearanceComponent))
            {
                appearanceComponent.EyeColor = BloodCultRuleComponent.EyeColor;
                Dirty(cultist, appearanceComponent);
            }

            if (totalCultMembers < BloodCultRuleComponent.PentagramThreshold)
                return;

            EnsureComp<PentagramComponent>(cultist);
        }
    }

    private List<MindContainerComponent> FindPotentialTargets(List<EntityUid> exclude = null!)
    {
        var potentialTargets = new List<MindContainerComponent>();

        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent, ActorComponent>();
        while (query.MoveNext(out var uid, out var mind, out _, out var actor))
        {
            var entity = mind.Mind;

            if (entity == default)
                continue;

            if (exclude?.Contains(uid) is true)
            {
                continue;
            }

            potentialTargets.Add(mind);
        }

        return potentialTargets;
    }

    public bool MakeCultist(EntityUid cultist, BloodCultRuleComponent rule)
    {
        if (!_mindSystem.TryGetMind(cultist, out var mindId, out var mind))
            return false;

        EnsureComp<BloodCultistComponent>(cultist);

        if (HasComp<ClumsyComponent>(cultist))
            RemComp<ClumsyComponent>(cultist);

        EnsureComp<CultMemberComponent>(cultist);

        _tagSystem.AddTag(cultist, "Cultist");

        _factionSystem.RemoveFaction(cultist, "NanoTrasen", false);
        _factionSystem.AddFaction(cultist, "Cultist");

        if (_inventorySystem.TryGetSlotEntity(cultist, "back", out var backPack))
        {
            foreach (var itemPrototype in rule.StartingItems)
            {
                var itemEntity = Spawn(itemPrototype, Transform(cultist).Coordinates);

                if (backPack != null)
                {
                    _storageSystem.Insert(backPack.Value, itemEntity, out _);
                }
            }
        }

        _audioSystem.PlayGlobal(rule.GreatingsSound, Filter.Empty().AddPlayer(mind.Session!), false,
            AudioParams.Default);

        _chatManager.DispatchServerMessage(mind.Session!, Loc.GetString("cult-role-greeting"));

        _mindSystem.TryAddObjective(mindId, mind, "CultistKillObjective");

        return true;
    }

    private void OnNarsieSummon(CultNarsieSummoned ev)
    {
        var query = EntityQuery<MobStateComponent, MindContainerComponent, BloodCultistComponent>().ToList();

        foreach (var (mobState, mindContainer, _) in query)
        {
            if (!mindContainer.HasMind || mindContainer.Mind is null)
            {
                continue;
            }

            var reaper = Spawn(BloodCultRuleComponent.ReaperPrototype, Transform(mobState.Owner).Coordinates);
            _mindSystem.TransferTo(mindContainer.Mind.Value, reaper);

            _bodySystem.GibBody(mobState.Owner);
        }

        _roundEndSystem.EndRound();
    }
}
