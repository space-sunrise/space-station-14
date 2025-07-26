using Content.Client._Sunrise.UserInterface.Controls;
using Content.Shared._Sunrise.ExtendedColor;

namespace Content.Client._Sunrise.MarkingEffects;

public interface IMarkingEffectUiBuilder
{
    void BuildUI(MarkingEffect effect, MarkingEffectSelectorSliders parent);
}

