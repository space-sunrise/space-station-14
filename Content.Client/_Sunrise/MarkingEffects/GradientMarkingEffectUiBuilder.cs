using Content.Client._Sunrise.UserInterface.Controls;
using Content.Shared._Sunrise.ExtendedColor;

namespace Content.Client._Sunrise.MarkingEffects;

public sealed class GradientMarkingEffectUiBuilder : IMarkingEffectUiBuilder
{
    public void BuildUI(MarkingEffect effect, MarkingEffectSelectorSliders parent)
    {
        var gradient = (GradientMarkingEffect) effect;

        parent.CreateSelector();
        parent.CreateSelector("gradient");

        parent.CreateSlider("offsetY", (int)(gradient.Offset.Y * 100), -100, 100, v => gradient.Offset.Y = v / 100f);
        parent.CreateSlider("sizeY", (int)(gradient.Size.Y * 100), 30, 500, v => gradient.Size.Y = v / 100f);
        parent.CreateSlider("rotation", (int)gradient.Rotation, 0, 360, v => gradient.Rotation = v);
        parent.CreateToggle("pixelation", gradient.Pixelated, v => gradient.Pixelated = v);
        parent.CreateToggle("mirror", gradient.Mirrored, v => gradient.Mirrored = v);
    }
}
