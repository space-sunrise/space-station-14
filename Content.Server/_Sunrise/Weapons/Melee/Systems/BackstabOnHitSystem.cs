using Content.Server.Popups;
using Content.Shared._Sunrise.Weapons.Melee.Components;
using Content.Shared._Sunrise.Weapons.Melee.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
namespace Content.Server._Sunrise.Weapons.Melee.Systems;

public sealed class BackstabOnHitSystem : SharedBackstabOnHitSystem
{
    private const bool RecordBackstabPopupInReplay = true;

    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void OnBackstabBonusApplied(Entity<BackstabOnHitComponent> ent, EntityUid target)
    {
        if (ent.Comp.PopupMessages.Count == 0)
            return;

        _ = target;
        var popup = Loc.GetString(PickPopup(ent.Comp));
        _popup.PopupCursor(popup, Filter.Broadcast(), RecordBackstabPopupInReplay, PopupType.LargeCaution);
    }

    private LocId PickPopup(BackstabOnHitComponent component)
    {
        if (component.PopupWeights.Count != component.PopupMessages.Count || component.PopupWeights.Count == 0)
            return _random.Pick(component.PopupMessages);

        var totalWeight = 0f;
        foreach (var weight in component.PopupWeights)
        {
            if (weight > 0f)
                totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return _random.Pick(component.PopupMessages);

        var roll = _random.NextFloat() * totalWeight;
        for (var i = 0; i < component.PopupMessages.Count; i++)
        {
            var weight = component.PopupWeights[i];
            if (weight <= 0f)
                continue;

            if (roll < weight)
                return component.PopupMessages[i];

            roll -= weight;
        }

        return component.PopupMessages[^1];
    }
}
