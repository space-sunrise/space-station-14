using Content.Shared._Sunrise.Misc;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Content.Shared.Throwing;
using Content.Shared.Inventory.Events;
using Content.Shared.Hands;
using Content.Shared.Humanoid;

namespace Content.Server._Sunrise.Misc;

public sealed class XenoArtifactThrowingAutoInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoArtifactThrowingAutoInjectorComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<XenoArtifactThrowingAutoInjectorComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<XenoArtifactThrowingAutoInjectorComponent, GotEquippedHandEvent>(OnGotEquippedHand);
    }

    private void OnGotEquippedHand(EntityUid uid, XenoArtifactThrowingAutoInjectorComponent comp, ref GotEquippedHandEvent args)
    {
        EnsureComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid);
    }

    private void OnThrown(EntityUid uid, XenoArtifactThrowingAutoInjectorComponent comp, ref ThrownEvent args)
    {
        RemComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid);
    }

    private void OnStartCollide(EntityUid uid, XenoArtifactThrowingAutoInjectorComponent comp, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (HasComp<XenoArtifactThrowingAutoInjectorMarkComponent>(target))
            return;

        EntityManager.AddComponent<XenoArtifactThrowingAutoInjectorMarkComponent>(target);
        EntityManager.AddComponent<XenoArtifactComponent>(target);
        EnsureComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid);
        RemCompDeferred<EmbeddableProjectileComponent>(uid);
        comp.Used = true;
        _audio.PlayPvs(comp.HypospraySound, uid);
    }
}
