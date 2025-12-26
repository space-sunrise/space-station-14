using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.BloodCult;

public sealed class ShowCultHudSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid,
        BloodCultistComponent bloodCultistComponent,
        ref GetStatusIconsEvent args)
    {
        var ent = _player.LocalSession?.AttachedEntity;
        if (!HasComp<BloodCultistComponent>(ent))
            return;

        if (_prototype.TryIndex(bloodCultistComponent.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
