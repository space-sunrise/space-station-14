using Content.Client._Sunrise.Stylesheets;
using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class SunrisePanelSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        var mappingWidgetPanel = new StyleBoxFlat(sheet.SecondaryPalette.BackgroundDark.WithAlpha(0.8f));

        return
        [
            E<PanelContainer>().Class(SunriseStyleClass.MappingWidgetPanel).Panel(mappingWidgetPanel),
        ];
    }
}
