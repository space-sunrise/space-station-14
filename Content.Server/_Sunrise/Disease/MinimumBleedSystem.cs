// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Content.Shared.Body.Components;
using Robust.Shared.Configuration;
namespace Content.Server._Sunrise.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.Store.Systems;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Server.Medical;
public sealed class MinimumBleedSystem : EntitySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MinimumBleedComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (TryComp<BloodstreamComponent>(uid, out var blood))
            {
                if (blood.BleedAmount < component.MinValue)
                {
                    _bloodstream.TryModifyBleedAmount((uid, blood), component.MinValue);
                }
            }
        }
    }
}
