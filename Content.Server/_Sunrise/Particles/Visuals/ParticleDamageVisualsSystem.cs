using System.Numerics;
using Content.Server._Sunrise.Particles;
using Content.Shared._Sunrise.Particles;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.Materials;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Particles.Visuals;

/// <summary>
/// Converts configured damage and destruction events into spatial particle orchestras.
/// </summary>
public sealed class ParticleDamageVisualsSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string HeatDamage = "Heat";
    private const string ShockDamage = "Shock";

    private readonly Dictionary<string, ParticleMaterialReactionPrototype> _materialReactions = [];
    private readonly Dictionary<string, ParticleMaterialReactionPrototype> _modifierReactions = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParticleDamageVisualsComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ParticleDamageVisualsComponent, ParticleDamageImpactEvent>(OnExactImpact);
        SubscribeLocalEvent<ParticleDamageVisualsComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        RebuildMaterialReactions();
    }

    private void OnDamageChanged(Entity<ParticleDamageVisualsComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (args.Origin is not { } origin || TerminatingOrDeleted(origin))
            return;

        var coordinates = GetImpactCoordinates(ent, origin, out var movement);
        TrySpawnImpact(ent, args.Damageable, args.DamageDelta, origin, coordinates, movement);
    }

    private void OnExactImpact(
        Entity<ParticleDamageVisualsComponent> ent,
        ref ParticleDamageImpactEvent args)
    {
        if (TerminatingOrDeleted(args.Origin) ||
            !TryComp<DamageableComponent>(ent, out var damageable))
        {
            return;
        }

        var targetCoordinates = _transform.GetMapCoordinates(ent);
        var coordinates = new MapCoordinates(args.WorldPosition, targetCoordinates.MapId);
        TrySpawnImpact(ent, damageable, args.Damage, args.Origin, coordinates, args.Movement);
    }

    private void TrySpawnImpact(
        Entity<ParticleDamageVisualsComponent> ent,
        DamageableComponent damageable,
        DamageSpecifier damageSpecifier,
        EntityUid origin,
        MapCoordinates coordinates,
        Vector2 movement)
    {
        var damage = damageSpecifier.GetTotal().Float();
        if (!float.IsFinite(damage) || damage < ent.Comp.MinimumImpactDamage)
            return;

        if (_timing.CurTime < ent.Comp.NextImpactTime)
            return;

        if (!TryResolveImpactOrchestra(ent, damageable, damageSpecifier, out var orchestra))
            return;

        ent.Comp.NextImpactTime = _timing.CurTime + ent.Comp.ImpactCooldown;
        _orchestra.Send(
            orchestra,
            coordinates,
            ent,
            origin,
            movement,
            ent.Comp.ColorOverride,
            ent.Comp.ImpactIntensity);
    }

    private void OnDestruction(Entity<ParticleDamageVisualsComponent> ent, ref DestructionEventArgs args)
    {
        if (!TryResolveDestructionOrchestra(ent, out var orchestra))
            return;

        _orchestra.Send(
            orchestra,
            _transform.GetMapCoordinates(ent),
            ent,
            colorOverride: ent.Comp.ColorOverride,
            intensity: ent.Comp.DestructionIntensity);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ParticleMaterialReactionPrototype>())
            RebuildMaterialReactions();
    }

    private bool TryResolveImpactOrchestra(
        Entity<ParticleDamageVisualsComponent> ent,
        DamageableComponent damageable,
        DamageSpecifier damage,
        out ProtoId<ParticleOrchestraPrototype> orchestra)
    {
        if (ent.Comp.ImpactOrchestra is { } impactOverride)
        {
            orchestra = impactOverride;
            return true;
        }

        var reaction = ResolveMaterialReaction(ent, damageable);
        var resolved = GetImpactKind(damage) switch
        {
            ParticleImpactKind.Energy => reaction?.EnergyImpactOrchestra,
            ParticleImpactKind.Electrical => reaction?.ElectricalImpactOrchestra,
            _ => reaction?.KineticImpactOrchestra,
        };

        if (resolved is not { } materialOrchestra)
        {
            orchestra = default;
            return false;
        }

        orchestra = materialOrchestra;
        return true;
    }

    private bool TryResolveDestructionOrchestra(
        Entity<ParticleDamageVisualsComponent> ent,
        out ProtoId<ParticleOrchestraPrototype> orchestra)
    {
        if (ent.Comp.DestructionOrchestra is { } destructionOverride)
        {
            orchestra = destructionOverride;
            return true;
        }

        if (!ent.Comp.UseMaterialDestruction)
        {
            orchestra = default;
            return false;
        }

        TryComp<DamageableComponent>(ent, out var damageable);
        if (ResolveMaterialReaction(ent, damageable)?.DestructionOrchestra is not { } materialOrchestra)
        {
            orchestra = default;
            return false;
        }

        orchestra = materialOrchestra;
        return true;
    }

    private ParticleMaterialReactionPrototype? ResolveMaterialReaction(
        Entity<ParticleDamageVisualsComponent> ent,
        DamageableComponent? damageable)
    {
        if (ent.Comp.Material is { } explicitMaterial && _proto.TryIndex(explicitMaterial, out var explicitReaction))
            return explicitReaction;

        if (TryComp<PhysicalCompositionComponent>(ent, out var composition))
        {
            ParticleMaterialReactionPrototype? dominantReaction = null;
            var dominantAmount = 0;
            foreach (var (material, amount) in composition.MaterialComposition)
            {
                if (amount <= dominantAmount || !_materialReactions.TryGetValue(material, out var reaction))
                    continue;

                dominantReaction = reaction;
                dominantAmount = amount;
            }

            if (dominantReaction != null)
                return dominantReaction;
        }

        if (damageable?.DamageModifierSetId is { } modifier &&
            _modifierReactions.TryGetValue(modifier.Id, out var modifierReaction))
        {
            return modifierReaction;
        }

        return null;
    }

    private static ParticleImpactKind GetImpactKind(DamageSpecifier damage)
    {
        var heat = GetPositiveDamage(damage, HeatDamage);
        var shock = GetPositiveDamage(damage, ShockDamage);

        if (shock > heat && shock > 0f)
            return ParticleImpactKind.Electrical;

        return heat > 0f
            ? ParticleImpactKind.Energy
            : ParticleImpactKind.Kinetic;
    }

    private static float GetPositiveDamage(DamageSpecifier damage, string damageType)
    {
        return damage.DamageDict.TryGetValue(damageType, out var amount) && amount > 0
            ? amount.Float()
            : 0f;
    }

    private void RebuildMaterialReactions()
    {
        _materialReactions.Clear();
        _modifierReactions.Clear();

        foreach (var reaction in _proto.EnumeratePrototypes<ParticleMaterialReactionPrototype>())
        {
            foreach (var material in reaction.Materials)
            {
                if (!_materialReactions.TryAdd(material.Id, reaction))
                    Log.Error($"Particle material '{material}' is assigned by more than one reaction prototype");
            }

            foreach (var modifier in reaction.DamageModifiers)
            {
                if (!_modifierReactions.TryAdd(modifier.Id, reaction))
                    Log.Error($"Damage modifier '{modifier}' is assigned by more than one particle material reaction prototype");
            }
        }
    }

    private MapCoordinates GetImpactCoordinates(EntityUid target, EntityUid origin, out Vector2 movement)
    {
        var targetCoordinates = _transform.GetMapCoordinates(target);
        var originCoordinates = _transform.GetMapCoordinates(origin);
        movement = Vector2.Zero;

        if (targetCoordinates.MapId != originCoordinates.MapId)
            return targetCoordinates;

        if (_physics.TryGetNearest(target, origin, out var targetPoint, out _, out _))
            targetCoordinates = new MapCoordinates(targetPoint, targetCoordinates.MapId);

        movement = targetCoordinates.Position - originCoordinates.Position;
        return targetCoordinates;
    }

    private enum ParticleImpactKind : byte
    {
        Kinetic,
        Energy,
        Electrical,
    }
}
