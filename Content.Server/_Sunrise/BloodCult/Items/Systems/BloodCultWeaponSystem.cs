using Content.Server._Sunrise.BloodCult.Items.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

public sealed class BloodCultWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomachSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<BloodCultWeaponComponent, AfterInteractEvent>(OnInteractEvent);
        SubscribeLocalEvent<BloodBoltComponent, ProjectileHitEvent>(BloodBoltOnCollide);
    }

    private void BloodBoltOnCollide(Entity<BloodBoltComponent> ent, ref ProjectileHitEvent args)
    {
        if (HasComp<ConstructComponent>(args.Target))
        {
            _damageableSystem.TryChangeDamage(args.Target,
                ent.Comp.HealConstruct,
                origin: args.Shooter);
            return;
        }
        if (HasComp<BloodCultistComponent>(args.Target))
        {
            if (_solutionContainer.TryGetInjectableSolution(args.Target, out var injectableSolution, out _))
            {
                injectableSolution.Value.Comp.Solution.AddReagent(ent.Comp.UnholyProto, ent.Comp.UnholyVolume);
            }
            return;
        }
        if (HasComp<MobStateComponent>(args.Target))
        {
            _damageableSystem.TryChangeDamage(args.Target,
                ent.Comp.Damage,
                origin: args.Shooter);
        }
    }

    private void OnInteractEvent(EntityUid uid, BloodCultWeaponComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target == null)
            return;

        if (!HasComp<BloodCultistComponent>(args.User))
            return;

        if (!TryComp<BloodCultistComponent>(args.Target, out var cultistComponent))
            return;

        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;

        var convert = false;

        if (cultistComponent.HolyConvertToken != null)
        {
            cultistComponent.HolyConvertToken?.Cancel();
            cultistComponent.HolyConvertToken = null;
            convert = true;
        }

        if (_body.TryGetBodyOrganEntityComps<StomachComponent>((args.Target.Value, body), out var stomachs))
        {
            var highestAvailable = FixedPoint2.Zero;
            Solution? stomachSol = null;
            Entity<StomachComponent>? stomachToUse = null;
            foreach (var ent in stomachs)
            {
                var owner = ent.Owner;
                if (!_solutionContainer.ResolveSolution(owner, StomachSystem.DefaultSolutionName, ref ent.Comp1.Solution, out stomachSol))
                    continue;

                if (stomachSol.AvailableVolume <= highestAvailable)
                    continue;

                stomachToUse = ent;
                highestAvailable = stomachSol.AvailableVolume;
            }

            if (stomachToUse == null || stomachSol == null)
                return;

            if (_stomachSystem.TryChangeReagent(stomachToUse.Value.Owner,
                    component.ConvertedId,
                    component.ConvertedToId))
                convert = true;
        }

        if (_solutionContainer.TryGetInjectableSolution(args.Target.Value, out var injectableSolution, out _))
        {
            if (ConvertHolyWater(injectableSolution.Value.Comp.Solution, component.ConvertedId, component.ConvertedToId))
                convert = true;
        }

        if (!convert)
            return;

        _audio.PlayPvs(component.ConvertHolyWaterSound, args.Target.Value);
        _popup.PopupEntity(Loc.GetString("holy-water-deconverted"), args.User, args.User);
    }

    private bool ConvertHolyWater(Solution solution, string fromReagentId, string toReagentId)
    {
        foreach (var reagent in solution.Contents)
        {
            if (reagent.Reagent.Prototype != fromReagentId)
                continue;

            var amount = reagent.Quantity;

            solution.RemoveReagent(reagent.Reagent.Prototype, reagent.Quantity);
            solution.AddReagent(toReagentId, amount);

            return true;
        }

        return false;
    }

    private void OnMeleeHit(EntityUid uid, BloodCultWeaponComponent component, MeleeHitEvent args)
    {
        if (HasComp<BloodCultistComponent>(args.User))
            return;

        _stunSystem.TryAddParalyzeDuration(args.User, TimeSpan.FromSeconds(component.StuhTime));
        _damageableSystem.TryChangeDamage(args.User, component.Damage, origin: uid);
    }
}
