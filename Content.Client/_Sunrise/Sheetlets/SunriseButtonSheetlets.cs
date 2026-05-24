using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Sheetlets;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Toolshed.Commands.Values;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class SunriseButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    private Color _normalColor = Color.FromHex("#222222");
    private Color _hoverColor = Color.FromHex("#2e2e2e");
    private Color _pressedColor = Color.FromHex("#2e2e2e");
    private Color _disabledColor = Color.FromHex("#181818");
    public override StyleRule[] GetRules(T sheet, object config)
    {
        return [
            E<Button>().Class(SunriseStyleClass.StyleClassNoStyle)
                .Box(new StyleBoxFlat
                {
                    BackgroundColor = Color.Transparent,
                    ContentMarginLeftOverride = 15,
                    ContentMarginRightOverride = 15,
                    ContentMarginTopOverride = 12,
                    ContentMarginBottomOverride = 12
                }),

            E<Button>().Class(SunriseStyleClass.TutorialCategoryButtonClass)
                    .Box(StyleBoxHelpers.SquareStyleBox(sheet)),
            // Normal color
            E<Button>().Class(SunriseStyleClass.TutorialCategoryButtonClass)
                    .PseudoNormal()
                    .Modulate(_normalColor),
            // Hover color
            E<Button>().Class(SunriseStyleClass.TutorialCategoryButtonClass)
                    .PseudoHovered()
                    .Modulate(_hoverColor),
            // Pressed color
            E<Button>().Class(SunriseStyleClass.TutorialCategoryButtonClass)
                    .PseudoPressed()
                    .Modulate(_pressedColor),
            // Disabled color
            E<Button>().Class(SunriseStyleClass.TutorialCategoryButtonClass)
                    .PseudoDisabled()
                    .Modulate(_disabledColor),
        ];
    }
}
