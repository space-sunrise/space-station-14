using Content.Server._Sunrise.Silicons.Bots.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Security.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Silicons.Bots.Systems;

public sealed class CleanBotSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CleanBotComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<CleanBotComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CleanBotComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnDamageChanged(Entity<CleanBotComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin is not { } origin || origin == ent.Owner || TerminatingOrDeleted(origin))
            return;

        if (!HasComp<MobStateComponent>(origin))
            return;

        ClearWantedTarget(ent);

        if (ent.Comp.RetaliationTarget is { } retaliationTarget && retaliationTarget != origin)
            _npcFaction.DeAggroEntity(ent.Owner, retaliationTarget);

        ent.Comp.RetaliationTarget = origin;
        ent.Comp.RetaliationEndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.RetaliationTime);
        ent.Comp.NextUpdateTime = TimeSpan.Zero;

        _npcFaction.AggroEntity(ent.Owner, origin);
        SetBatonMode(ent, true);
    }

    private void OnShutdown(Entity<CleanBotComponent> ent, ref ComponentShutdown args)
    {
        ClearWantedTarget(ent);
        ClearRetaliationTarget(ent);
        SetBatonMode(ent, false);
    }

    private void OnMeleeHit(Entity<CleanBotComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.BatonMode || !args.IsHit || args.HitEntities.Count == 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!HasComp<Content.Shared.Damage.Components.StaminaComponent>(target))
                continue;

            _stamina.TakeStaminaDamage(target,
                ent.Comp.RetaliationStaminaDamage,
                source: args.User,
                with: ent.Owner,
                sound: ent.Comp.RetaliationStaminaSound);
        }
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CleanBotComponent>();

        while (query.MoveNext(out var uid, out var cleanBot))
        {
            if (now < cleanBot.NextUpdateTime)
                continue;

            cleanBot.NextUpdateTime = now + TimeSpan.FromSeconds(cleanBot.UpdateInterval);
            RefreshState((uid, cleanBot));
        }
    }

    private void RefreshState(Entity<CleanBotComponent> ent)
    {
        if (RefreshRetaliation(ent))
            return;

        SetBatonMode(ent, false);
        RefreshWantedTarget(ent);
    }

    private bool RefreshRetaliation(Entity<CleanBotComponent> ent)
    {
        if (ent.Comp.RetaliationTarget is not { } target)
            return false;

        if (_timing.CurTime > ent.Comp.RetaliationEndTime || !IsTargetReachable(ent.Owner, target, ent.Comp.ChaseLoseRange))
        {
            ClearRetaliationTarget(ent);
            return false;
        }

        SetBatonMode(ent, ShouldUseBatonOnTarget(target));
        _npcFaction.AggroEntity(ent.Owner, target);
        return true;
    }

    private void RefreshWantedTarget(Entity<CleanBotComponent> ent)
    {
        if (ent.Comp.WantedTarget is { } target && IsWantedTarget(ent.Owner, target, ent.Comp, requireVision: false))
        {
            _npcFaction.AggroEntity(ent.Owner, target);
            return;
        }

        ClearWantedTarget(ent);

        var newTarget = FindWantedTarget(ent);
        if (newTarget == null)
            return;

        ent.Comp.WantedTarget = newTarget.Value;
        _npcFaction.AggroEntity(ent.Owner, newTarget.Value);
    }

    private EntityUid? FindWantedTarget(Entity<CleanBotComponent> ent)
    {
        var origin = Transform(ent.Owner).Coordinates;
        EntityUid? bestTarget = null;
        var bestDistance = float.MaxValue;

        foreach (var candidate in _lookup.GetEntitiesInRange<CriminalRecordComponent>(origin, ent.Comp.WantedVisionRange))
        {
            var target = candidate.Owner;
            if (!IsWantedTarget(ent.Owner, target, ent.Comp, requireVision: true))
                continue;

            if (!origin.TryDistance(EntityManager, Transform(target).Coordinates, out var distance) || distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestTarget = target;
        }

        return bestTarget;
    }

    private bool IsWantedTarget(EntityUid owner, EntityUid target, CleanBotComponent component, bool requireVision)
    {
        if (!TryComp<CriminalRecordComponent>(target, out var criminal) || criminal.StatusIcon != component.WantedStatusIcon)
            return false;

        if (requireVision && !_examine.InRangeUnOccluded(owner, target, component.WantedVisionRange))
            return false;

        if (!TryComp<CuffableComponent>(target, out var cuffable) || cuffable.Container.Count != 0)
            return false;

        return IsTargetReachable(owner, target, component.ChaseLoseRange);
    }

    private bool IsTargetReachable(EntityUid owner, EntityUid target, float maxDistance)
    {
        if (TerminatingOrDeleted(target) || !_mobState.IsAlive(target))
            return false;

        var ownerCoords = Transform(owner).Coordinates;
        var targetCoords = Transform(target).Coordinates;

        return ownerCoords.TryDistance(EntityManager, targetCoords, out var distance) && distance <= maxDistance;
    }

    private void ClearWantedTarget(Entity<CleanBotComponent> ent)
    {
        if (ent.Comp.WantedTarget is not { } target)
            return;

        _npcFaction.DeAggroEntity(ent.Owner, target);
        ent.Comp.WantedTarget = null;
    }

    private void ClearRetaliationTarget(Entity<CleanBotComponent> ent)
    {
        if (ent.Comp.RetaliationTarget is not { } target)
            return;

        _npcFaction.DeAggroEntity(ent.Owner, target);
        ent.Comp.RetaliationTarget = null;
        ent.Comp.RetaliationEndTime = TimeSpan.Zero;
    }

    private bool ShouldUseBatonOnTarget(EntityUid target)
    {
        if (!TryComp<StaminaComponent>(target, out var stamina) || !stamina.Critical)
            return true;

        return !TryComp<CuffableComponent>(target, out var cuffable) || cuffable.Container.Count != 0;
    }

    private void SetBatonMode(Entity<CleanBotComponent> ent, bool enabled)
    {
        if (ent.Comp.BatonMode == enabled)
            return;

        ent.Comp.BatonMode = enabled;

        if (TryComp<CleanBotDetainOnHitComponent>(ent, out var detain))
            detain.Enabled = !enabled;
    }
}
