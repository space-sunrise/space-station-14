using Content.Server.Popups;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Popups;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared._Sunrise.Weapons.Melee.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
namespace Content.Server._Sunrise.Weapons.Melee.Systems;

public sealed class BackstabOnHitSystem : SharedBackstabOnHitSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnBackstabBonusApplied(Entity<BackstabOnHitComponent> ent, EntityUid target)
    {
        if (ent.Comp.PopupMessages is not { } popupTable)
            return;

        if (!_prototype.TryIndex(popupTable, out WeightedRandomPrototype? popupWeights))
        {
            Log.Warning($"Missing weighted popup prototype {popupTable} on {ToPrettyString(ent.Owner)}.");
            return;
        }

        var popup = Loc.GetString(popupWeights.Pick(_random));
        _popup.PopupEntity(popup, target, PopupType.LargeCaution);
    }
}
