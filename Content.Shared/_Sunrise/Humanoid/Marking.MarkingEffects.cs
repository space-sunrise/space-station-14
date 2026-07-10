using Content.Shared._Sunrise.MarkingEffects;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Humanoid.Markings;

public sealed partial class Marking
{
    public void EnsureMarkingEffects()
    {
        MarkingEffects ??= [];

        for (var i = 0; i < _markingColors.Count; i++)
        {
            if (MarkingEffects.Count <= i)
            {
                MarkingEffects.Add(new ColorMarkingEffect(_markingColors[i]));
                continue;
            }

            if (!MarkingEffects[i].Colors.ContainsKey("base"))
                MarkingEffects[i].Colors["base"] = _markingColors[i];
        }

        if (MarkingEffects.Count > _markingColors.Count)
            MarkingEffects.RemoveRange(_markingColors.Count, MarkingEffects.Count - _markingColors.Count);
    }

    public MarkingEffect GetMarkingEffectOrDefault(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= _markingColors.Count)
            return ColorMarkingEffect.White;

        if (MarkingEffects is null || MarkingEffects.Count <= colorIndex)
            return new ColorMarkingEffect(_markingColors[colorIndex]);

        return MarkingEffects[colorIndex];
    }

    public bool MarkingEffectsEqual(Marking other)
    {
        if (_markingColors.Count != other._markingColors.Count)
            return false;

        for (var i = 0; i < _markingColors.Count; i++)
        {
            if (!GetMarkingEffectOrDefault(i).Equals(other.GetMarkingEffectOrDefault(i)))
                return false;
        }

        return true;
    }
}
