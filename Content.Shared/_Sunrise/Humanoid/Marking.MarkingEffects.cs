using System.Linq;
using Content.Shared._Sunrise.MarkingEffects;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Humanoid.Markings;

public partial record struct Marking
{
    /// <summary>
    /// Visual effects associated with each color layer of the marking.
    /// </summary>
    [DataField("markingEffects", customTypeSerializer: typeof(MarkingEffectListSerializer))]
    public List<MarkingEffect> MarkingEffects = [];

    public Marking(
        ProtoId<MarkingPrototype> markingId,
        IEnumerable<Color> markingColors,
        IEnumerable<MarkingEffect>? markingEffects)
        : this(markingId, markingColors)
    {
        MarkingEffects = markingEffects?.Select(effect => effect.Clone()).ToList() ?? [];
        EnsureMarkingEffects();
    }

    /// <summary>
    /// Creates an independent copy, including mutable color and effect collections.
    /// </summary>
    public Marking DeepClone()
    {
        return new Marking(MarkingId, _markingColors, MarkingEffects)
        {
            Forced = Forced,
        };
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

    public void SetColor(int colorIndex, Color color) => _markingColors[colorIndex] = color;

    public void SetColor(Color color)
    {
        for (var i = 0; i < _markingColors.Count; i++)
            _markingColors[i] = color;
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
            MarkingEffects[i] = effect.Clone();
    }

    public override string ToString()
    {
        EnsureMarkingEffects();

        var sanitizedName = MarkingId.Id.Replace('@', '_');
        var colors = _markingColors.Select(color => color.ToHex());

        if (MarkingEffects.Count == 0)
            return $"{sanitizedName}@{string.Join(",", colors)}";

        return $"{sanitizedName}@{string.Join(",", colors)}@{string.Join(";", MarkingEffects)}";
    }

    private void InitializeSunriseMarkingEffects()
    {
        MarkingEffects = [];
        EnsureMarkingEffects();
    }

    private Marking WithSunriseColors(List<Color> colors)
    {
        var copy = DeepClone();
        copy._markingColors = colors;
        copy.EnsureMarkingEffects();
        return copy;
    }

    private int GetSunriseHashCode()
    {
        var hash = new HashCode();
        hash.Add(MarkingId);
        hash.Add(Forced);

        foreach (var color in MarkingColors)
            hash.Add(color);

        for (var i = 0; i < MarkingColors.Count; i++)
            hash.Add(GetMarkingEffectOrDefault(i).Type);

        return hash.ToHashCode();
    }

    private static Marking? ParseSunriseDbString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var split = input.Split('@');
        if (split.Length < 2)
            return null;

        var colors = new List<Color>();
        foreach (var colorHex in split[1].Split(','))
            colors.Add(Color.FromHex(colorHex));

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
