using System.Linq;
using Content.Client.Humanoid;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Client.Utility;
using Robust.Shared.Map;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client._Sunrise.DynamicAppearance;

/// <summary>
/// Character preview: real entity (clothes ON) or client-side dummy (clothes OFF) + rotation.
/// </summary>
public sealed partial class DynamicAppearanceWindow
{
    // ═══════════ Handler wiring ═══════════

    private void InitPreviewHandlers()
    {
        RotateLeftButton.OnPressed += _ =>
        {
            _previewRotation = _previewRotation.TurnCw();
            ApplyPreviewRotation();
        };

        RotateRightButton.OnPressed += _ =>
        {
            _previewRotation = _previewRotation.TurnCcw();
            ApplyPreviewRotation();
        };

        ShowClothesButton.OnToggled += args =>
        {
            _showClothes = args.Pressed;
            RefreshPreview();
        };
    }

    // ═══════════ Preview refresh ═══════════

    /// <summary>
    /// Always show a client-side dummy for the preview (live-updating appearance).
    /// When clothes are ON the dummy is given copies of the real entity's equipped items.
    /// When clothes are OFF the dummy shows bare appearance only.
    /// </summary>
    private void RefreshPreview()
    {
        RebuildDummy();

        if (_entManager.EntityExists(_dummyEntity))
            SpriteView.SetEntity(_dummyEntity);

        ApplyPreviewRotation();
    }

    /// <summary>
    /// Lightweight refresh: re-apply the current draft appearance to the dummy.
    /// Called by handlers after every draft mutation (color changed, marking added, etc.).
    /// The clothing items on the dummy are preserved — only the humanoid appearance changes.
    /// </summary>
    private void RefreshDummyPreview()
    {
        if (!_entManager.EntityExists(_dummyEntity))
            return;

        ApplyDraftToDummy();
    }

    // ═══════════ Rotation ═══════════

    private void ApplyPreviewRotation()
    {
        SpriteView.OverrideDirection = (Direction)((int)_previewRotation % 4 * 2);
    }

    // ═══════════ Dummy lifecycle ═══════════

    /// <summary>
    /// Creates (or re-creates) the client-side dummy entity, applies the current draft appearance,
    /// and — when "show clothes" is enabled — copies equipped items from the real entity.
    /// </summary>
    private void RebuildDummy()
    {
        DestroyDummy();

        if (!_protoMan.TryIndex<SpeciesPrototype>(_draftState.Species, out var speciesProto))
            return;

        _dummyEntity = _entManager.SpawnEntity(speciesProto.DollPrototype, MapCoordinates.Nullspace);
        ApplyDraftToDummy();

        if (_showClothes && _entManager.EntityExists(_previewEntity))
            GiveDummyInventory(_dummyEntity, _previewEntity);
    }

    /// <summary>
    /// Copies the equipped items from <paramref name="source"/> onto <paramref name="dummy"/>.
    /// Items are spawned as new client-side entities so the real inventory is untouched.
    /// </summary>
    private void GiveDummyInventory(EntityUid dummy, EntityUid source)
    {
        if (!_inventorySystem.TryGetSlots(dummy, out var slots))
            return;

        foreach (var slot in slots)
        {
            if (!_inventorySystem.TryGetSlotEntity(source, slot.Name, out var slotItem))
                continue;

            var meta = _entManager.GetComponent<MetaDataComponent>(slotItem.Value);
            var proto = meta.EntityPrototype;
            if (proto == null)
                continue;

            var copy = _entManager.SpawnEntity(proto.ID, MapCoordinates.Nullspace);
            _inventorySystem.TryEquip(dummy, copy, slot.Name, silent: true, force: true);
        }
    }

    /// <summary>
    /// Applies the current <see cref="_draftState"/> to the dummy entity via a temporary
    /// <see cref="HumanoidCharacterProfile"/> and <see cref="HumanoidAppearanceSystem.LoadProfile"/>.
    /// </summary>
    private void ApplyDraftToDummy()
    {
        if (!_entManager.EntityExists(_dummyEntity))
            return;

        var profile = BuildProfileFromDraft();
        _humanoidSystem.LoadProfile(_dummyEntity, profile);
    }

    private void DestroyDummy()
    {
        if (_entManager.EntityExists(_dummyEntity))
            _entManager.DeleteEntity(_dummyEntity);

        _dummyEntity = EntityUid.Invalid;
    }

    // ═══════════ Profile construction ═══════════

    /// <summary>
    /// Builds a minimal <see cref="HumanoidCharacterProfile"/> suitable for
    /// <see cref="HumanoidAppearanceSystem.LoadProfile"/> from the current draft state.
    /// </summary>
    private HumanoidCharacterProfile BuildProfileFromDraft()
    {
        // Extract hair / facial hair from MarkingSet so we can populate the profile fields.
        var hairMarking = _draftState.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairList)
            ? hairList.FirstOrDefault()
            : null;

        var facialHairMarking = _draftState.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialList)
            ? facialList.FirstOrDefault()
            : null;

        var hairId = hairMarking?.MarkingId ?? HairStyles.DefaultHairStyle;
        var hairColor = hairMarking != null && hairMarking.MarkingColors.Count > 0
            ? hairMarking.MarkingColors[0]
            : Color.Black;

        var facialId = facialHairMarking?.MarkingId ?? HairStyles.DefaultFacialHairStyle;
        var facialColor = facialHairMarking != null && facialHairMarking.MarkingColors.Count > 0
            ? facialHairMarking.MarkingColors[0]
            : Color.Black;

        // Extract gradient / marking effects from hair markings.
        var hairEffectType = MarkingEffectType.Color;
        MarkingEffect? hairEffect = null;
        if (hairMarking != null && hairMarking.MarkingEffects.Count > 0)
        {
            hairEffect = hairMarking.MarkingEffects[0];
            hairEffectType = hairEffect.Type;
        }

        var facialEffectType = MarkingEffectType.Color;
        MarkingEffect? facialEffect = null;
        if (facialHairMarking != null && facialHairMarking.MarkingEffects.Count > 0)
        {
            facialEffect = facialHairMarking.MarkingEffects[0];
            facialEffectType = facialEffect.Type;
        }

        // Flatten all markings.
        var allMarkings = _draftState.MarkingSet.GetForwardEnumerator().ToList();

        var appearance = new HumanoidCharacterAppearance(
            hairId,
            hairColor,
            facialId,
            facialColor,
            _draftState.EyeColor,
            _draftState.SkinColor,
            allMarkings,
            hairEffectType,
            hairEffect,
            facialEffectType,
            facialEffect,
            _draftState.Width,
            _draftState.Height);

        // Use the correct body type for the species so non-human species (e.g. slime) render
        // with their proper sprites instead of falling back to the default human body type.
        var bodyType = _speciesProto?.BodyTypes.FirstOrDefault()
            ?? SharedHumanoidAppearanceSystem.DefaultBodyType;

        return HumanoidCharacterProfile
            .DefaultWithSpecies(_draftState.Species)
            .WithCharacterAppearance(appearance)
            .WithSex(_draftState.Sex)
            .WithGender(_draftState.Gender)
            .WithAge(_draftState.Age)
            .WithVoice(_draftState.Voice)
            .WithBodyType(bodyType);
    }
}
