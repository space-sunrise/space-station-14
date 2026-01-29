using System.Linq;
using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Shared._Sunrise.CarpQueen;
using Content.Shared.Humanoid;
using Content.Shared.Pointing;
using Content.Shared.Random.Helpers;
using Content.Shared.Dataset;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.RatKing;
using Content.Shared._Sunrise.TTS;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Chat;

namespace Content.Server._Sunrise.CarpQueen;

public sealed class CarpQueenSystem : SharedCarpQueenSystem
{
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly CarpEggSystem _carpEggs = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CarpQueenComponent, CarpQueenSummonActionEvent>(OnSummon);
        SubscribeLocalEvent<CarpQueenComponent, AfterPointedAtEvent>(OnPointedAt);
        SubscribeLocalEvent<CarpQueenServantComponent, ComponentShutdown>(OnServantShutdown);
    }

    protected override void OnStartup(EntityUid uid, CarpQueenComponent component, ComponentStartup args)
    {
        base.OnStartup(uid, component, args);

        if (TryComp<HungerComponent>(uid, out var hunger))
            component.LastObservedHunger = _hunger.GetHunger(hunger);

        if (TryComp<TTSComponent>(uid, out var ttsComponent))
        {
            TryAssignRandomFemaleVoiceToQueen(ttsComponent);
        }
    }

    private void OnSummon(EntityUid uid, CarpQueenComponent component, CarpQueenSummonActionEvent args)
    {
        if (args.Handled)
            return;

        if (component.ArmyMobSpawnOptions.Count == 0)
            return;

        var toRemoveServants = new List<EntityUid>();
        var aliveServants = 0;
        foreach (var s in component.Servants)
        {
            if (!Exists(s))
            {
                toRemoveServants.Add(s);
                continue;
            }
            if (TryComp<MobStateComponent>(s, out var mobState) && mobState.CurrentState == MobState.Dead)
                continue;
            aliveServants++;
        }
        foreach (var rem in toRemoveServants)
            component.Servants.Remove(rem);

        var toRemoveEggs = new List<EntityUid>();
        foreach (var e in component.Eggs)
        {
            if (!Exists(e))
                toRemoveEggs.Add(e);
        }
        foreach (var rem in toRemoveEggs)
            component.Eggs.Remove(rem);

        var eggsCount = component.Eggs.Count;
        if (aliveServants + eggsCount >= component.MaxArmySize)
        {
            _popup.PopupEntity(Loc.GetString("carp-queen-max-army", ("amount", component.MaxArmySize)), uid, uid);
            return;
        }

        if (!TryComp<HungerComponent>(uid, out var hungerComp))
            return;

        if (_hunger.GetHunger(hungerComp) < component.HungerPerSummon)
        {
            _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
            return;
        }

        args.Handled = true;
        _hunger.ModifyHunger(uid, -component.HungerPerSummon, hungerComp);
        var egg = Spawn("MobCarpEgg", Transform(uid).Coordinates);
        var eggComp = EnsureComp<CarpEggComponent>(egg);
        eggComp.Queen = uid;
        eggComp.RequiredVolume = component.EggRequiredVolume;
        eggComp.HatchDelay = component.EggHatchDelay;
        eggComp.MaxWaitWithoutLiquid = component.EggMaxWaitWithoutLiquid;
        eggComp.QueenSearchRange = component.EggQueenSearchRange;
        eggComp.FriendSearchRange = component.EggFriendSearchRange;
        eggComp.BiteReagentAmount = component.BiteReagentAmount;

        Dirty(egg, eggComp);
        component.Eggs.Add(egg);
        _carpEggs.RequestHatchCheck(egg);
        _popup.PopupEntity(Loc.GetString("carp-queen-summon-popup"), uid, uid);
    }

    private void OnServantShutdown(EntityUid uid, CarpQueenServantComponent servant, ComponentShutdown args)
    {
        if (servant.Queen == null || !TryComp(servant.Queen.Value, out CarpQueenComponent? queen))
            return;

        queen.Servants.Remove(uid);
    }

    private void OnPointedAt(EntityUid uid, CarpQueenComponent component, ref AfterPointedAtEvent args)
    {
        if (component.CurrentOrder != CarpQueenOrderType.Kill)
            return;

        var target = args.Pointed;
        if (!Exists(target))
            return;

        var valid = false;
        if (TryComp<MobStateComponent>(target, out var mobState))
            valid = mobState.CurrentState != MobState.Dead;
        else if (HasComp<NpcFactionMemberComponent>(target))
            valid = true;
        else if (HasComp<ActorComponent>(target))
            valid = true;

        if (!valid)
            return;

        foreach (var servant in component.Servants)
        {
            if (TerminatingOrDeleted(servant))
                continue;

            if (TryComp<CarpServantMemoryComponent>(servant, out var memory))
            {
                var exception = EnsureComp<FactionExceptionComponent>(servant);

                if (_npcFaction.IsIgnored((servant, exception), target))
                    _npcFaction.UnignoreEntity((servant, exception), target);

                if (memory.RememberedFriends.Remove(target))
                    Dirty(servant, memory);

                if (memory.ForbiddenTargets.Remove(target))
                    Dirty(servant, memory);
            }

            _npc.SetBlackboard(servant, NPCBlackboard.CurrentOrderedTarget, target);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CarpQueenComponent, HungerComponent>();
        while (query.MoveNext(out var uid, out var queen, out var hunger))
        {
            var current = _hunger.GetHunger(hunger);
            if (current > queen.LastObservedHunger)
            {
                var delta = current - queen.LastObservedHunger;
                var heal = MathF.Min(delta * queen.HealPerHunger, queen.MaxHealPerTick);
                if (heal > 0f)
                {
                    var spec = new DamageSpecifier();
                    spec.DamageDict["Blunt"] = -heal / 2f;
                    spec.DamageDict["Slash"] = -heal / 2f;
                    spec.DamageDict["Heat"] = 0f;
                    _damageable.TryChangeDamage(uid, spec, true, false);
                }
            }

            queen.LastObservedHunger = current;
        }
    }

    public override void UpdateServantNpc(EntityUid uid, CarpQueenOrderType orderType)
    {
        base.UpdateServantNpc(uid, orderType);

        if (!TryComp<CarpQueenServantComponent>(uid, out var servant) || servant.Queen == null || !Exists(servant.Queen.Value))
            return;

        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        _npc.SetBlackboard(uid, NPCBlackboard.FollowTarget, new EntityCoordinates(servant.Queen.Value, Vector2.Zero));

        var ratKingOrder = SharedCarpQueenSystem.ConvertToRatKingOrder(orderType);
        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, ratKingOrder);
        _npc.SetBlackboard(uid, "FollowCloseRange", 1.0f);
        _npc.SetBlackboard(uid, "FollowRange", 1.5f);
        _htn.Replan(htn);
    }


    public override void DoCommandCallout(EntityUid uid, CarpQueenComponent component)
    {
        base.DoCommandCallout(uid, component);

        if (!component.OrderCallouts.TryGetValue(component.CurrentOrder, out var datasetId) ||
            !PrototypeManager.TryIndex<LocalizedDatasetPrototype>(datasetId, out var datasetPrototype))
            return;

        var msg = Random.Pick(datasetPrototype);
        _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true);
    }

    private void TryAssignRandomFemaleVoiceToQueen(TTSComponent ttsComponent)
    {
        try
        {
            var availableFemaleVoices = GetAvailableFemaleVoices();
            if (availableFemaleVoices.Count == 0)
            {
                Log.Warning("No female TTS voices available for carp queen");
                AssignFallbackVoice(ttsComponent);
                return;
            }

            var selectedVoice = _random.Pick(availableFemaleVoices);
            ttsComponent.VoicePrototypeId = selectedVoice.ID;
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to assign TTS voice to carp queen: {ex.Message}");
            AssignFallbackVoice(ttsComponent);
        }
    }

    private List<TTSVoicePrototype> GetAvailableFemaleVoices()
    {
        var femaleVoices = new List<TTSVoicePrototype>();

        foreach (var voice in _prototypeManager.EnumeratePrototypes<TTSVoicePrototype>())
        {
            if (IsVoiceSuitableForQueen(voice))
            {
                femaleVoices.Add(voice);
            }
        }

        return femaleVoices;
    }

    private bool IsVoiceSuitableForQueen(TTSVoicePrototype voice)
    {
        return voice.Sex == Sex.Female && voice.RoundStart && !voice.SponsorOnly;
    }

    private void AssignFallbackVoice(TTSComponent ttsComponent)
    {
        ttsComponent.VoicePrototypeId = "Charlotte";
    }
}


