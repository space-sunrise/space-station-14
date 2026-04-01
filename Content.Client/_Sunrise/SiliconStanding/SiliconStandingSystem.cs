using Robust.Client.GameObjects;
using Content.Shared._Sunrise.SiliconStanding;

namespace Content.Client._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconStandingComponent, SiliconRestStartEvent>(OnRestStart);
        SubscribeLocalEvent<SiliconStandingComponent, SiliconRestEndEvent>(OnRestEnd);
    }

    private void OnRestStart(Entity<SiliconStandingComponent> ent, ref SiliconRestStartEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.LayerSetState((ent, sprite), "_rest");
    }

    private void OnRestEnd(Entity<SiliconStandingComponent> ent, ref SiliconRestEndEvent args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            _sprite.LayerSetState((ent, sprite), "robot");
    }
}