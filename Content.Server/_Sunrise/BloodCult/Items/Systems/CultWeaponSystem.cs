using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;
using CultWeaponComponent = Content.Shared._Sunrise.BloodCult.Items.CultWeaponComponent;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

public sealed class CultWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomachSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<CultWeaponComponent, AfterInteractEvent>(OnInteractEvent);
    }

    private void OnInteractEvent(EntityUid uid, CultWeaponComponent component, AfterInteractEvent args)
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
            var firstStomach = stomachs.FirstOrNull();

            if (firstStomach == null)
                return;

            if (!_solutionContainer.TryGetSolution(firstStomach.Value.Owner, firstStomach.Value.Comp1.BodySolutionName, out var bodySolution))
                return;

            if (_stomachSystem.TryChangeReagent(firstStomach.Value.Owner, component.ConvertedId, component.ConvertedToId))
                convert = true;

            if (ConvertHolyWater(bodySolution.Value.Comp.Solution, component.ConvertedId, component.ConvertedToId))
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

    private void OnMeleeHit(EntityUid uid, CultWeaponComponent component, MeleeHitEvent args)
    {
        if (HasComp<BloodCultistComponent>(args.User))
            return;

        _stunSystem.TryParalyze(args.User, TimeSpan.FromSeconds(component.StuhTime), true);
        _damageableSystem.TryChangeDamage(args.User, component.Damage, origin: uid);
    }
}
