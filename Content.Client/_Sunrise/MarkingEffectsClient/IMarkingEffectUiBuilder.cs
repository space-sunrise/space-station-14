using Content.Client._Sunrise.UserInterface.Controls;
using Content.Shared._Sunrise.MarkingEffects;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.MarkingEffectsClient;

public interface IMarkingEffectUiBuilder
{
    /// <summary>
    /// Builds UI elements to customize <see cref="MarkingEffect"/>
    /// </summary>
    void BuildUI(MarkingEffect effect, MarkingEffectSelectorSliders parent);
}

