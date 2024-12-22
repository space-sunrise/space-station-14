using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.SponsorTiers
{
    public sealed class MarkingPreview : Control
    {

        private static readonly RsiDirection[] Directions =
        {
            RsiDirection.North, RsiDirection.East, RsiDirection.South, RsiDirection.West
        };

        private int _currentDirectionIndex;
        private float _directionChangeTimer = 1.0f;

        private readonly List<List<Control>> _directionalLayers = new();

        public MarkingPreview()
        {
            IoCManager.InjectDependencies(this);
        }

        public void SetLayersFromSprites(IRsiStateLike dummyStateLike, List<SpriteSpecifier> sprites)
        {
            foreach (var child in Children)
            {
                RemoveChild(child);
            }

            _directionalLayers.Clear();

            var backgroundLayer = new List<Control>();
            foreach (var direction in Directions)
            {
                var icon = new TextureRect
                {
                    MinSize = new Vector2(128, 128),
                    Stretch = TextureRect.StretchMode.KeepAspectCentered,
                    Texture = dummyStateLike.GetFrame(direction, 0),
                    Visible = false
                };

                backgroundLayer.Add(icon);
                AddChild(icon);
            }

            _directionalLayers.Add(backgroundLayer);

            foreach (var sprite in sprites)
            {
                var directionControls = new List<Control>();

                foreach (var direction in Directions)
                {
                    var icon = new TextureRect
                    {
                        MinSize = new Vector2(128, 128),
                        Stretch = TextureRect.StretchMode.KeepAspectCentered,
                        Texture = sprite.RsiStateLike().GetFrame(direction, 0),
                        Visible = false
                    };

                    directionControls.Add(icon);
                    AddChild(icon);
                }

                _directionalLayers.Add(directionControls);
            }

            UpdateDirectionVisibility();
        }

        private void UpdateDirectionVisibility()
        {
            for (var i = 0; i < _directionalLayers.Count; i++)
            {
                for (var j = 0; j < Directions.Length; j++)
                {
                    _directionalLayers[i][j].Visible = j == _currentDirectionIndex;
                }
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            if (!VisibleInTree || _directionalLayers.Count == 0)
                return;

            _directionChangeTimer -= args.DeltaSeconds;
            if (_directionChangeTimer <= 0)
            {
                _directionChangeTimer = 1.0f;
                _currentDirectionIndex = (_currentDirectionIndex + 1) % Directions.Length;
                UpdateDirectionVisibility();
            }
        }
    }
}
