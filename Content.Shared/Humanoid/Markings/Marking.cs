using System.Linq;
using Content.Shared._Sunrise.ExtendedColor;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared._Sunrise.ExtendedColor;

namespace Content.Shared.Humanoid.Markings
{
    [DataDefinition]
    [Serializable, NetSerializable]
    public sealed partial class Marking : IEquatable<Marking>, IComparable<Marking>, IComparable<string>
    {
        [DataField("markingColor")]
        private List<Color> _markingColors = new();

        // sunrise gradient edit start
        [DataField("colorType")]
        public ColorType ColorType = ColorType.Color;
        [DataField("extendedColor")]
        public ExtendedColor? ExtendedColor;
        // sunrise gradient edit end


        private Marking()
        {
        }

        public Marking(string markingId,
            List<Color> markingColors,
            ColorType colorType = ColorType.Color,
            ExtendedColor? extendedColor = null)
        {
            MarkingId = markingId;
            _markingColors = markingColors;
            ColorType = colorType; // sunrise gradient edit
            ExtendedColor = extendedColor; // sunrise gradient edit
        }

        public Marking(string markingId,
            IReadOnlyList<Color> markingColors,
            ColorType colorType = ColorType.Color,
                ExtendedColor? extendedColor = null)
            : this(markingId, new List<Color>(markingColors), colorType, extendedColor)
        {
        }

        public Marking(string markingId, int colorCount)
        {
            MarkingId = markingId;
            List<Color> colors = new();
            for (int i = 0; i < colorCount; i++)
                colors.Add(Color.White);
            _markingColors = colors;
        }

        public Marking(Marking other)
        {
            MarkingId = other.MarkingId;
            _markingColors = new(other.MarkingColors);
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
        new public string ToString()
        {
            // reserved character
            string sanitizedName = this.MarkingId.Replace('@', '_');
            List<string> colorStringList = new();
            foreach (Color color in _markingColors)
                colorStringList.Add(color.ToHex());

            if (ColorType == ColorType.Color || ExtendedColor == null)
                return $"{sanitizedName}@{String.Join(',', colorStringList)}";

            Dictionary<string, string> extColorStringDict = new();
            foreach ((string key, Color color) in ExtendedColor.Colors)
                extColorStringDict[key] = color.ToHex();

            var extColorsString = string.Join(",", extColorStringDict.Select(kv => $"{kv.Key}={kv.Value}"));
            return $"{sanitizedName}@{String.Join(',', colorStringList)}@{ColorType}@{extColorsString}";
        }

        public static Marking? ParseFromDbString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var split = input.Split('@');

            switch (split.Length)
            {
                case 2:
                {
                    var name = split[0];
                    var colorsRaw = split[1];
                    var colorList = new List<Color>();
                    foreach (var colorHex in colorsRaw.Split(','))
                        colorList.Add(Color.FromHex(colorHex));

                    return new Marking(name, colorList);
                }
                case 4:
                {
                    var name = split[0];
                    var colorsRaw = split[1];
                    var colorTypeStr = split[2];
                    var extColorsRaw = split[3];

                    var colorList = new List<Color>();
                    foreach (var colorHex in colorsRaw.Split(','))
                        colorList.Add(Color.FromHex(colorHex));

                    if (!Enum.TryParse<ColorType>(colorTypeStr, out var colorType))
                        colorType = ColorType.Color;

                    var extColorDict = new Dictionary<string, Color>();
                    foreach (var kvp in extColorsRaw.Split(','))
                    {
                        var pair = kvp.Split('=');
                        if (pair.Length != 2)
                            continue;
                        var key = pair[0];
                        var valueHex = pair[1];
                        extColorDict[key] = Color.FromHex(valueHex);
                    }

                    var extendedColor = new ExtendedColor(colorType, extColorDict);

                    var marking = new Marking(name, colorList)
                    {
                        ColorType = colorType,
                        ExtendedColor = extendedColor
                    };

                    return marking;
                }
                default:
                    return null;
            }
        }
    }
}
