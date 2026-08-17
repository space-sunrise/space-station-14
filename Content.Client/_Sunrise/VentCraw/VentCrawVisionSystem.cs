using Content.Client.SubFloor;
using Content.Shared._Sunrise.VentCraw;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.VentCraw;

public sealed class VentCrawSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SubFloorHideSystem _subFloorHideSystem = default!;
    [Dependency] private readonly EntityQuery<VentCrawlerComponent> _ventCrawlerQuery = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalPlayer?.ControlledEntity;

        if (!_ventCrawlerQuery.TryGetComponent(player, out var playerVentCrawlerComponent))
            return;

        _subFloorHideSystem.ShowVentPipe = playerVentCrawlerComponent.InTube;
    }
}
