using Content.Shared._Sunrise.TapeRecorder;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.TapeRecorder;

public sealed class TapeRecorderTimeline : Control
{
    private static readonly Color BackgroundColor = new(0.08f, 0.09f, 0.10f);
    private static readonly Color EmptyTapeColor = new(0.18f, 0.22f, 0.20f);
    private static readonly Color UsedTapeColor = new(0.82f, 0.18f, 0.14f);
    private static readonly Color PositionColor = new(0.62f, 0.96f, 0.50f);
    private static readonly Color BorderColor = new(0.02f, 0.02f, 0.02f);

    private readonly List<TapeCassetteRecordedRange> _recordedRanges = [];
    private TimeSpan _position;
    private TimeSpan _capacity;

    public TapeRecorderTimeline()
    {
        MouseFilter = MouseFilterMode.Pass;
    }

    public void UpdateState(TimeSpan position, TimeSpan capacity, IReadOnlyList<TapeCassetteRecordedRange> recordedRanges)
    {
        _position = position;
        _capacity = capacity;
        _recordedRanges.Clear();
        _recordedRanges.AddRange(recordedRanges);
        InvalidateArrange();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        handle.DrawRect(PixelSizeBox, BackgroundColor);

        if (PixelWidth <= 0 || PixelHeight <= 0)
            return;

        var trackBox = new UIBox2(1, 3, MathF.Max(1, PixelWidth - 1), MathF.Max(3, PixelHeight - 3));
        handle.DrawRect(trackBox, EmptyTapeColor);

        var capacitySeconds = (float) _capacity.TotalSeconds;
        if (capacitySeconds > 0f)
        {
            foreach (var range in _recordedRanges)
            {
                var start = Math.Clamp((float) range.Start.TotalSeconds / capacitySeconds, 0f, 1f);
                var end = Math.Clamp((float) range.End.TotalSeconds / capacitySeconds, 0f, 1f);

                if (end <= start)
                    continue;

                var left = MathHelper.Lerp(trackBox.Left, trackBox.Right, start);
                var right = MathHelper.Lerp(trackBox.Left, trackBox.Right, end);
                handle.DrawRect(new UIBox2(left, trackBox.Top, right, trackBox.Bottom), UsedTapeColor);
            }

            var position = Math.Clamp((float) _position.TotalSeconds / capacitySeconds, 0f, 1f);
            var positionX = MathHelper.Lerp(trackBox.Left, trackBox.Right, position);
            handle.DrawRect(new UIBox2(positionX - 1, trackBox.Top - 1, positionX + 1, trackBox.Bottom + 1), PositionColor);
        }

        handle.DrawRect(trackBox, BorderColor, false);
    }
}
