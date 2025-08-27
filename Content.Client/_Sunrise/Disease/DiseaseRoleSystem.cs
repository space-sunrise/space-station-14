using Content.Shared.Humanoid;
using Content.Shared._Sunrise.Disease;
using Content.Client._Sunrise.Disease.UI;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._Sunrise.Disease;

public sealed class DiseaseRoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ClientInfectEvent>(OnInfect);
        SubscribeNetworkEvent<DiseaseInfoData>(OnDiseaseInfo);
    }



    private void OnInfect(ClientInfectEvent ev)
    {
        var target = GetEntity(ev.Infected);
        var performer = GetEntity(ev.Owner);

        if (!TryComp<HumanoidAppearanceComponent>(target, out var body))
            return;

        var sick = EnsureComp<SickComponent>(target);
        sick.owner = performer;
        sick.Inited = true;
        if (TryComp<DiseaseRoleComponent>(performer, out var comp))
        {
            comp.Infected.Add(target);
        }
    }

    private void OnDiseaseInfo(DiseaseInfoData ev)
    {
        var window = IoCManager.Resolve<IUserInterfaceManager>().CreateWindow<DiseaseInfoWindow>();
        window.Populate(ev);
    }

}
