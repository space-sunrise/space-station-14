// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.CCVar;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Disease;
namespace Content.Shared._Sunrise.Disease;
using Content.Shared.Drunk;
using Content.Shared.StatusEffect;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._Sunrise.Disease;
using Robust.Shared.Random;
public abstract class SharedSickSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public void OnInfected(EntityUid uid, EntityUid disease, float prob)
    {
        if (HasComp<DiseaseImmuneComponent>(uid)) return;


        if (_robustRandom.Prob(prob))
        {
            EnsureComp<SickComponent>(uid).owner = disease;
            if (TryComp<DiseaseRoleComponent>(disease, out var compd))
            {
                compd.Infected.Add(uid);
            }
            RaiseNetworkEvent(new UpdateInfectionsEvent(GetNetEntity(uid)));
        }

    }
}
