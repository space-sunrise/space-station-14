// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Robust.Shared.Random;
using Content.Shared.Humanoid;

namespace Content.Shared._Sunrise.Disease;

public abstract class SharedDiseaseRoleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    public override void Initialize()
    {
        base.Initialize();
    }
    public void OnInfect(InfectEvent ev, float probability = 0)
    {
        if (ev.Handled)
            return;
        ev.Handled = true;

        if (!TryComp<DiseaseRoleComponent>(ev.Performer, out var comp)) return;
        if (!HasComp<HumanoidAppearanceComponent>(ev.Target)) return;
        if (HasComp<DiseaseImmuneComponent>(ev.Target)) return;
        if (HasComp<SickComponent>(ev.Target)) return;

        var prob = probability;
        if (probability == 0) prob = comp.BaseInfectChance;
        if (TryComp<DiseaseTempImmuneComponent>(ev.Target, out var immune))
            prob -= immune.Prob;
        prob = Math.Max(Math.Min(prob, 0), 1);
        if (_robustRandom.Prob(prob))
        {
            var comps = AddComp<SickComponent>(ev.Target);
            comps.owner = ev.Performer;
            comp.Infected.Add(ev.Target);
        }
    }
}
