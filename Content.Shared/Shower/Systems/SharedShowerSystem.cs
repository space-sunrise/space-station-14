using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Shower.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Shower.Systems;

/// <summary>
/// Handles shower toggling and visual updates.
/// </summary>
public abstract class SharedShowerSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowerComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleShowerVerb);
        SubscribeLocalEvent<ShowerComponent, ActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnToggleShowerVerb(Entity<ShowerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;
        AlternativeVerb toggleVerb = new() { Act = () => ToggleShower(ent, user) };

        if (ent.Comp.IsOn)
        {
            toggleVerb.Text = Loc.GetString("shower-turn-off");
            toggleVerb.Icon =
                new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
        }
        else
        {
            toggleVerb.Text = Loc.GetString("shower-turn-on");
            toggleVerb.Icon =
                new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
        }
        args.Verbs.Add(toggleVerb);
    }

    private void OnActivateInWorld(Entity<ShowerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        ToggleShower(ent, args.User);
    }

    protected void UpdateAppearance(Entity<ShowerComponent> ent)
    {
        Appearance.SetData(ent, ShowerVisuals.IsOn, ent.Comp.IsOn ? ShowerState.On : ShowerState.Off);
    }

    /// <summary>
    /// Toggles a shower on/off.
    /// </summary>
    /// <param name="ent">The shower being toggled.</param>
    /// <param name="user">The user doing the toggling; used for predicted audio.</param>
    public virtual void ToggleShower(Entity<ShowerComponent> ent, EntityUid? user = null)
    {
        ent.Comp.IsOn = !ent.Comp.IsOn;

        Audio.PlayPredicted(ent.Comp.ToggleSound, ent, user);
        UpdateAppearance(ent);
        Dirty(ent);
    }
}
