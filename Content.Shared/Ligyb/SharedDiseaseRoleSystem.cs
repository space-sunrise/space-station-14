// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Humanoid;

namespace Content.Shared.Ligyb;

public abstract class SharedDiseaseRoleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddBaseChanceEvent>(OnBaseChance);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddCoughChanceEvent>(OnCoughChance);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddLethalEvent>(OnLethal);
        SubscribeLocalEvent<DiseaseRoleComponent, DiseaseAddShieldEvent>(OnShield);
    }


    private void OnLethal(EntityUid uid, DiseaseRoleComponent component, DiseaseAddLethalEvent args)
    {
        component.Lethal += 1;
    }

    private void OnShield(EntityUid uid, DiseaseRoleComponent component, DiseaseAddShieldEvent args)
    {
        component.Shield += 1;
    }

    private void OnBaseChance(EntityUid uid, DiseaseRoleComponent component, DiseaseAddBaseChanceEvent args)
    {
        if (component.BaseInfectChance < 0.9f)
            component.BaseInfectChance += 0.1f;
        else
            component.BaseInfectChance = 1;
    }

    private void OnCoughChance(EntityUid uid, DiseaseRoleComponent component, DiseaseAddCoughChanceEvent args)
    {
        if (component.CoughInfectChance < 0.85f)
            component.CoughInfectChance += 0.05f;
        else
            component.CoughInfectChance = 1;
    }
    public void OnInfect(InfectEvent ev)
    {
        if (ev.Handled)
            return;
        if (TryComp<DiseaseRoleComponent>(ev.Performer, out var comp))
        {
            ev.Handled = true;

            if (!TryComp<HumanoidAppearanceComponent>(ev.Target, out var body))
                return;
            if (HasComp<DiseaseImmuneComponent>(ev.Target)) return;
            if (HasComp<SickComponent>(ev.Target)) return;
            var prob = comp.BaseInfectChance;
            if (TryComp<DiseaseTempImmuneComponent>(ev.Target, out var immune))
            {
                prob -= immune.Prob;

            }
            if (prob < 0) prob = 0;
            if (prob > 1) prob = 1;
            if (_robustRandom.Prob(prob))
            {
                var comps = AddComp<SickComponent>(ev.Target);
                comps.owner = ev.Performer;

                comp.Infected.Add(ev.Target);

            }
        }
    }
}
