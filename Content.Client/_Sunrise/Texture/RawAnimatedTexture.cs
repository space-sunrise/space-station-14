using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Texture;

public sealed class RawAnimatedTexture : Control
{
    private IRsiStateLike? _state;
    private int _curFrame;
    private float _curFrameTime;

    public TextureRect DisplayRect { get; }

    public RsiDirection RsiDirection { get; } = RsiDirection.South;

    public RawAnimatedTexture()
    {
        IoCManager.InjectDependencies(this);

        DisplayRect = new TextureRect();
        AddChild(DisplayRect);
    }

    public void SetFromSpriteSpecifier(SpriteSpecifier specifier)
    {
        _curFrame = 0;
        _state = specifier.RsiStateLike();
        _curFrameTime = _state.GetDelay(0);
        DisplayRect.Texture = _state.GetFrame(RsiDirection, 0);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!VisibleInTree || _state == null || !_state.IsAnimated)
            return;

        var oldFrame = _curFrame;

        _curFrameTime -= args.DeltaSeconds;
        while (_curFrameTime < _state.GetDelay(_curFrame))
        {
            _curFrame = (_curFrame + 1) % _state.AnimationFrameCount;
            _curFrameTime += _state.GetDelay(_curFrame);
        }

        if (_curFrame != oldFrame)
        {
            DisplayRect.Texture = _state.GetFrame(RsiDirection, _curFrame);
        }
    }
}
