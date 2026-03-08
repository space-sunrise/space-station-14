using System.IO;
using System.Linq;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Sunrise.DynamicAppearance;

/// <summary>
/// Profile import/export support for the in-round appearance editor.
/// Import only applies fields that this editor is currently allowed to change,
/// and re-validates imported appearance locally before it reaches the server.
/// </summary>
public sealed partial class DynamicAppearanceWindow
{
    private void InitImportExportHandlers()
    {
        ImportButton.OnPressed += _ => ImportProfile();
        ExportButton.OnPressed += _ => ExportProfile();
    }

    private async void ImportProfile()
    {
        if (_importExportInProgress || _playerManager.LocalSession == null)
            return;

        StartImportExport();
        await using var file = await _dialogManager.OpenFile(new FileDialogFilters(new FileDialogFilters.Group("yml")), FileAccess.Read);

        if (file == null)
        {
            EndImportExport();
            return;
        }

        try
        {
            var profile = _humanoidSystem.FromStream(file, _playerManager.LocalSession);
            ApplyImportedProfile(profile);
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error when importing dynamic appearance profile\n{exc}");
        }
        finally
        {
            EndImportExport();
        }
    }

    private async void ExportProfile()
    {
        if (_importExportInProgress)
            return;

        StartImportExport();
        var file = await _dialogManager.SaveFile(new FileDialogFilters(new FileDialogFilters.Group("yml")));

        if (file == null)
        {
            EndImportExport();
            return;
        }

        try
        {
            var profile = BuildProfileFromDraft().WithName(_draftState.Name);
            var dataNode = _humanoidSystem.ToDataNode(profile);
            await using var writer = new StreamWriter(file.Value.fileStream, leaveOpen: false);
            dataNode.Write(writer);
        }
        catch (Exception exc)
        {
            _sawmill.Error($"Error when exporting dynamic appearance profile\n{exc}");
        }
        finally
        {
            EndImportExport();
        }
    }

    private void StartImportExport()
    {
        _importExportInProgress = true;
        ImportButton.Disabled = true;
        ExportButton.Disabled = true;
    }

    private void EndImportExport()
    {
        _importExportInProgress = false;
        ImportButton.Disabled = false;
        ExportButton.Disabled = false;
    }

    private void ApplyImportedProfile(HumanoidCharacterProfile profile)
    {
        var updated = CloneState(_draftState);
        var allowed = GetEffectiveAllowedFields();

        var targetSpecies = updated.Species;
        if (allowed.HasFlag(DynamicAppearanceFields.Species)
            && IsSpeciesLocallyAllowed(profile.Species))
        {
            targetSpecies = profile.Species;
        }

        if (!_protoMan.TryIndex<SpeciesPrototype>(targetSpecies, out var speciesProto))
            return;

        var targetSex = updated.Sex;
        if (allowed.HasFlag(DynamicAppearanceFields.Sex)
            && speciesProto.Sexes.Contains(profile.Sex))
        {
            targetSex = profile.Sex;
        }

        if (!speciesProto.Sexes.Contains(targetSex))
            targetSex = speciesProto.Sexes[0];

        updated.Species = targetSpecies;
        updated.Sex = targetSex;

        var targetSkinColor = updated.SkinColor;
        var strategy = _protoMan.Index(speciesProto.SkinColoration).Strategy;
        if (allowed.HasFlag(DynamicAppearanceFields.SkinColor))
            targetSkinColor = strategy.EnsureVerified(profile.Appearance.SkinColor);
        else
            targetSkinColor = strategy.EnsureVerified(targetSkinColor);

        updated.SkinColor = targetSkinColor;

        if (allowed.HasFlag(DynamicAppearanceFields.EyeColor))
            updated.EyeColor = profile.Appearance.EyeColor;

        if (allowed.HasFlag(DynamicAppearanceFields.Name) && !string.IsNullOrWhiteSpace(profile.Name))
            updated.Name = profile.Name;

        if (allowed.HasFlag(DynamicAppearanceFields.Pronouns))
            updated.Gender = profile.Gender;

        if (allowed.HasFlag(DynamicAppearanceFields.Age))
            updated.Age = Math.Clamp(profile.Age, speciesProto.MinAge, speciesProto.MaxAge);
        else
            updated.Age = Math.Clamp(updated.Age, speciesProto.MinAge, speciesProto.MaxAge);

        if (allowed.HasFlag(DynamicAppearanceFields.Size))
        {
            updated.Width = Math.Clamp(profile.Appearance.Width, speciesProto.MinWidth, speciesProto.MaxWidth);
            updated.Height = Math.Clamp(profile.Appearance.Height, speciesProto.MinHeight, speciesProto.MaxHeight);
        }
        else
        {
            updated.Width = Math.Clamp(updated.Width, speciesProto.MinWidth, speciesProto.MaxWidth);
            updated.Height = Math.Clamp(updated.Height, speciesProto.MinHeight, speciesProto.MaxHeight);
        }

        if (_ttsEnabled && allowed.HasFlag(DynamicAppearanceFields.Voice))
        {
            if (IsVoiceLocallyAllowed(profile.Voice, targetSex))
                updated.Voice = profile.Voice;
        }

        var importedMarkings = BuildMarkingSetFromProfile(profile, targetSkinColor);
        updated.MarkingSet = MergeImportedMarkings(updated.MarkingSet, importedMarkings, targetSpecies, targetSex, updated.SkinColor, updated.EyeColor, allowed);

        ApplyDraftState(updated);
    }

