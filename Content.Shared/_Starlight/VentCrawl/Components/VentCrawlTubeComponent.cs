using Robust.Shared.Containers;

namespace Content.Shared.VentCrawl.Tube.Components;

[RegisterComponent]
public sealed partial class VentCrawlTubeComponent : Component
{
    public string ContainerId { get; set; } = "VentCrawlTube";

    public bool Connected;

    [ViewVariables]
    public Container Contents { get; set; } = default!;
}

[ByRefEvent]
public record struct GetVentCrawlsConnectableDirectionsEvent
{
    public Direction[] Connectable;
}
