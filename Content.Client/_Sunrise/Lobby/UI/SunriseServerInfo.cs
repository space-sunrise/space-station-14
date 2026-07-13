using System.Numerics;
using System.Text;
using Content.Client.Administration.UI.CustomControls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Lobby.UI;
    public sealed class SunriseServerInfo : Control
    {
        [Dependency] private readonly ILocalizationManager _loc = default!;

        private readonly BoxContainer _leftColumn;
        private readonly RichTextLabel _leftLabel;
        private readonly RichTextLabel _rightLabel;
        private readonly VSeparator _separator;
        private const float SeparationOverride = 12;

        public SunriseServerInfo()
        {
            IoCManager.InjectDependencies(this);
            _leftColumn = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                MinWidth = 10
            };

            _leftLabel = new RichTextLabel
            {
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Top,
                MinWidth = 10
            };

            _leftColumn.AddChild(_leftLabel);

            _separator = new VSeparator
            {
                VerticalExpand = true,
                VerticalAlignment = VAlignment.Stretch,
                Modulate = Color.White
            };

            _rightLabel = new RichTextLabel
            {
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Top,
                MinWidth = 10
            };

            AddChild(_leftColumn);
            AddChild(_separator);
            AddChild(_rightLabel);
        }

        public void AddStationTime(Control control)
        {
            _leftColumn.AddChild(control);
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var halfWidth = (availableSize.X - SeparationOverride) / 2;
            _leftColumn.Measure(new Vector2(halfWidth, availableSize.Y));
            _rightLabel.Measure(new Vector2(halfWidth, availableSize.Y));

            var height = Math.Max(_leftColumn.DesiredSize.Y, _rightLabel.DesiredSize.Y);
            return new Vector2(availableSize.X, height);
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var halfWidth = (finalSize.X - SeparationOverride) / 2;

            _leftColumn.Arrange(new UIBox2(0, 0, halfWidth, finalSize.Y));

            var sepWidth = 2;
            var sepX = halfWidth + (SeparationOverride - sepWidth) / 2;
            _separator.Arrange(new UIBox2(sepX, 0, sepX + sepWidth, finalSize.Y));

            var rightX = halfWidth + SeparationOverride;
            _rightLabel.Arrange(new UIBox2(rightX, 0, finalSize.X, finalSize.Y));

            return finalSize;
        }

        public void SetInfoBlob(string markup)
        {
            var roundKey = _loc.GetString("ui-server-info-round");
            var playersKey = _loc.GetString("ui-server-info-players");
            var mapKey = _loc.GetString("ui-server-info-map");
            var modeKey = _loc.GetString("ui-server-info-mode");

            var roundShort = _loc.GetString("ui-server-info-round-short");
            var playersShort = _loc.GetString("ui-server-info-players-short");
            var mapShort = _loc.GetString("ui-server-info-map-short");
            var modeShort = _loc.GetString("ui-server-info-mode-short");

            var lines = markup.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var leftText = new StringBuilder();
            var rightText = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (trimmed.Contains(">"))
                {
                    rightText.AppendLine(trimmed);
                }
                else
                {
                    var updatedLine = trimmed;
                    var parts = updatedLine.Split(':');
                    if (parts.Length > 1)
                    {
                        var key = parts[0].Trim();
                        var val = string.Join(":", parts, 1, parts.Length - 1).Trim();

                        if (key.Equals(roundKey, StringComparison.OrdinalIgnoreCase))
                        {
                            updatedLine = $"{roundShort}: {val}";
                        }
                        else if (key.Equals(playersKey, StringComparison.OrdinalIgnoreCase))
                        {
                            updatedLine = $"{playersShort}: {val}";
                        }
                        else if (key.Equals(mapKey, StringComparison.OrdinalIgnoreCase))
                        {
                            updatedLine = $"{mapShort}: {val}";
                        }
                        else if (key.Equals(modeKey, StringComparison.OrdinalIgnoreCase))
                        {
                            updatedLine = $"{modeShort}: {val}";
                        }
                    }
                    leftText.AppendLine(updatedLine);
                }
            }

            _leftLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(leftText.ToString().Trim()), tagsAllowed: null);
            _rightLabel.SetMessage(FormattedMessage.FromMarkupOrThrow(rightText.ToString().Trim()), tagsAllowed: null);
        }
    }
}
