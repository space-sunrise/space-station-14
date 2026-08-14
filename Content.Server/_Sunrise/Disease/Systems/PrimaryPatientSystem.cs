// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease;
using Content.Shared._Sunrise.TimeWindow;
using Content.Server.Popups;
using Content.Shared.Popups;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class PrimaryPatientSystem : EntitySystem
{
    [Dependency] private readonly SentientDiseaseSystem _sentientDiseaseSystem = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    private const int Compensation = 5000;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrimaryPatientComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PrimaryPatientComponent, CureDiseaseEvent>(OnCureDisease);
        SubscribeLocalEvent<PrimaryPatientComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<PrimaryPatientComponent, EnterCryostorageEvent>(OnMindRemoved);
    }

    private void OnMindRemoved(EntityUid uid, PrimaryPatientComponent component, EnterCryostorageEvent args)
    {
        if (!TryComp<SentientDiseaseComponent>(component.SentientDisease, out var sentientDiseaseComp))
            return;

        if (sentientDiseaseComp.Data != null)
        {
            sentientDiseaseComp.Data.MutationPoints += Compensation;
            sentientDiseaseComp.FactPrimaryInfected--;
            _popupSystem.PopupEntity(
                Loc.GetString("sentient-disease-infect-compensation", ("price", Compensation)),
                component.SentientDisease.Value,
                component.SentientDisease.Value,
                PopupType.Medium
            );
        }

        _disease.CureDisease(uid);
    }

    private void OnInit(Entity<PrimaryPatientComponent> entity, ref ComponentInit args)
    {
        _timedWindowSystem.Reset(entity.Comp.UpdateWindow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PrimaryPatientComponent, DiseaseComponent>();
        while (query.MoveNext(out var uid, out var component, out var diseaseComponent))
        {
            if (_timedWindowSystem.IsExpired(component.UpdateWindow))
            {
                _timedWindowSystem.Reset(component.UpdateWindow);
                _disease.InfectAround((uid, diseaseComponent), component.RangeInfect);
            }
        }
    }

    private void OnCureDisease(EntityUid uid, PrimaryPatientComponent component, CureDiseaseEvent args)
    {
        RemComp<PrimaryPatientComponent>(uid);
    }

    private void OnRemove(EntityUid uid, PrimaryPatientComponent component, ComponentRemove args)
    {
        if (component.SentientDisease != null
            && TryComp<SentientDiseaseComponent>(component.SentientDisease, out var sentientDisease))
            _sentientDiseaseSystem.RemovePrimaryInfected(component.SentientDisease.Value, uid, sentientDisease);
    }

}
