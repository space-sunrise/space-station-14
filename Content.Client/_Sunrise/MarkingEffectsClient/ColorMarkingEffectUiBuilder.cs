using Content.Client._Sunrise.UserInterface.Controls;
using Content.Shared._Sunrise.MarkingEffects;

namespace Content.Client._Sunrise.MarkingEffectsClient;

public sealed class ColorMarkingEffectUiBuilder : IMarkingEffectUiBuilder
{
    public void BuildUI(MarkingEffect effect, MarkingEffectSelectorSliders parent)
    {
        parent.CreateSelector();
    }
}