    private void ApplyDraftState(DynamicAppearanceState state)
    {
        _draftState = state;
        _speciesProto = _protoMan.TryIndex<SpeciesPrototype>(_draftState.Species, out var speciesProto)
            ? speciesProto
            : null;

        RefreshAllowedFields();
        RefreshName();
        RefreshSpecies();
        RefreshAge();
        RefreshSex();
        RefreshPronouns();
        RefreshVoice();
        RefreshSizeSliders();
        RefreshSkinColor();
        RefreshEyeColor();
        RefreshHairPickers();
        RefreshBodyMarkings();
        RefreshPreview();
    }

    private DynamicAppearanceState CloneState(DynamicAppearanceState state)
    {
        return new DynamicAppearanceState(
            new MarkingSet(state.MarkingSet),
            state.Species,
            state.Sex,
            state.Age,
            state.Gender,
            state.Voice,
            state.SkinColor,
            state.EyeColor,
            new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(state.CustomBaseLayers),
            state.BodyType,
            state.Width,
            state.Height,
            state.Name);
    }

    private bool IsSpeciesLocallyAllowed(string speciesId)
    {
        if (!_protoMan.TryIndex<SpeciesPrototype>(speciesId, out var speciesProto))
            return false;

        if (_overrideRestrictions)
            return true;

        if (!speciesProto.RoundStart)
            return false;

        var sponsorSpecies = _sponsorsMgr?.GetClientPrototypes().ToHashSet() ?? new HashSet<string>();
        return !speciesProto.SponsorOnly || sponsorSpecies.Contains(speciesId);
    }

    private bool IsVoiceLocallyAllowed(string voiceId, Sex sex)
    {
        if (!_protoMan.TryIndex<TTSVoicePrototype>(voiceId, out var voiceProto))
            return false;

        if (!HumanoidCharacterProfile.CanHaveVoice(voiceProto, sex))
            return false;

        if (_overrideRestrictions)
            return true;

        if (!voiceProto.RoundStart)
            return false;

        var sponsorVoices = _sponsorsMgr?.GetClientPrototypes().ToHashSet() ?? new HashSet<string>();
        return !voiceProto.SponsorOnly || sponsorVoices.Contains(voiceId);
    }

