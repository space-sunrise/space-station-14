using System.Numerics;

namespace Content.Server.FootPrints.Components;

[RegisterComponent]
public sealed partial class FootPrintsComponent : Component
{
    [DataField]
    public string LeftBarePrint = "footprint-left-bare-human";

    [DataField]
    public string RightBarePrint = "footprint-right-bare-human";

    [DataField]
    public string ShoesPrint = "footprint-shoes";

    [DataField]
    public string SuitPrint = "footprint-suit";

    [DataField]
    public string[] DraggingPrint =
    {
        "dragging-1",
        "dragging-2",
        "dragging-3",
        "dragging-4",
        "dragging-5"
    };

    [DataField]
    public Vector2 OffsetCenter = new(-0.5f, -1f);

    [DataField]
    public Vector2 OffsetPrint = new(0.1f, 0f);

    [DataField]
    public Color PrintsColor = Color.FromHex("#00000000");

    [DataField]
    public float StepSize = 0.7f;

    [DataField]
    public float DragSize = 0.5f;

    [DataField]
    public float ColorQuantity;

    public Vector2 StepPos = Vector2.Zero;

    public bool RightStep = true;
}
