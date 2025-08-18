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
        // При поднятии возвращаем спрайт в closed только если ещё не был использован
        if (!HasComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid))
            EnsureComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid);
    }

    private void OnThrown(EntityUid uid, XenoArtifactThrowingAutoInjectorComponent comp, ref ThrownEvent args)
    {
        // При броске всегда open (убираем UsedXenoInjectorComponent)
        RemComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid);
    }

    private void OnStartCollide(EntityUid uid, XenoArtifactThrowingAutoInjectorComponent comp, ref StartCollideEvent args)
    {
        // Если уже использован — ничего не делаем
        if (HasComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid))
            return;

        var target = args.OtherEntity;
        // Универсальная проверка: все живые (MobStateComponent), кроме киборгов и синтетиков
        var isLiving = HasComp<Content.Shared.Mobs.Components.MobStateComponent>(target);
        var isBorg = HasComp<Content.Shared.Silicons.Borgs.Components.BorgChassisComponent>(target);
        var isStationAi = HasComp<Content.Shared.Silicons.StationAi.StationAiCoreComponent>(target);
        if (isLiving && !isBorg && !isStationAi)
        {
            if (!HasComp<XenoArtifactThrowingAutoInjectorMarkComponent>(target))
            {
                EntityManager.AddComponent<XenoArtifactThrowingAutoInjectorMarkComponent>(target);
                EntityManager.AddComponent<XenoArtifactComponent>(target);
                // После успешного заражения делаем инъектор использованным и удаляем EmbeddableProjectileComponent
                EnsureComp<UsedXenoArtifactThrowingAutoInjectorComponent>(uid);
                RemComp<EmbeddableProjectileComponent>(uid);
            }
        }
    }
}
