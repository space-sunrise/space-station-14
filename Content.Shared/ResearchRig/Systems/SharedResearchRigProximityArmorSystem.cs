using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.ResearchRig.Components;
using Content.Shared.Armor;
using Content.Shared.Explosion.Components;
using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.ResearchRig.Systems;

/// <summary>
/// Handles proximity-based armor modification for research RIG suits
/// </summary>
public abstract class SharedResearchRigProximityArmorSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly InventorySystem Inventory = default!;
    [Dependency] protected readonly EntityLookupSystem EntityLookup = default!;
    [Dependency] protected readonly SharedTransformSystem Transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ResearchRigProximityArmorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ResearchRigProximityArmorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ResearchRigProximityArmorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnStartup(EntityUid uid, ResearchRigProximityArmorComponent component, ComponentStartup args)
    {
        // Set initial armor to base values
        UpdateArmor(uid, component, false);
    }

    private void OnEquipped(EntityUid uid, ResearchRigProximityArmorComponent component, ClothingGotEquippedEvent args)
    {
        // Start proximity checking when equipped
        component.TimeSinceLastCheck = 0f;
    }

    private void OnUnequipped(EntityUid uid, ResearchRigProximityArmorComponent component, ClothingGotUnequippedEvent args)
    {
        // Reset to base armor when unequipped
        UpdateArmor(uid, component, false);
        component.IsNearResearchEquipment = false;
        Dirty(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ResearchRigProximityArmorComponent, ClothingComponent>();
        while (query.MoveNext(out var uid, out var proximityComp, out var clothingComp))
        {
            if (clothingComp.InSlot == null)
                continue;

            proximityComp.TimeSinceLastCheck += frameTime;
            if (proximityComp.TimeSinceLastCheck < proximityComp.CheckInterval)
                continue;

            proximityComp.TimeSinceLastCheck = 0f;

            // Find the wearer
            var wearer = clothingComp.InSlot.ContainerSlot?.Owner;
            if (wearer == null)
                continue;

            CheckProximityAndUpdateArmor(uid, proximityComp, wearer.Value);
        }
    }

    protected virtual void CheckProximityAndUpdateArmor(EntityUid uid, ResearchRigProximityArmorComponent component, EntityUid wearer)
    {
        var wearerPos = Transform.GetMapCoordinates(wearer);
        var isNearEquipment = false;

        // Check for nearby research equipment
        var nearbyEntities = EntityLookup.GetEntitiesInRange(wearerPos, component.ProximityRange);
        foreach (var entity in nearbyEntities)
        {
            if (HasComponent<MetaDataComponent>(entity))
            {
                var prototype = MetaData(entity).EntityPrototype?.ID;
                if (prototype != null && component.ResearchEquipmentPrototypes.Contains(prototype))
                {
                    isNearEquipment = true;
                    break;
                }
            }
        }

        // Update armor if proximity status changed
        if (isNearEquipment != component.IsNearResearchEquipment)
        {
            component.IsNearResearchEquipment = isNearEquipment;
            UpdateArmor(uid, component, isNearEquipment);
            Dirty(uid, component);
        }
    }

    protected virtual void UpdateArmor(EntityUid uid, ResearchRigProximityArmorComponent component, bool enhanced)
    {
        // Update armor coefficients
        if (TryComp<ArmorComponent>(uid, out var armor))
        {
            var coefficients = enhanced ? component.EnhancedArmorCoefficients : component.BaseArmorCoefficients;
            armor.Modifiers.Coefficients.Clear();

            foreach (var (damageType, coefficient) in coefficients)
            {
                armor.Modifiers.Coefficients[damageType] = coefficient;
            }
            Dirty(uid, armor);
        }

        // Update explosion resistance
        if (TryComp<ExplosionResistanceComponent>(uid, out var explosionRes))
        {
            explosionRes.DamageCoefficient = enhanced ?
                component.EnhancedExplosionCoefficient :
                component.BaseExplosionCoefficient;
            Dirty(uid, explosionRes);
        }

        // Update pressure protection
        if (TryComp<PressureProtectionComponent>(uid, out var pressureComp))
        {
            pressureComp.HighPressureMultiplier = enhanced ?
                component.EnhancedHighPressureMultiplier :
                component.BaseHighPressureMultiplier;
            Dirty(uid, pressureComp);
        }
    }
}
