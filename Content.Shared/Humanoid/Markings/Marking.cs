using System.Linq;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings
{
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class Marking : IEquatable<Marking>, IComparable<Marking>, IComparable<string>
    {
        [DataField("markingColor")]
        private List<Color> _markingColors = new();

        // sunrise gradient edit start
        [DataField("markingEffects")]
        public List<MarkingEffect> MarkingEffects = new();
        // sunrise gradient edit end


        private Marking()
        {
        }

        public Marking(string markingId,
            List<Color> markingColors,
            List<MarkingEffect>? markingEffects = null)
        {
            MarkingId = markingId;
            _markingColors = markingColors;
            MarkingEffects = markingEffects ?? new(); // sunrise gradient edit
        }

        public Marking(string markingId,
            IReadOnlyList<Color> markingColors,
            IReadOnlyList<MarkingEffect>? markingEffects = null)
            : this(
                markingId,
                new List<Color>(markingColors),
                markingEffects is not null ? new List<MarkingEffect>(markingEffects) : new List<MarkingEffect>())
        {
        }

        public Marking(string markingId, int colorCount)
        {
            MarkingId = markingId;
            List<Color> colors = new();
            for (int i = 0; i < colorCount; i++)
            {
                colors.Add(Color.White);
                MarkingEffects.Add(ColorMarkingEffect.White);
            }

            _markingColors = colors;
        }

        public Marking(Marking other)
        {
            MarkingId = other.MarkingId;
            _markingColors = new(other.MarkingColors);
            MarkingEffects = other.MarkingEffects.Select(e => e.Clone()).ToList();
            Visible = other.Visible;
            Forced = other.Forced;
        }

        /// <summary>
        ///     ID of the marking prototype.
        /// </summary>
        [DataField("markingId", required: true)]
        public string MarkingId { get; private set; } = default!;

        /// <summary>
        ///     All colors currently on this marking.
        /// </summary>
        [ViewVariables]
        public IReadOnlyList<Color> MarkingColors => _markingColors;

        /// <summary>
        ///     If this marking is currently visible.
        /// </summary>
        [DataField("visible")]
        public bool Visible = true;

        /// <summary>
        ///     If this marking should be forcefully applied, regardless of points.
        /// </summary>
        [ViewVariables]
        public bool Forced;

        public void SetColor(int colorIndex, Color color) =>
            _markingColors[colorIndex] = color;

        public void SetColor(Color color)
        {
            for (int i = 0; i < _markingColors.Count; i++)
            {
                _markingColors[i] = color;
            }
        }

        public void SetMarkingEffect(int colorIndex, MarkingEffect effect)
        {
            if(MarkingEffects.Count > colorIndex && colorIndex >= 0)
                MarkingEffects[colorIndex] = effect;
        }

        public void SetMarkingEffect(MarkingEffect effect)
        {
            for (int i = 0; i < MarkingEffects.Count; i++)
            {
                MarkingEffects[i] = effect;
            }
        }

        public int CompareTo(Marking? marking)
        {
            if (marking == null)
            {
                return 1;
            }

            return string.Compare(MarkingId, marking.MarkingId, StringComparison.Ordinal);
        }

        public int CompareTo(string? markingId)
        {
            if (markingId == null)
                return 1;

            return string.Compare(MarkingId, markingId, StringComparison.Ordinal);
        }

        public bool Equals(Marking? other)
        {
            if (other == null)
            {
                return false;
            }
            return MarkingId.Equals(other.MarkingId)
                && _markingColors.SequenceEqual(other._markingColors)
                && Visible.Equals(other.Visible)
                && Forced.Equals(other.Forced);
        }

        // VERY BIG TODO: TURN THIS INTO JSONSERIALIZER IMPLEMENTATION


        // look this could be better but I don't think serializing
        // colors is the correct thing to do
        //
        // this is still janky imo but serializing a color and feeding
        // it into the default JSON serializer (which is just *fine*)
        // doesn't seem to have compatible interfaces? this 'works'
        // for now but should eventually be improved so that this can,
        // in fact just be serialized through a convenient interface
        public new string ToString()
        {
            string sanitizedName = this.MarkingId.Replace('@', '_');

            var colorStringList = _markingColors.Select(c => c.ToHex()).ToList();
            if (MarkingEffects == null || MarkingEffects.Count == 0)
                return $"{sanitizedName}@{string.Join(',', colorStringList)}";

            var extColorsList = MarkingEffects.Select(ext => ext.ToString());

            var extColorsString = string.Join(";", extColorsList);
            return $"{sanitizedName}@{string.Join(',', colorStringList)}@{extColorsString}";
        }

        public static Marking? ParseFromDbString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var split = input.Split('@');
            if (split.Length < 2)
                return null;

            var name = split[0];
            var colorsRaw = split[1];

            var colorList = new List<Color>();
            foreach (var colorHex in colorsRaw.Split(','))
            {
                colorList.Add(Color.FromHex(colorHex));
            }

            if (split.Length == 2)
                return new Marking(name, colorList);

            var extColorsRaw = split[2];
            var markingEffects = new List<MarkingEffect>();

            foreach (var extColorStr in extColorsRaw.Split(';'))
            {
                var parsed = MarkingEffect.Parse(extColorStr);
                if (parsed != null)
                    markingEffects.Add(parsed);
            }

            return new Marking(name, colorList, markingEffects);
        }

    }
}
