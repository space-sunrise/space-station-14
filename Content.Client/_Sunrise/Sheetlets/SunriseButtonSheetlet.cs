using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Sunrise.Sheetlets;

[CommonSheetlet]
public sealed class SunriseButtonSheetlet : Sheetlet<PalettedStylesheet>
{
    private static readonly ResPath GhostDepartmentHeadingPath =
        new("/Textures/_Sunrise/Interface/Ghost/department_heading.svg.96dpi.png");

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var ghostDepartmentHeading = CreateGhostDepartmentHeadingBox(
            ResCache.GetResource<TextureResource>(GhostDepartmentHeadingPath).Texture);

        return
        [
            E<Button>()
                .Class(SunriseStyleClass.StyleClassNoStyle)
                .Box(new StyleBoxFlat
                {
                    BackgroundColor = Color.Transparent,
                    ContentMarginLeftOverride = 15,
                    ContentMarginRightOverride = 15,
                    ContentMarginTopOverride = 12,
                    ContentMarginBottomOverride = 12,
                }),
            // Стандартная палитра кнопок делает pressed-состояние зелёным,
            // поэтому для секций используем PrimaryPalette напрямую.
            E<ContainerButton>()
                .Class(SunriseStyleClass.GhostDepartmentHeading)
                .PseudoNormal()
                .Box(ghostDepartmentHeading)
                .Modulate(sheet.PrimaryPalette.Element),
            E<ContainerButton>()
                .Class(SunriseStyleClass.GhostDepartmentHeading)
                .PseudoHovered()
                .Box(ghostDepartmentHeading)
                .Modulate(sheet.PrimaryPalette.HoveredElement),
            E<ContainerButton>()
                .Class(SunriseStyleClass.GhostDepartmentHeading)
                .PseudoPressed()
                .Box(ghostDepartmentHeading)
                .Modulate(sheet.PrimaryPalette.PressedElement),
            E<ContainerButton>()
                .Class(SunriseStyleClass.GhostDepartmentHeading)
                .PseudoDisabled()
                .Box(ghostDepartmentHeading)
                .Modulate(sheet.SecondaryPalette.DisabledElement),
        ];
    }

    private static StyleBoxTexture CreateGhostDepartmentHeadingBox(Texture texture)
    {
        var styleBox = new StyleBoxTexture
        {
            Texture = texture,
        };

        styleBox.SetPatchMargin(StyleBox.Margin.All, 8);
        styleBox.SetPadding(StyleBox.Margin.All, 1);
        styleBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 12);
        styleBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 5);
        return styleBox;
    }
}
