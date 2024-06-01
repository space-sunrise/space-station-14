using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Humanoid;
using Content.Shared.Ligyb;
namespace Content.Client.Ligyb;

public sealed class DiseaseRoleSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ClientInfectEvent>(OnInfect);
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
        if(TryComp<DiseaseRoleComponent>(performer, out var comp))
        {
            comp.Infected.Add(target);
        }
    }

}
