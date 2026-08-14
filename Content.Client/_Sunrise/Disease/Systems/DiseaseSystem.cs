// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Disease.Components;
using Robust.Client.Player;
using Content.Shared._Sunrise.Disease;

namespace Content.Client._Sunrise.Disease.Systems;

public sealed class DiseaseSystem : SharedDiseaseSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseComponent, GetStatusIconsEvent>(GetPatient);
        SubscribeLocalEvent<PrimaryPatientComponent, GetStatusIconsEvent>(GetPrimaryPatient);
    }

    private void GetPatient(Entity<DiseaseComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity == ent)
            return;

        if (HasComp<PrimaryPatientComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetPrimaryPatient(Entity<PrimaryPatientComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity == ent)
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

}
