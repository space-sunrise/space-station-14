using Content.Shared.Humanoid;
using Content.Shared.DoAfter;
using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Popups;
using Content.Shared.Actions;
using Robust.Shared.Localization;

namespace Content.Shared._Sunrise.Medical.PsychologistSystem;

public sealed partial class PsychologistSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    [Dependency] private readonly SharedActionsSystem _actionsSystem  = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PsychologistBlockAlcoholComponent, ComponentStartup>(onPsychologistBlockAlcohol);

        SubscribeLocalEvent<HumanoidAppearanceComponent, AlcoholBlockEvent>(OnAlcoholBlockTry);
        SubscribeLocalEvent<HumanoidAppearanceComponent, DoAfterAlcoholBlockEvent>(DoAfterAlcoholBlock);

        SubscribeLocalEvent<SolutionIngestBlockerComponent, BeforeIngestedEvent>(OnDrink);
    }

    private void DoAfterAlcoholBlock(Entity<HumanoidAppearanceComponent> ent, ref DoAfterAlcoholBlockEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (args.Target != null)
        {
            if (CompOrNull<SolutionIngestBlockerComponent>(args.Target) != null)
            {
                _popupSystem.PopupEntity(Loc.GetString("psychologist-alcoholblock-removed", ("target", args.Target)), ent.Owner);
                RemComp<SolutionIngestBlockerComponent>(args.Target.Value);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("psychologist-alcoholblock-applied", ("target", args.Target)), ent.Owner);
                AddComp<SolutionIngestBlockerComponent>(args.Target.Value);
            }
        }
    }

    private void OnAlcoholBlockTry(Entity<HumanoidAppearanceComponent> ent, ref AlcoholBlockEvent args)
    {
        if (EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.Target, out var humanoidAppearanceComponent))
        {
            if (humanoidAppearanceComponent != null)
            {
                if (humanoidAppearanceComponent.Species.Id == "Dwarf")
                {
                    _popupSystem.PopupEntity(Loc.GetString("psychologist-alcoholblock-dwarf-forbidden"), ent.Owner);
                    return;
                }
            }
        }

        if (_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, ent.Owner, args.Action.Comp.UseDelay ?? TimeSpan.FromSeconds(30),
            new DoAfterAlcoholBlockEvent(), eventTarget: args.Target, target: args.Target, used: ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true
        }))
        {
            args.Handled = true;
        }
        else
        {
            args.Handled = false;
        }

    }
    private void OnDrink(Entity<SolutionIngestBlockerComponent> ent, ref BeforeIngestedEvent args)
    {
        if (args.Solution != null)
        {
            foreach (var cont in args.Solution.Contents)
            {
                if (cont.Reagent.ToString() == ent.Comp.ReagentForBlock)
                {
                    args.Cancelled = true;
                    return;
                }
                var reagent = _prototypeManager.Index<ReagentPrototype>($"{cont.Reagent}");

                if (reagent.Metabolisms != null)
                {
                    foreach (var metabolism in reagent.Metabolisms)
                    {
                        foreach (var effect in metabolism.Value.Effects)
                        {
                            if (effect is AdjustReagent adjust && adjust.Reagent != null && adjust.Reagent == ent.Comp.ReagentForBlock)
                            {
                                args.Cancelled = true;
                                return;
                            }
                        }
                    }
                }
                else
                {
                    return;
                }
            }
        }
        else
        {
            return;
        }
    }
    private void onPsychologistBlockAlcohol(Entity<PsychologistBlockAlcoholComponent> ent, ref ComponentStartup args)
    {
        _actionsSystem.AddAction(ent.Owner, "PsychologistAlcoholBlock");
    }

}
