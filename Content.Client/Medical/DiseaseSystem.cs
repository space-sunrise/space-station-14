// Sunrise added start
using Content.Shared.Chemistry.Components;
using Content.Shared.Power;
using Robust.Client.GameObjects;
// Sunrise added end

namespace Content.Client.Medical;

/* Here be dragons */

public enum DiseaseMachineVisualLayers : byte
{
    IsOn,
    IsRunning
}

// Sunrise added start
[RegisterComponent]
public sealed partial class DiseaseMachineVisualsComponent : Component
{
    [DataField]
    public string IdleState = "icon";

    [DataField]
    public string RunningState = "running";
}

public sealed class DiseaseMachineVisualsSystem : VisualizerSystem<DiseaseMachineVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, DiseaseMachineVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, PowerDeviceVisuals.Powered, out var powered, args.Component))
        {
            args.Sprite.LayerSetVisible(DiseaseMachineVisualLayers.IsOn, powered);
        }

        if (AppearanceSystem.TryGetData<bool>(uid, SolutionContainerMixerVisuals.Mixing, out var mixing, args.Component))
        {
            args.Sprite.LayerSetState(DiseaseMachineVisualLayers.IsRunning, mixing ? component.RunningState : component.IdleState);
        }
    }
}
// Sunrise added end
