using Content.Client._Sunrise.UserInterface.Controls;
using Content.Shared._Sunrise.MarkingEffects;

namespace Content.Client._Sunrise.MarkingEffectsClient;

public interface IMarkingEffectUiBuilder
{
    void BuildUI(MarkingEffect effect, MarkingEffectSelectorSliders parent);
}

