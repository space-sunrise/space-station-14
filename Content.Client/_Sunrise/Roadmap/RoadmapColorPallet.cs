using Content.Client.Stylesheets.Palette;
using Content.Shared._Sunrise.Roadmap;
using Robust.Shared.Maths;

namespace Content.Client._Sunrise.Roadmap;

public static class RoadmapColorPallet
{
    public static readonly ColorPalette Window = ColorPalette.FromHexBase(
        "#1d1c1c",
        background: Color.FromHex("#1d1c1c"),
        text: Color.FromHex("#ffffff"));

    public static readonly ColorPalette Item = ColorPalette.FromHexBase(
        "#121111",
        background: Color.FromHex("#121111"),
        text: Color.FromHex("#666666"));

    public static readonly ColorPalette VersionCard = ColorPalette.FromHexBase(
        "#121111",
        background: Color.FromHex("#121111"),
        text: Color.FromHex("#ffffff"));

    public static readonly ColorPalette VersionHeader = ColorPalette.FromHexBase(
        "#2a2929",
        background: Color.FromHex("#2a2929"),
        text: Color.FromHex("#ffffff"));

    public static readonly ColorPalette StatePlanned = ColorPalette.FromHexBase("#e74c3c");
    public static readonly ColorPalette StateInProgress = ColorPalette.FromHexBase("#3498db");
    public static readonly ColorPalette StatePartial = ColorPalette.FromHexBase("#f1c40f");
    public static readonly ColorPalette StateComplete = ColorPalette.FromHexBase("#2ecc71");

    public static Color WindowBackground => Window.Background;
    public static Color ItemBackground => Item.Background;
    public static Color ItemHintText => Item.Text;
    public static Color VersionCardBackground => VersionCard.Background;
    public static Color VersionHeaderBackground => VersionHeader.Background;
    public static Color VersionHeaderText => VersionHeader.Text;

    public static Color GetStateColor(RoadmapItemState state)
    {
        return state switch
        {
            RoadmapItemState.Planned => StatePlanned.Base,
            RoadmapItemState.InProgress => StateInProgress.Base,
            RoadmapItemState.Partial => StatePartial.Base,
            RoadmapItemState.Complete => StateComplete.Base,
            _ => Color.Transparent,
        };
    }
}
