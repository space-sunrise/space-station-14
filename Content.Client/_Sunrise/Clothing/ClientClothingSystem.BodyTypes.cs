using System.Diagnostics.CodeAnalysis;
using Content.Shared._Sunrise;
using Content.Shared._Sunrise.Humanoid;
using Content.Shared.Clothing.Components;
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Tag;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Client.Clothing;

public sealed partial class ClientClothingSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public void RefreshEquipmentVisuals(Entity<InventoryComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        UpdateAllSlots(ent.Owner, ent.Comp);
    }

    private void GetSunriseBodyTypeVisuals(
        EntityUid equipee,
        ClothingComponent clothing,
        string slot,
        ref List<PrototypeLayerData>? layers)
    {
        if (!TryGetSunriseBodyTypeVisualKey(equipee, out var bodyTypeVisualKey))
            return;

        clothing.ClothingVisuals.TryGetValue($"{slot}-{bodyTypeVisualKey}", out layers);
    }

    private string GetSunriseBodyTypeState(EntityUid equipee, RSI rsi, string state)
    {
        if (!TryGetSunriseBodyTypeVisualKey(equipee, out var bodyTypeVisualKey))
            return state;

        var bodyTypeState = $"{state}-{bodyTypeVisualKey}";
        return rsi.TryGetState(bodyTypeState, out _) ? bodyTypeState : state;
    }

    private string? GetSunriseBodyTypeVisualKey(EntityUid equipee)
    {
        return TryGetSunriseBodyTypeVisualKey(equipee, out var bodyTypeVisualKey)
            ? bodyTypeVisualKey
            : null;
    }

    private bool TryGetSunriseBodyTypeVisualKey(EntityUid equipee, [NotNullWhen(true)] out string? bodyTypeVisualKey)
    {
        bodyTypeVisualKey = null;
        if (!TryComp(equipee, out SunriseHumanoidProfileComponent? profile) ||
            !_prototype.TryIndex(profile.BodyType, out var bodyType))
        {
            return false;
        }

        bodyTypeVisualKey = bodyType.VisualKey;
        return true;
    }

    private DisplacementData? GetSunriseBodyTypeDisplacement(
        EntityUid equipee,
        EntityUid equipment,
        string slot,
        InventoryComponent inventory,
        string? bodyTypeVisualKey,
        DisplacementData? fallback)
    {
        if (bodyTypeVisualKey is null ||
            !TryComp(equipee, out HumanoidProfileComponent? humanoid))
        {
            return fallback;
        }

        var sexDisplacements = humanoid.Sex switch
        {
            Sex.Male => inventory.MaleDisplacements,
            Sex.Female => inventory.FemaleDisplacements,
            _ => null,
        };

        if (sexDisplacements is null || sexDisplacements.Count == 0)
            return fallback;

        var displacement = sexDisplacements.GetValueOrDefault($"{slot}-{bodyTypeVisualKey}")
                           ?? sexDisplacements.GetValueOrDefault(slot);

        if (!_tag.HasTag(equipment, "Hardsuit"))
            return displacement;

        return sexDisplacements.GetValueOrDefault($"hardsuit-{bodyTypeVisualKey}")
               ?? sexDisplacements.GetValueOrDefault(slot)
               ?? fallback;
    }
}