    private MarkingSet BuildMarkingSetFromProfile(HumanoidCharacterProfile profile, Color skinColor)
    {
        var importedMarkings = new List<Marking>();

        foreach (var marking in profile.Appearance.Markings)
        {
            if (!_protoMan.TryIndex<MarkingPrototype>(marking.MarkingId, out var markingProto)
                || markingProto.MarkingCategory is MarkingCategories.Hair or MarkingCategories.FacialHair)
            {
                continue;
            }

            importedMarkings.Add(new Marking(marking));
        }

        TryAddLegacyProfileMarking(
            importedMarkings,
            profile.Species,
            profile.Sex,
            profile.Appearance.HairStyleId,
            profile.Appearance.HairColor,
            skinColor,
            HumanoidVisualLayers.Hair,
            profile.Appearance.HairMarkingEffect);

        TryAddLegacyProfileMarking(
            importedMarkings,
            profile.Species,
            profile.Sex,
            profile.Appearance.FacialHairStyleId,
            profile.Appearance.FacialHairColor,
            skinColor,
            HumanoidVisualLayers.FacialHair,
            profile.Appearance.FacialHairMarkingEffect);

        if (!_protoMan.TryIndex<SpeciesPrototype>(profile.Species, out var speciesProto))
        {
            _sawmill.Warning($"Imported appearance profile referenced unknown species '{profile.Species}'. Falling back to generic marking validation.");
            var fallback = new MarkingSet(importedMarkings);
            fallback.EnsureValid(_markingManager);
            return fallback;
        }

        var set = new MarkingSet(importedMarkings, speciesProto.MarkingPoints, _markingManager, _protoMan);
        set.EnsureValid(_markingManager);
        set.EnsureSpecies(profile.Species, skinColor, _markingManager, _protoMan);
        set.EnsureSexes(profile.Sex, _markingManager);
        return set;
    }

    private void TryAddLegacyProfileMarking(
        ICollection<Marking> markings,
        string species,
        Sex sex,
        string markingId,
        Color color,
        Color skinColor,
        HumanoidVisualLayers layer,
        MarkingEffect? effect)
    {
        if (!_markingManager.Markings.TryGetValue(markingId, out var markingProto)
            || !_markingManager.CanBeApplied(species, sex, markingProto, _protoMan))
        {
            return;
        }

        var resolvedColor = _markingManager.MustMatchSkin(species, layer, out var alpha, _protoMan)
            ? skinColor.WithAlpha(alpha)
            : color;

        var colors = Enumerable.Repeat(resolvedColor, markingProto.Sprites.Count).ToList();
        var effects = effect == null
            ? null
            : Enumerable.Range(0, markingProto.Sprites.Count).Select(_ => effect.Clone()).ToList();

        markings.Add(new Marking(markingId, colors, effects));
    }

    private MarkingSet MergeImportedMarkings(
        MarkingSet currentMarkings,
        MarkingSet importedMarkings,
        string species,
        Sex sex,
        Color skinColor,
        Color eyeColor,
        DynamicAppearanceFields allowed)
    {
        var merged = new List<Marking>();

        foreach (var (category, markings) in currentMarkings.Markings)
        {
            var isHairCategory = category is MarkingCategories.Hair or MarkingCategories.FacialHair;
            if (isHairCategory && allowed.HasFlag(DynamicAppearanceFields.Hair))
                continue;
            if (!isHairCategory && allowed.HasFlag(DynamicAppearanceFields.Markings))
                continue;

            merged.AddRange(markings.Select(marking => new Marking(marking)));
        }

        var sponsorProtos = _sponsorsMgr?.GetClientPrototypes().ToHashSet() ?? new HashSet<string>();

        foreach (var (category, markings) in importedMarkings.Markings)
        {
            var isHairCategory = category is MarkingCategories.Hair or MarkingCategories.FacialHair;
            if (isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Hair))
                continue;
            if (!isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Markings))
                continue;

            foreach (var marking in markings)
            {
                if (!IsMarkingLocallyAllowed(marking, species, sex, sponsorProtos))
                    continue;

                merged.Add(new Marking(marking));
            }
        }

        if (!_protoMan.TryIndex<SpeciesPrototype>(species, out var speciesProto))
            return new MarkingSet(merged);

        var set = new MarkingSet(merged, speciesProto.MarkingPoints, _markingManager, _protoMan);
        set.EnsureValid(_markingManager);
        set.EnsureSpecies(species, skinColor, _markingManager, _protoMan);
        set.EnsureSexes(sex, _markingManager);
        set.EnsureDefault(skinColor, eyeColor, _markingManager);
        return set;
    }

    private bool IsMarkingLocallyAllowed(Marking marking, string species, Sex sex, HashSet<string> sponsorProtos)
    {
        if (!_protoMan.TryIndex<MarkingPrototype>(marking.MarkingId, out var proto))
            return false;

        if (!_markingManager.CanBeApplied(species, sex, proto, _protoMan))
            return false;

        if (_overrideRestrictions)
            return true;

        return !proto.SponsorOnly || sponsorProtos.Contains(marking.MarkingId);
    }
}
