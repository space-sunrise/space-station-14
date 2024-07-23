using Content.Shared._Sunrise.GhostTheme;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.GhostTheme;

public sealed class GhostThemeSystem: EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostThemeComponent, AfterAutoHandleStateEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, GhostThemeComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (component.GhostTheme == null
            || !_prototypeManager.TryIndex<GhostThemePrototype>(component.GhostTheme, out var ghostThemePrototype))
        {
            return;
        }
        foreach (var entry in ghostThemePrototype.Components.Values)
        {
            if (entry.Component is not SpriteComponent spriteComponent ||
                !EntityManager.TryGetComponent<SpriteComponent>(uid, out var targetsprite))
                continue;
            targetsprite.CopyFrom(spriteComponent);
            targetsprite.LayerSetShader(0, "unshaded");
        }
    }
}
