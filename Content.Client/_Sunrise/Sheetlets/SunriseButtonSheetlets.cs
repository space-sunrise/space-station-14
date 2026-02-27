using System.Numerics;
using Content.Client._Sunrise.Stylesheets;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class SunriseButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
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
        ];
    }
}
