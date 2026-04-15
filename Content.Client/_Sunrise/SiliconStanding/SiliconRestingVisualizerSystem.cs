using Content.Client.Silicons.Borgs;
using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.SiliconStanding;

public sealed class SiliconRestingVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SiliconStandingSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconRestingComponent, AppearanceChangeEvent>(OnAppearanceChange, after: [typeof(BorgSystem)]);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentStartup>(OnRestingStartup);
        SubscribeLocalEvent<SiliconRestingComponent, ComponentShutdown>(OnRestingShutdown);
    }

    private void OnAppearanceChange(Entity<SiliconRestingComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        Refresh(ent.Owner, args.Sprite);
    }

    private void OnRestingStartup(Entity<SiliconRestingComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent.Owner);
    }

    private void OnRestingShutdown(Entity<SiliconRestingComponent> ent, ref ComponentShutdown args)
    {
        Refresh(ent.Owner);
    }

    public void Refresh(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        if (!TryComp<BorgChassisComponent>(uid, out var borg))
            return;

        var isResting = _standing.GetEffectiveResting(uid);
        UpdateBorgBodyState((uid, sprite), isResting);

        if (!_appearance.TryGetData<bool>(uid, BorgVisuals.HasPlayer, out var hasPlayer))
            hasPlayer = false;

        var lightVisible = !isResting && (borg.BrainEntity != null || hasPlayer);
        _sprite.LayerSetVisible((uid, sprite), BorgVisualLayers.Light, lightVisible);
        _sprite.LayerSetRsiState((uid, sprite), BorgVisualLayers.Light, hasPlayer ? borg.HasMindState : borg.NoMindState);
    }

    private void UpdateBorgBodyState(Entity<SpriteComponent?> ent, bool isResting)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (!_sprite.LayerMapTryGet(ent, BorgVisualLayers.Body, out var layer, false))
            return;

        var currentState = _sprite.LayerGetRsiState(ent, layer).Name;
        var baseState = currentState != null && currentState.EndsWith("_rest")
            ? currentState[..^"_rest".Length]
            : currentState ?? string.Empty;
        var restingState = $"{baseState}_rest";
        var finalState = baseState;

        if (isResting && ent.Comp.BaseRSI?.TryGetState(restingState, out _) == true)
            finalState = restingState;

        _sprite.LayerSetRsiState(ent, layer, finalState);
    }
}
