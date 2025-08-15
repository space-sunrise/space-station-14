using System.Threading;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Pentagram;
using Content.Shared.EntityEffects;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Shared._Sunrise.BloodCult;

[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public sealed partial class DeconvertCultist : EntityEffect
{
    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-deconvert-cultist");
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var uid = args.TargetEntity;

        if (!args.EntityManager.TryGetComponent(uid, out BloodCultistComponent? component))
            return;

        if (component.HolyConvertToken != null)
            return;

        var random = new System.Random();
        var convert = random.Next(1, 101) <= component.HolyConvertChance;
        if (!convert)
            return;

        args.EntityManager.System<SharedStunSystem>()
            .TryAddParalyzeDuration(uid, TimeSpan.FromSeconds(5f));
        var target = Identity.Name(uid, args.EntityManager);
        args.EntityManager.System<SharedPopupSystem>()
            .PopupEntity(Loc.GetString("holy-water-started-converting", ("target", target)), uid);

        component.HolyConvertToken = new CancellationTokenSource();
        Timer.Spawn(TimeSpan.FromSeconds(component.HolyConvertTime),
            () => ConvertCultist(uid, args.EntityManager),
            component.HolyConvertToken.Token);
    }

    private void ConvertCultist(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<BloodCultistComponent>(uid, out var cultist))
            return;

        cultist.HolyConvertToken = null;
        entityManager.RemoveComponent<BloodCultistComponent>(uid);
        if (entityManager.HasComponent<SharedPentagramComponent>(uid))
            entityManager.RemoveComponent<SharedPentagramComponent>(uid);
        if (entityManager.HasComponent<CultMemberComponent>(uid))
            entityManager.RemoveComponent<CultMemberComponent>(uid);
        entityManager.System<TagSystem>().RemoveTag(uid, "Cultist");
    }
}
