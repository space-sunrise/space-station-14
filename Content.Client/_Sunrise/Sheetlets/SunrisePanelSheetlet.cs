using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class SunrisePanelSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet
{
    private static readonly ResPath GhostDepartmentBodyPath =
        new("/Textures/_Sunrise/Interface/Ghost/department_body.svg.96dpi.png");

    public override StyleRule[] GetRules(T sheet, object config)
    {
        var mappingWidgetPanel = new StyleBoxFlat(sheet.SecondaryPalette.BackgroundDark.WithAlpha(0.8f));
        var ghostDepartmentBody = CreateGhostDepartmentBodyBox(
            ResCache.GetResource<TextureResource>(GhostDepartmentBodyPath).Texture);

        var prisonerRecordPanel = new StyleBoxFlat
        {
            BackgroundColor = sheet.SecondaryPalette.BackgroundDark,
            BorderColor = sheet.SecondaryPalette.BackgroundLight,
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 8,
            ContentMarginRightOverride = 8,
            ContentMarginTopOverride = 8,
            ContentMarginBottomOverride = 8
        };

        return
        [
            E<PanelContainer>().Class(SunriseStyleClass.MappingWidgetPanel).Panel(mappingWidgetPanel),
            E<PanelContainer>().Class(SunriseStyleClass.PrisonerRecordPanel).Panel(prisonerRecordPanel),
            E<PanelContainer>()
                .Class(SunriseStyleClass.GhostDepartmentBody)
                .Panel(ghostDepartmentBody)
                .Modulate(sheet.PrimaryPalette.Element),
        ];
    }

    private static StyleBoxTexture CreateGhostDepartmentBodyBox(Texture texture)
    {
        var styleBox = new StyleBoxTexture
        {
            Texture = texture,
        };

        styleBox.SetPatchMargin(StyleBox.Margin.All, 8);
        styleBox.SetPadding(StyleBox.Margin.All, 1);
        styleBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 10);
        styleBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 8);
        return styleBox;
    }
}
