using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._Sunrise.FancyCardControl;
using Content.Shared._Sunrise.Tutorial.Eui;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TutorialCompletionWindow : BaseWindow
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private FancyCard Card { get; }
    public event Action<string>? ActionPressed;

    public TutorialCompletionWindow()
    {
        IoCManager.InjectDependencies(this);
        Resizable = false;

        var rootContainer = new BoxContainer();
        Card = new FancyCard(new FancyCardConfig
        {
            TitleText = "-",
            DescText = "-",
            Buttons = [],
        });

        MouseFilter = MouseFilterMode.Stop;
        rootContainer.AddChild(Card);
        AddChild(rootContainer);
    }

    public void ApplyState(TutorialCompletionEuiState state)
    {
        var buttons = BuildButtons(state.Actions);

        var proto = _proto.Index<TutorialSequencePrototype>(state.ProtoId);
        var config = new FancyCardConfig
        {
            TitleText = Loc.GetString(state.Title),
            DescText = Loc.GetString(state.Description),
            Buttons = buttons,
            CardSize = new Vector2(580, 320),
            BackdropTexture = proto.Texture,
        };

        Card.ApplyConfig(config);
    }

    private List<FancyCardButton> BuildButtons(IEnumerable<TutorialCompletionEuiAction> actions)
    {
        var buttons = new List<FancyCardButton>();

        buttons.AddRange(actions.Select(action =>
        {
            var id = action.Id;
            return new FancyCardButton(
                id,
                Loc.GetString(action.Text),
                new Vector2(150, 30),
                !string.IsNullOrEmpty(action.Tooltip) ? Loc.GetString(action.Tooltip) : null,
                action.Disabled,
                () => ActionPressed?.Invoke(id));
        }));

        return buttons;
    }
    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        return DragMode.Move;
    }
}
