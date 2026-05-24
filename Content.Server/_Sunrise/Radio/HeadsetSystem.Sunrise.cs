using Content.Shared.Radio.Components;
using Content.Shared._Sunrise.Radio;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.Radio.EntitySystems;

public sealed partial class HeadsetSystem
{
    private void OnToggleAction(Entity<HeadsetComponent> ent, ref ToggleHeadsetActionEvent args)
    {
        if (args.Handled || !ent.Comp.Enabled)
            return;

        if (TryComp<ActorComponent>(args.Performer, out var actor))
        {
            _ui.TryToggleUi(ent.Owner, HeadsetUiKey.Key, actor.PlayerSession);
            args.Handled = true;
        }
    }

    private void OnActivate(Entity<HeadsetComponent> ent, ref ActivateInWorldEvent args)
    {
        if (TryComp<ActorComponent>(args.User, out var actor))
        {
            _ui.OpenUi(ent.Owner, HeadsetUiKey.Key, actor.PlayerSession);
            args.Handled = true;
        }
    }
}
