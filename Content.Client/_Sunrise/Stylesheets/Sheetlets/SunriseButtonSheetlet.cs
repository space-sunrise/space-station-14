using System.Numerics;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SunriseButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        return new StyleRule[]
        {
            E<TextureButton>().Class("LobbyIconButton").Pseudo(TextureButton.StylePseudoClassHover).Prop(Control.StylePropertyModulateSelf, Palettes.AlphaModulate.HoveredElement),
            E<TextureButton>().Class("LobbyIconButton").Pseudo(TextureButton.StylePseudoClassPressed).Prop(Control.StylePropertyModulateSelf, Palettes.AlphaModulate.PressedElement),
            E<TextureButton>().Class("LobbyIconButton").Pseudo(TextureButton.StylePseudoClassNormal).Prop(Control.StylePropertyModulateSelf, Palettes.AlphaModulate.Element),
            E<TextureButton>().Class("LobbyIconButton").Pseudo(TextureButton.StylePseudoClassDisabled).Prop(Control.StylePropertyModulateSelf, Palettes.AlphaModulate.DisabledElement),
        };
    }
}
