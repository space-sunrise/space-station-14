using Content.Client._Sunrise.UserInterface.Controls;
using Content.Shared._Sunrise.MarkingEffects;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.MarkingEffectsClient;

public sealed class RoughGradientMarkingEffectUiBuilder : IMarkingEffectUiBuilder
{
    public void BuildUI(MarkingEffect effect, MarkingEffectSelectorSliders parent)
    {
        if (effect is not RoughGradientMarkingEffect rough)
            return;

        parent.CreateSelector(type: MarkingEffectType.RoughGradient);
        parent.CreateSelector("gradient", type: MarkingEffectType.RoughGradient);

        parent.CreateToggle(Loc.GetString("marking-effect-roughgradient-parameter-horizontal"),
            rough.Horizontal,
            val => rough.Horizontal = val);
    }
}
