using Content.Shared._Sunrise.GhostTheme;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Content.Sunrise.Interfaces.Shared; // Sunrise-Sponsors

namespace Content.Server._Sunrise.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    private ISharedSponsorsManager? _sponsorsManager; // Sunrise-Sponsors

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnPlayerAttached);

        IoCManager.Instance!.TryResolveType(out _sponsorsManager); // Sunrise-Sponsors
    }

    private void OnPlayerAttached(EntityUid uid, GhostComponent component, PlayerAttachedEvent args)
    {
        if (_sponsorsManager == null ||
            !_sponsorsManager.TryGetGhostTheme(args.Player.UserId, out var ghostTheme) ||
            !_prototypeManager.TryIndex<GhostThemePrototype>(ghostTheme, out var ghostThemePrototype)
           )
        {
            return;
        }
        foreach (var entry in ghostThemePrototype!.Components.Values)
        {
            var comp = (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true);
            comp.Owner = uid;
            EntityManager.AddComponent(uid, comp, true);
        }

        EnsureComp<GhostThemeComponent>(uid).GhostTheme = ghostTheme;

    }
}
