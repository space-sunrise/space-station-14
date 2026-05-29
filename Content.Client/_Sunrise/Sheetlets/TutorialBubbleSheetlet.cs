using Content.Client._Sunrise.Tutorial;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Commands.Values;
using YamlDotNet.Core.Tokens;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class TutorialBubbleSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var frameColor = Color.DarkGoldenrod;
        var keybindColor = Color.FromHex("#252525");
        var keybindBorderColor = Color.Goldenrod;

        var bubbleFrameTexture = ResCache.GetTexture("/Textures/_Sunrise/Interface/Tutorial/border.svg.96dpi.png");
        var bubbleFrameBox = new StyleBoxTexture
        {
            Texture = bubbleFrameTexture,
            Modulate = frameColor,
        };
        bubbleFrameBox.SetPatchMargin(StyleBox.Margin.All, 2);

        var keybindBox = new StyleBoxFlat
        {
            BackgroundColor = keybindColor,
            BorderColor = keybindBorderColor,
            BorderThickness = new Thickness(1),
        };

        return
        [
            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialBubbleFrame)
                .Panel(bubbleFrameBox),

            E<PanelContainer>()
                .Class(SunriseStyleClass.TutorialKeybindFrame)
                .Panel(keybindBox)
                .Margin(new Thickness(5, 0)),

            E<Label>()
                .Class(SunriseStyleClass.TutorialKeybindFrame)
                .Font(sheet.BaseFont.GetFont(12, FontKind.Bold))
                .FontColor(Color.White)
        ];
    }
}
