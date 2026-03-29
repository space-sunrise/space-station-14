using Content.Shared.Actions;

namespace Content.Shared._Sunrise.Borgs.ModuleInnate;

/// <summary>
/// Ивент на активацию встроенного предмета с взаимодействием с целью
/// </summary>
public sealed partial class ModuleInnateInteractionItemEvent : EntityTargetActionEvent
{
    [DataField]
    public EntityUid Item;
}
