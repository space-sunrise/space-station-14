using Content.Shared._Sunrise.Boss.Components;
using Content.Shared.Damage;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Sunrise.Boss.Systems;

public abstract class SharedDamageOnCollideSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
}
