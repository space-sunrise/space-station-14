using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Humanoid;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction;
using Content.Shared.Speech.Muting;
using Content.Shared._Sunrise.Antags.Abductor;
using Content.Shared._Sunrise.Medical.Surgery.Effects.Step;
using Content.Shared._Sunrise.Medical.Surgery.Events;
using Content.Shared._Sunrise.Medical.Surgery.Steps.Parts;
using Content.Shared._Sunrise.VentCraw;
using Content.Shared.CombatMode.Pacification;
using Robust.Shared.Prototypes;
using Content.Server.Speech.Components;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Medical.Surgery;
public sealed partial class OrganSystem : EntitySystem
{

    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganImplantationCompleted>(OnFunctionalOrganImplanted);
        SubscribeLocalEvent<FunctionalOrganComponent, SurgeryOrganExtracted>(OnFunctionalOrganExtracted);

        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganImplantationCompleted>(OnEyeImplanted);
        SubscribeLocalEvent<OrganEyesComponent, SurgeryOrganExtracted>(OnEyeExtracted);

        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganImplantationCompleted>(OnTongueImplanted);
        SubscribeLocalEvent<OrganTongueComponent, SurgeryOrganExtracted>(OnTongueExtracted);

        SubscribeLocalEvent<AbductorOrganComponent, SurgeryOrganImplantationCompleted>(OnAbductorOrganImplanted);
        SubscribeLocalEvent<AbductorOrganComponent, SurgeryOrganExtracted>(OnAbductorOrganExtracted);

        SubscribeLocalEvent<DamageableComponent, SurgeryOrganImplantationCompleted>(OnOrganImplanted);
        SubscribeLocalEvent<DamageableComponent, SurgeryOrganExtracted>(OnOrganExtracted);

        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganImplantationCompleted>(OnVisualizationImplanted);
        SubscribeLocalEvent<OrganVisualizationComponent, SurgeryOrganExtracted>(OnVisualizationExtracted);
    }

    //

    private void OnFunctionalOrganImplanted(Entity<FunctionalOrganComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
            if (!EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.AddComponent(args.Body, _compFactory.GetComponent(comp.Component.GetType()));
    }

    private void OnFunctionalOrganExtracted(Entity<FunctionalOrganComponent> ent, ref SurgeryOrganExtracted args)
    {
        foreach (var comp in (ent.Comp.Components ?? []).Values)
            if (EntityManager.HasComponent(args.Body, comp.Component.GetType()))
                EntityManager.RemoveComponent(args.Body, _compFactory.GetComponent(comp.Component.GetType()));
    }

    //

    private void OnOrganImplanted(Entity<DamageableComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, ent.Comp.Damage, true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }
    private void OnOrganExtracted(Entity<DamageableComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<OrganDamageComponent>(ent.Owner, out var damageRule)
         || damageRule.Damage is null
         || !TryComp<DamageableComponent>(args.Body, out var bodyDamageable)) return;

        var change = _damageableSystem.TryChangeDamage(args.Body, damageRule.Damage.Invert(), true, false, bodyDamageable);
        if (change is not null)
            _damageableSystem.TryChangeDamage(ent.Owner, change.Invert(), true, false, ent.Comp);
    }

    //

    private void OnAbductorOrganImplanted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {

        EnsureComp<AbductorVictimComponent>(args.Body, out var victim);
        victim.Organ = ent.Comp.Organ;

        if (ent.Comp.Organ == AbductorOrganType.Vent)
            AddComp<VentCrawlerComponent>(args.Body);

        if (ent.Comp.Organ == AbductorOrganType.Pacified)
            AddComp<PacifiedComponent>(args.Body);

        if (ent.Comp.Organ == AbductorOrganType.Liar)
        {
            EnsureComp<ReplacementAccentComponent>(args.Body, out var accent);
            accent.Accent = "liar";
        }

        if (ent.Comp.Organ == AbductorOrganType.Owo)
            victim.TransformationTime += _timing.CurTime;
    }
    private void OnAbductorOrganExtracted(Entity<AbductorOrganComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (TryComp<AbductorVictimComponent>(args.Body, out var victim))
            if (victim.Organ == ent.Comp.Organ)
                victim.Organ = AbductorOrganType.None;

        if (ent.Comp.Organ == AbductorOrganType.Vent)
            RemComp<VentCrawlerComponent>(args.Body);

        if (ent.Comp.Organ == AbductorOrganType.Pacified)
            RemComp<PacifiedComponent>(args.Body);

        if (ent.Comp.Organ == AbductorOrganType.Liar)
            RemComp<ReplacementAccentComponent>(args.Body);

        if (ent.Comp.Organ == AbductorOrganType.Owo)
        {
            RemComp<AbductorOwoTransformatedComponent>(args.Body);
            RemComp<OwOAccentComponent>(args.Body);
            _humanoid.RemoveMarking(args.Body, "CatEars");
            _humanoid.RemoveMarking(args.Body, "CatTail");
        }
    }

    //

    private void OnTongueImplanted(Entity<OrganTongueComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (HasComp<AbductorComponent>(args.Body) || !ent.Comp.IsMuted) return;
        RemComp<MutedComponent>(args.Body);
    }

    private void OnTongueExtracted(Entity<OrganTongueComponent> ent, ref SurgeryOrganExtracted args)
    {
        ent.Comp.IsMuted = HasComp<MutedComponent>(args.Body);
        AddComp<MutedComponent>(args.Body);
    }

    //

    private void OnEyeExtracted(Entity<OrganEyesComponent> ent, ref SurgeryOrganExtracted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable)) return;

        ent.Comp.EyeDamage = blindable.EyeDamage;
        ent.Comp.MinDamage = blindable.MinDamage;
        _blindable.UpdateIsBlind((args.Body, blindable));
    }
    private void OnEyeImplanted(Entity<OrganEyesComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        if (!TryComp<BlindableComponent>(args.Body, out var blindable)) return;

        _blindable.SetMinDamage((args.Body, blindable), ent.Comp.MinDamage ?? 0);
        _blindable.AdjustEyeDamage((args.Body, blindable), (ent.Comp.EyeDamage ?? 0) - blindable.MaxDamage);
    }

    //

    private void OnVisualizationExtracted(Entity<OrganVisualizationComponent> ent, ref SurgeryOrganExtracted args)
        => _humanoid.SetLayersVisibility(args.Body, [ent.Comp.Layer], false);
    private void OnVisualizationImplanted(Entity<OrganVisualizationComponent> ent, ref SurgeryOrganImplantationCompleted args)
    {
        _humanoid.SetLayersVisibility(args.Body, [ent.Comp.Layer], true);
        _humanoid.SetBaseLayerId(args.Body, ent.Comp.Layer, ent.Comp.Prototype);
    }
}
