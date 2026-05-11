using Content.Shared.Clothing.Dirt;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Maths;

namespace Content.Client.Clothing.Dirt.UI;

[GenerateTypedNameReferences]
public sealed partial class ClothingDirtTooltipControl : PanelContainer
{
    public ClothingDirtTooltipControl()
    {
        RobustXamlLoader.Load(this);
    }

    public void Fill(ClothingDirtComponent dirt)
    {
        var lvl = dirt.DirtLevel;

        DirtPct.Text = $"{lvl:F0}%";
        DirtPct.FontColorOverride = lvl switch
        {
            > 66f => new Color(0.9f, 0.15f, 0.15f),
            > 33f => new Color(0.95f, 0.6f, 0.1f),
            _     => new Color(0.85f, 0.85f, 0.2f),
        };

        BarBg.MinWidth = 138;
        BarFill.MinWidth = (int)(138f * lvl / 100f);
        BarFill.PanelOverride = new StyleBoxFlat { BackgroundColor = dirt.DirtColor.WithAlpha(0.9f) };

        // слои - показываем если их больше одного
        Layers.RemoveAllChildren();
        if (dirt.Layers.Count > 1)
        {
            Layers.Visible = true;
            foreach (var layer in dirt.Layers.Where(l => l.Intensity > 0f))
            {
                var row = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    SeparationOverride = 4,
                };

                var dot = new PanelContainer { MinWidth = 7, MinHeight = 7 };
                dot.VerticalAlignment = Control.VAlignment.Center;
                dot.PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = layer.Color,
                    BorderColor = new Color(0f, 0f, 0f, 0.4f),
                    BorderThickness = new Thickness(1),
                };

                row.AddChild(dot);
                row.AddChild(new Label
                {
                    Text = GuessDirtName(layer.Color),
                    HorizontalExpand = true,
                    StyleClasses = { "tooltipLabel" },
                });
                row.AddChild(new Label
                {
                    Text = $"{layer.Intensity:F0}%",
                    FontColorOverride = layer.Color,
                    StyleClasses = { "tooltipValue" },
                });

                Layers.AddChild(row);
            }
        }
        else
        {
            Layers.Visible = false;
        }

        HintSep.Visible = true;
        Hint.Visible = true;
    }

    // грубое определение источника по цвету
    // в будущем можно вынести в ClothingDirtLayer.SourceTag
    private static string GuessDirtName(Color c)
    {
        if (c.R > 0.4f && c.G < 0.25f && c.B < 0.25f) return "Кровь";
        if (c.G > 0.4f && c.R < 0.3f && c.B < 0.3f)  return "Слизь";
        if (c.R < 0.2f && c.G < 0.2f && c.B < 0.2f)  return "Масло";
        if (c.B > 0.55f)                               return "Химикат";
        return "Грязь";
    }
}
