// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Collections.Frozen;
using Content.Shared.Paper;
using Robust.Shared.Utility;
using static Content.Shared.Paper.PaperComponent;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Reagent;
namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// This handles <see cref="SolutionContainerMixerComponent"/>
/// </summary>
public abstract class SharedVaccinatorSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    /// <summary>
    /// A cache of all reactions indexed by at most ONE of their required reactants.
    /// I.e., even if a reaction has more than one reagent, it will only ever appear once in this dictionary.
    /// </summary>
    private FrozenDictionary<string, List<ReactionPrototype>> _reactionsSingle = default!;

    /// <summary>
    ///     A cache of all reactions indexed by one of their required reactants.
    /// </summary>
    private FrozenDictionary<string, List<ReactionPrototype>> _reactions = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeReactionCache();
        SubscribeLocalEvent<VaccinatorComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<VaccinatorComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);
    }


    /// <summary>
    ///     Handles building the reaction cache.
    /// </summary>
    private void InitializeReactionCache()
    {
        // Construct single-reaction dictionary.
        var dict = new Dictionary<string, List<ReactionPrototype>>();
        foreach (var reaction in _prototypeManager.EnumeratePrototypes<ReactionPrototype>())
        {
            // For this dictionary we only need to cache based on the first reagent.
            var reagent = reaction.Reactants.Keys.First();
            var list = dict.GetOrNew(reagent);
            list.Add(reaction);
        }
        _reactionsSingle = dict.ToFrozenDictionary();

        dict.Clear();
        foreach (var reaction in _prototypeManager.EnumeratePrototypes<ReactionPrototype>())
        {
            foreach (var reagent in reaction.Reactants.Keys)
            {
                var list = dict.GetOrNew(reagent);
                list.Add(reaction);
            }
        }
        _reactions = dict.ToFrozenDictionary();
    }

    private void OnActivateInWorld(Entity<VaccinatorComponent> entity, ref ActivateInWorldEvent args)
    {
        TryStartMix(entity, args.User);
    }

    private void OnRemoveAttempt(Entity<VaccinatorComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (args.Container.ID == ent.Comp.ContainerId && ent.Comp.Mixing)
            args.Cancel();
    }

    protected virtual bool HasPower(Entity<VaccinatorComponent> entity)
    {
        return true;
    }

    public void TryStartMix(Entity<VaccinatorComponent> entity, EntityUid? user)
    {
        var (uid, comp) = entity;
        if (comp.Mixing)
            return;

        if (!HasPower(entity))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-no-power"), entity, user.Value);
            return;
        }

        if (!_container.TryGetContainer(uid, comp.ContainerId, out var container) || container.Count == 0)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-popup-nothing-to-mix"), entity, user.Value);
            return;
        }

        comp.Mixing = true;
        try
        {
            if (_net.IsServer)
                comp.MixingSoundEntity = _audio.PlayPvs(comp.MixingSound, entity, comp.MixingSound?.Params.WithLoop(true));
        }
        catch { }
        comp.MixTimeEnd = _gameTiming.CurTime + comp.MixDuration;
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, true);
        Dirty(uid, comp);
    }

    public void StopMix(Entity<VaccinatorComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Mixing)
            return;
        _audio.Stop(comp.MixingSoundEntity);
        _appearance.SetData(entity, SolutionContainerMixerVisuals.Mixing, false);
        comp.Mixing = false;
        comp.MixingSoundEntity = null;
        Dirty(uid, comp);
    }

    public void FinishMix(Entity<VaccinatorComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Mixing)
            return;
        StopMix(entity);

        if (!TryComp<ReactionMixerComponent>(entity, out var reactionMixer)
            || !_container.TryGetContainer(uid, comp.ContainerId, out var container))
            return;
        bool printed = false;
        foreach (var ent in container.ContainedEntities)
        {
            if (!_solution.TryGetFitsInDispenser(ent, out var soln, out _))
                continue;
            if (!printed)
            {
                if (!(_gameTiming.CurTime < comp.PrintReadyAt))
                {
                    RaiseNetworkEvent(new PaperInputTextMessageLigyb(soln.Value.Comp.Solution.Contents, GetNetEntity(uid)));
                    printed = true;
                    comp.PrintReadyAt = _gameTiming.CurTime + comp.PrintCooldown;
                }
            }
            _solution.UpdateChemicals(soln.Value, true, reactionMixer);

        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VaccinatorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Mixing)
                continue;

            if (_gameTiming.CurTime < comp.MixTimeEnd)
                continue;

            FinishMix((uid, comp));
        }
    }
}



[Serializable, NetSerializable]
public sealed class PaperInputTextMessageLigyb : EntityEventArgs
{
    public readonly List<ReagentQuantity> ReagentQuantity;
    public readonly NetEntity Uid;
    public PaperInputTextMessageLigyb(List<ReagentQuantity> reagentQuantity, NetEntity uid)
    {
        Uid = uid;
        ReagentQuantity = reagentQuantity;
    }
}
