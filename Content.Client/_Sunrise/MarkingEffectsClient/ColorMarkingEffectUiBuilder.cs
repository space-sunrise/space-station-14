using Content.Client._Sunrise.UserInterface.Controls;
using Content.Shared._Sunrise.MarkingEffects;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.MarkingEffectsClient;

public sealed class ColorMarkingEffectUiBuilder : IMarkingEffectUiBuilder
{
    public void BuildUI(MarkingEffect effect, MarkingEffectSelectorSliders parent)
    {
        if (effect is not ColorMarkingEffect)
            return;

        parent.CreateSelector(type: MarkingEffectType.Color);
    }
}
