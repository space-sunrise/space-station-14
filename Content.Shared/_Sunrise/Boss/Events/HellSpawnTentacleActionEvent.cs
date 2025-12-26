using Content.Shared.Actions;

namespace Content.Shared._Sunrise.Boss.Events;

public sealed partial class HellSpawnTentacleActionEvent : WorldTargetActionEvent
{
    /// <summary>
    /// Правой или левой рукой был сделан граб
    /// </summary>
    [DataField]
    public bool Left;

    public HellSpawnTentacleActionEvent(bool left) : this()
    {
        Left = left;
    }
}
