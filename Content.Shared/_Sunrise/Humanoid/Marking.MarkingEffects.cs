using System.Linq;
using Content.Shared._Sunrise.MarkingEffects;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Humanoid.Markings;

public sealed partial class Marking
{
    /// <summary>
    /// Visual effects associated with each color layer of the marking.
    /// </summary>
    [DataField("markingEffects", customTypeSerializer: typeof(MarkingEffectListSerializer))]
    public List<MarkingEffect> MarkingEffects = [];

    public Marking(string markingId, List<Color> markingColors, List<MarkingEffect>? markingEffects)
        : this(markingId, markingColors)
    {
        MarkingEffects = markingEffects ?? [];
        EnsureMarkingEffects();
    }

    public Marking(string markingId, IReadOnlyList<Color> markingColors, IReadOnlyList<MarkingEffect>? markingEffects)
        : this(markingId,
            new List<Color>(markingColors),
            markingEffects is null ? [] : new List<MarkingEffect>(markingEffects))
    {
    }

    /// <summary>
    /// Keeps the effect collection aligned with the marking color collection.
    /// </summary>
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

    /// <summary>
    /// Returns the effect for a color layer or a compatible solid-color fallback.
    /// </summary>
    public MarkingEffect GetMarkingEffectOrDefault(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= _markingColors.Count)
            return ColorMarkingEffect.White;

        if (MarkingEffects is null || MarkingEffects.Count <= colorIndex)
            return new ColorMarkingEffect(_markingColors[colorIndex]);

        return MarkingEffects[colorIndex];
    }

    /// <summary>
    /// Checks whether this marking and another marking have equivalent visual effects.
    /// </summary>
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

    /// <summary>
    /// Replaces the effect associated with a specific color layer.
    /// </summary>
    public void SetMarkingEffect(int colorIndex, MarkingEffect effect)
    {
        EnsureMarkingEffects();

        if (colorIndex >= 0 && colorIndex < MarkingEffects.Count)
            MarkingEffects[colorIndex] = effect;
    }

    /// <summary>
    /// Replaces every color-layer effect with the provided effect.
    /// </summary>
    public void SetMarkingEffect(MarkingEffect effect)
    {
        EnsureMarkingEffects();

        for (var i = 0; i < MarkingEffects.Count; i++)
        {
            MarkingEffects[i] = effect;
        }
    }

    private void CopyMarkingEffects(Marking other)
    {
        MarkingEffects = other.MarkingEffects?.Select(effect => effect.Clone()).ToList() ?? [];
        EnsureMarkingEffects();
    }

    private string ToDbString()
    {
        EnsureMarkingEffects();

        var sanitizedName = MarkingId.Replace('@', '_');
        var colors = _markingColors.Select(color => color.ToHex());

        if (MarkingEffects.Count == 0)
            return $"{sanitizedName}@{string.Join(",", colors)}";

        return $"{sanitizedName}@{string.Join(",", colors)}@{string.Join(";", MarkingEffects)}";
    }

    private static Marking? ParseDbString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var split = input.Split('@');
        if (split.Length < 2)
            return null;

        var colors = new List<Color>();
        foreach (var colorHex in split[1].Split(','))
        {
            colors.Add(Color.FromHex(colorHex));
        }

        if (split.Length == 2)
            return new Marking(split[0], colors);

        var effects = new List<MarkingEffect>();
        foreach (var effectString in split[2].Split(';'))
        {
            if (MarkingEffect.Parse(effectString) is { } effect)
                effects.Add(effect);
        }

        return new Marking(split[0], colors, effects);
    }
}
