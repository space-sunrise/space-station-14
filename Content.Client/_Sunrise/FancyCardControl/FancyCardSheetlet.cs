using Content.Client._Sunrise.Sheetlets;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Shared._Sunrise.Boss.Systems;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.FancyCardControl;

[CommonSheetlet]
public sealed class FancyCardSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var background = new StyleBoxFlat
        {
            BackgroundColor = sheet.SecondaryPalette.Background,
        };

        var backgroundLight = new StyleBoxFlat
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundLight,
        };

        var titleBarBox = new StyleBoxFlat
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundLight,
            BorderColor = sheet.SecondaryPalette.Background,
            BorderThickness = new Thickness(2),
        };

        var quoteInnerBox = new StyleBoxFlat
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundDark,
            BorderColor = sheet.SecondaryPalette.Background,
            BorderThickness = new Thickness(1),
        };

        return
        [
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardPrimary)
                .Panel(background),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardSecondary)
                .Panel(backgroundLight),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardTitleBar)
                .Panel(titleBarBox),
            E<PanelContainer>()
                .Class(SunriseStyleClass.FancyCardDescInner)
                .Panel(quoteInnerBox),
        ];
    }
}
