using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Reagents.Convert;

/// <summary>
/// Конвертирует реагенты внутри сущности по клику на нее
/// </summary>
public sealed class ByClickReagentConvertSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ByClickReagentConvertComponent, AfterInteractEvent>(OnBibleInteract);
    }

    private void OnBibleInteract(Entity<ByClickReagentConvertComponent> ent, ref AfterInteractEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!IsPossible(ent, args.Target, args.User))
            return;

        if (!TryComp<ContainerManagerComponent>(args.Target, out var container))
            return;

        var solutions = container.Containers
            .Where(kvp => kvp.Key.StartsWith("solution@"))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var solutionKey in solutions)
        {
            if (!container.Containers.TryGetValue(solutionKey, out var liquidContainer))
                continue;

            if (!TryComp<SolutionComponent>(liquidContainer.ContainedEntities[0], out var solution))
                continue;

            foreach (var (reagentTypeObj, amount) in solution.Solution.Contents)
            {
                if (reagentTypeObj.Prototype != ent.Comp.Target)
                    continue;

                solution.Solution.RemoveReagent(reagentTypeObj.Prototype, amount);
                solution.Solution.AddReagent(ent.Comp.Result, amount);

                _popup.PopupClient(ent.Comp.PopupMessage, args.Target.Value, args.User);
                _audio.PlayPvs(ent.Comp.Sound, args.Target.Value);

                return;
            }
        }
    }

    /// <summary>
    /// Проверяет, возможна ли конвертация?
    /// </summary>
    /// <returns>Да/Нет</returns>
    private bool IsPossible(Entity<ByClickReagentConvertComponent> ent, EntityUid? target, EntityUid user)
    {
        if (!target.HasValue)
            return false;

        if (!_whitelist.CheckBoth(user, ent.Comp.BlacklistUser, ent.Comp.WhitelistUser))
            return false;

        if (!_whitelist.CheckBoth(target, ent.Comp.BlacklistTarget, ent.Comp.WhitelistTarget))
            return false;

        if (_useDelay.IsDelayed(ent.Owner))
            return false;

        return true;
    }
}
