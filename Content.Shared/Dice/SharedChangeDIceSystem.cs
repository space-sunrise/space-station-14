using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Dice;

public sealed class ChangeDiceVerbSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiceComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<DiceComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var (uid, comp) = ent;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !ent.Comp.IsNotStandardDice)
            return;

        var @event = args;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("comp-change-dice-sides-number"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),

            Act = () =>
            {
                _ui.OpenUi(uid, ChangeDiceUiKey.Key, @event.User);
            },
            Priority = 1
        });
    }
}
