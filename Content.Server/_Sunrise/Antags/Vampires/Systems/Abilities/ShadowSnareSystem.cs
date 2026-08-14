using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;
using Content.Shared.Damage.Systems;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Flash;
using Content.Shared.Humanoid;
using Content.Shared.Light.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Server.Light.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.Antags.Vampires.Systems.Abilities;

public sealed class ShadowSnareSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnare = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowSnareComponent, StepTriggerAttemptEvent>(OnShadowSnareStepAttempt);
        SubscribeLocalEvent<ShadowSnareComponent, StepTriggeredOffEvent>(OnShadowSnareTriggered);
        SubscribeLocalEvent<ShadowSnareComponent, AfterFlashedEvent>(OnShadowSnareFlashed);
    }

    private void OnShadowSnareStepAttempt(Entity<ShadowSnareComponent> ent, ref StepTriggerAttemptEvent args)
        => args.Continue = true;

    private void OnShadowSnareTriggered(Entity<ShadowSnareComponent> ent, ref StepTriggeredOffEvent args)
    {
        var target = args.Tripper;

        // Only trigger on humanoids
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        // Don't trigger on vampires or thralls
        if (HasComp<VampireComponent>(target) || HasComp<VampireThrallComponent>(target))
            return;

        // Apply brute damage
        _damageable.TryChangeDamage(target, ent.Comp.Damage, true, origin: ent);

        // Apply temporary blindness using flash system
        var blindDuration = TimeSpan.FromSeconds(ent.Comp.BlindDuration);
        _flash.Flash(target, null, null, blindDuration, slowTo: 1f, displayPopup: false);

        // Extinguish nearby lights
        ExtinguishNearbyLights(ent);

        // Spawn ensnare entity and apply to target
        var ensnareEnt = EntityManager.SpawnAttachedTo(ent.Comp.EnsnarePrototype, Transform(target).Coordinates);
        if (TryComp<EnsnaringComponent>(ensnareEnt, out var ensnaring))
        {
            ensnaring.WalkSpeed = ent.Comp.WalkSpeed;
            ensnaring.SprintSpeed = ent.Comp.SprintSpeed;
            ensnaring.FreeTime = ent.Comp.FreeTime;
            ensnaring.BreakoutTime = ent.Comp.BreakoutTime;
            _ensnare.TryEnsnare(target, ensnareEnt, ensnaring);
        }

        // Play trigger sound
        _audio.PlayPvs(ent.Comp.TriggerSound, ent, AudioParams.Default.WithVolume(1f));

        QueueDel(ent);
    }

    private void OnShadowSnareFlashed(Entity<ShadowSnareComponent> ent, ref AfterFlashedEvent args)
        => QueueDel(ent);

    private void ExtinguishNearbyLights(Entity<ShadowSnareComponent> ent)
    {
        var center = Transform(ent).Coordinates;

        foreach (var lightEnt in _lookup.GetEntitiesInRange(center, ent.Comp.LightExtinguishRadius))
        {
            if (TryComp<PoweredLightComponent>(lightEnt, out var light))
                _poweredLight.SetState(lightEnt, false, light);
        }
    }
}
