using System.Collections.Generic;
using Content.Server.EUI;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Eui;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Eui;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Tutorial;

public sealed class TutorialCompletionEui : BaseEui
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    private readonly EntityUid _player;
    private readonly TutorialSystem _system;

    public TutorialCompletionEui(EntityUid player)
    {
        IoCManager.InjectDependencies(this);
        _player = player;
        _system = _entitySystem.GetEntitySystem<TutorialSystem>();
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override void Closed()
    {
        _system.OnCompletionEuiClosed(Player);
    }

    public override EuiStateBase GetNewState()
    {
        var actions = new List<TutorialCompletionEuiAction>
        {
            new(TutorialCompletionActions.Leave, "tutorial-complete-leave", null, false),
            new(TutorialCompletionActions.Stay, "tutorial-complete-stay", null, false),
        };

        var comp = _ent.GetComponent<TutorialPlayerComponent>(_player);

        return new TutorialCompletionEuiState(
            "tutorial-complete-title",
            "tutorial-complete-desc",
            actions,
            comp.SequenceId);
    }
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not TutorialCompletionEuiActionMessage action)
            return;

        _system.HandleCompletionAction(_player, action.ActionId);
        Close();
    }
}
