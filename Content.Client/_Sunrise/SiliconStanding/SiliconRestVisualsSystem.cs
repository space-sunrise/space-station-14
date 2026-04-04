using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Content.Shared.Mobs;
using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Client._Sunrise.SiliconStanding;

/// <summary>
/// Handles resting (sitting) visuals for borgs.
/// Applies "_rest" sprite states to the body layer when available,
/// based on SiliconStandingVisuals.Resting appearance data.
/// </summary>
public sealed class SunriseBorgRestVisualsSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SiliconRestVisualsComponent, AppearanceChangeEvent>(OnAppearance);
    }

    /// <summary>
    /// Applies resting body visuals when the borg enters a resting state.
    /// Only modifies the body layer and does not affect light rendering.
    /// </summary> 
    private void OnAppearance(EntityUid uid, SiliconRestVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(uid, SiliconStandingVisuals.Resting, out var isResting) || !isResting)
            return;

        if (_appearance.TryGetData<MobState>(uid, MobStateVisuals.State, out var state) && state != MobState.Alive)
            return;

        if (!TryComp<BorgChassisComponent>(uid, out var borg))
            return;

        var sprite = args.Sprite;

        if (!_appearance.TryGetData<bool>(uid, BorgVisuals.HasPlayer, out var hasPlayer))
            hasPlayer = false;

        var baseState = hasPlayer ? borg.HasMindState : borg.NoMindState;

        // Remove suffixes like "_e" and trailing "_" to match RSI naming for rest states
        var normalState = baseState.Replace("_e", "").Replace("_", "");

        var finalState = normalState;

        // Construct resting variant (robot -> robot_rest)
        var restState = normalState + "_rest";

        if (sprite.BaseRSI?.TryGetState(restState, out _) == true)
            finalState = restState;

        if (_sprite.LayerMapTryGet((uid, sprite), BorgVisualLayers.Body, out var layer, false))
            _sprite.LayerSetRsiState(uid, layer, finalState);
    }
}
