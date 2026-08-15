using Content.Shared._Sunrise.Particles;
using Content.Shared.Anomaly.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Particles;

/// <summary>
/// Supplies anomaly severity and light color to data-driven ambient and pulse orchestras.
/// </summary>
public sealed class AnomalyParticleSystem : EntitySystem
{
    [Dependency] private readonly ParticleOrchestraSystem _orchestra = default!;

    private readonly Dictionary<EntityUid, ActiveParticleOrchestra> _active = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AnomalyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<AnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<AnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var orchestra in _active.Values)
        {
            _orchestra.Stop(orchestra);
        }

        _active.Clear();
    }

    private void OnInit(Entity<AnomalyComponent> ent, ref ComponentInit args)
    {
        StopOrchestra(ent);

        if (!TryComp<AnomalyParticleVisualsComponent>(ent, out var visuals))
            return;

        if (!visuals.Enabled)
            return;

        var orchestra = _orchestra.Start(
            visuals.AmbientOrchestra,
            ent,
            colorOverride: GetTint(ent, visuals),
            intensity: GetAmbientIntensity(ent.Comp.Severity, visuals));
        if (orchestra != null)
            _active[ent] = orchestra;
    }

    private void OnShutdown(Entity<AnomalyComponent> ent, ref ComponentShutdown args)
    {
        StopOrchestra(ent);
    }

    private void OnSeverityChanged(Entity<AnomalyComponent> ent, ref AnomalySeverityChangedEvent args)
    {
        if (_active.TryGetValue(ent, out var orchestra) &&
            TryComp<AnomalyParticleVisualsComponent>(ent, out var visuals))
        {
            _orchestra.UpdateIntensity(orchestra, GetAmbientIntensity(args.Severity, visuals));
        }
    }

    private void OnPulse(Entity<AnomalyComponent> ent, ref AnomalyPulseEvent args)
    {
        if (!TryComp<AnomalyParticleVisualsComponent>(ent, out var visuals))
            return;

        if (!visuals.Enabled)
            return;

        var power = Math.Clamp(args.PowerModifier, 0.5f, 1.75f);
        var severity = Math.Clamp(1f + args.Severity * 0.45f * power, 0.9f, 1.55f);
        SpawnPulse(ent, visuals, visuals.PulseIntensity * severity);
    }

    private void OnSupercritical(Entity<AnomalyComponent> ent, ref AnomalySupercriticalEvent args)
    {
        if (!TryComp<AnomalyParticleVisualsComponent>(ent, out var visuals))
            return;

        if (!visuals.Enabled)
            return;

        SpawnPulse(ent, visuals, visuals.PulseIntensity * 1.85f);
    }

    private void SpawnPulse(
        EntityUid uid,
        AnomalyParticleVisualsComponent visuals,
        float intensity)
    {
        _orchestra.Spawn(
            visuals.PulseOrchestra,
            uid,
            colorOverride: GetTint(uid, visuals),
            intensity: intensity);
    }

    private Color? GetTint(EntityUid uid, AnomalyParticleVisualsComponent visuals)
    {
        if (!visuals.TintFromPointLight)
            return null;

        return TryComp<PointLightComponent>(uid, out var light)
            ? ParticleColorHelper.EnsureVisible(light.Color)
            : Color.MediumPurple;
    }

    private static float GetAmbientIntensity(
        float severity,
        AnomalyParticleVisualsComponent visuals)
    {
        return visuals.AmbientIntensity * (1.3f + Math.Clamp(severity, 0f, 1f) * 0.45f);
    }

    private void StopOrchestra(EntityUid uid)
    {
        if (_active.Remove(uid, out var orchestra))
            _orchestra.Stop(orchestra);
    }
}
