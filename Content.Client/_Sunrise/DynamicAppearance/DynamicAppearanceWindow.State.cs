using System.Linq;
using Content.Shared._Sunrise;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;

namespace Content.Client._Sunrise.DynamicAppearance;

/// <summary>
/// Pushing server state into UI controls and refreshing individual sections.
/// </summary>
public sealed partial class DynamicAppearanceWindow
{
    /// <summary>
    /// Full state push: sets the draft, resolves the preview entity, and refreshes every control.
    /// Called by the BUI when the server sends a new <see cref="DynamicAppearanceBUIState"/>.
    /// </summary>
    public void UpdateState(DynamicAppearanceBUIState buiState)
    {
        _draftState = buiState.State;
        _baseAllowedFields = buiState.AllowedFields;
        _speciesProto = _protoMan.TryIndex<SpeciesPrototype>(_draftState.Species, out var sp) ? sp : null;

        // Resolve the entity for preview
        _previewEntity = _entManager.GetEntity(buiState.Entity);

        // Apply allowed-fields visibility before refreshing controls so users
        // only interact with sections they are permitted to change.
        RefreshAllowedFields();

        RefreshName();
        RefreshSpecies();
        RefreshAge();
        RefreshSex();
        RefreshBodyTypes();
        RefreshPronouns();
        RefreshVoice();
        RefreshSizeSliders();
        RefreshSkinColor();
        RefreshEyeColor();
        RefreshHairPickers();
        RefreshBodyMarkings();
        RefreshPreview();
    }

    public void UpdatePermissions(DynamicAppearancePermissionsMessage permissions)
    {
        _canOverrideRestrictions = permissions.CanOverrideRestrictions;
        _overrideRestrictions = permissions.OverrideRestrictions;

        RefreshAdminOverride();
        RefreshAllowedFields();
        RefreshSpecies();
        RefreshBodyTypes();
        RefreshVoice();
    }

    // ═══════════ Allowed-fields visibility ═══════════

    /// <summary>
    /// Shows or hides UI sections based on the server-provided <see cref="_allowedFields"/> whitelist.
    /// </summary>
    private void RefreshAllowedFields()
    {
        var allowedFields = GetEffectiveAllowedFields();
        var sexAllowed = allowedFields.HasFlag(DynamicAppearanceFields.Sex);
        var voiceAllowed = _ttsEnabled && allowedFields.HasFlag(DynamicAppearanceFields.Voice);

        NameSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Name);
        SpeciesSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Species);
        AgeSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Age);
        SexSection.Visible = sexAllowed || voiceAllowed;
        SexRow.Visible = sexAllowed;
        TTSContainer.Visible = voiceAllowed;
        BodyTypeSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.BodyType);
        PronounsSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Pronouns);
        SizeSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Size);
        SkinColorSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.SkinColor);
        EyeColorSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.EyeColor);
        HairSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Hair);
        MarkingsSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Markings);
    }

    // ═══════════ Individual refresh methods ═══════════

    private void RefreshName()
    {
        NameEdit.Text = _draftState.Name;
    }

    private void RefreshSpecies()
    {
        SpeciesButton.Clear();
        _speciesValues.Clear();

        var availableSpecies = _protoMan.EnumeratePrototypes<SpeciesPrototype>()
            .Where(species => _overrideRestrictions || species.RoundStart)
            .ToList();

        if (!string.IsNullOrEmpty(_draftState.Species)
            && _protoMan.TryIndex<SpeciesPrototype>(_draftState.Species, out var currentSpecies)
            && availableSpecies.All(species => species.ID != currentSpecies.ID))
        {
            availableSpecies.Add(currentSpecies);
        }

        availableSpecies.Sort((a, b) =>
            string.Compare(Loc.GetString(a.Name), Loc.GetString(b.Name), StringComparison.CurrentCultureIgnoreCase));

        var sponsorSpecies = _sponsorsMgr?.GetClientPrototypes().ToHashSet() ?? new HashSet<string>();

        for (var i = 0; i < availableSpecies.Count; i++)
        {
            var species = availableSpecies[i];
            var name = Loc.GetString(species.Name);

            SpeciesButton.AddItem(name, i);
            _speciesValues.Add(species);

            if (species.SponsorOnly
                && !_overrideRestrictions
                && !sponsorSpecies.Contains(species.ID))
            {
                SpeciesButton.SetItemDisabled(SpeciesButton.GetIdx(i), true);
                SpeciesButton.SetItemText(SpeciesButton.GetIdx(i), Loc.GetString("sponsor-marking", ("name", name)));
            }

            if (_draftState.Species == species.ID)
                SpeciesButton.SelectId(i);
        }
    }

    private void RefreshAge()
    {
        AgeEdit.Text = _draftState.Age.ToString();
    }

    private void RefreshSex()
    {
        RebuildSexButton();
    }

    private void RebuildSexButton()
    {
        _sexValues.Clear();
        SexButton.Clear();

        if (_speciesProto == null)
            return;

        foreach (var sex in _speciesProto.Sexes)
        {
            SexButton.AddItem(
                Loc.GetString($"humanoid-profile-editor-sex-{sex.ToString().ToLowerInvariant()}-text"),
                _sexValues.Count);
            _sexValues.Add(sex);
        }

        var idx = _sexValues.IndexOf(_draftState.Sex);
        if (idx >= 0)
        {
            SexButton.SelectId(idx);
        }
        else if (_sexValues.Count > 0)
        {
            SexButton.SelectId(0);
            _draftState.Sex = _sexValues[0];
        }
    }

    private void RefreshPronouns()
    {
        var genderIdx = _genderValues.IndexOf(_draftState.Gender);
        if (genderIdx >= 0)
            PronounsButton.SelectId(genderIdx);
    }

    private void RefreshBodyTypes()
    {
        _bodyTypeValues.Clear();
        BodyTypeButton.Clear();

        if (_speciesProto == null)
            return;

        _bodyTypeValues.AddRange(GetValidBodyTypes(_speciesProto, _draftState.Sex));
        var resolvedBodyType = ResolveValidBodyType(_speciesProto, _draftState.Sex, _draftState.BodyType);
        _draftState.BodyType = resolvedBodyType;

        for (var i = 0; i < _bodyTypeValues.Count; i++)
        {
            BodyTypeButton.AddItem(Loc.GetString(_bodyTypeValues[i].Name), i);
        }

        var index = _bodyTypeValues.FindIndex(proto => proto.ID == resolvedBodyType);
        if (index >= 0)
            BodyTypeButton.SelectId(index);
    }

    private void RefreshVoice()
    {
        if (!_ttsEnabled)
            return;

        if (!GetEffectiveAllowedFields().HasFlag(DynamicAppearanceFields.Voice))
        {
            _filteredVoices.Clear();
            VoiceButton.Clear();
            return;
        }

        RebuildVoiceList();
    }

    private void RebuildVoiceList()
    {
        // Collect sponsor-owned prototype IDs so we can show/hide sponsor-only voices.
        // Sunrise-Sponsors-Start
        var clientSponsorProtos = _sponsorsMgr?.GetClientPrototypes()?.ToHashSet()
                                  ?? new HashSet<string>();
        // Sunrise-Sponsors-End
        var ignoreRestrictions = _overrideRestrictions;

        var voiceEntries = _protoMan.EnumeratePrototypes<TTSVoicePrototype>()
            .Where(v => (ignoreRestrictions || v.RoundStart)
                        && HumanoidCharacterProfile.CanHaveVoice(v, _draftState.Sex)
                        && (ignoreRestrictions || !v.SponsorOnly || clientSponsorProtos.Contains(v.ID))) // Sunrise-Sponsors
            .Select(v => (Voice: v, DisplayName: Loc.GetString(v.Name)))
            .ToList();

        voiceEntries.Sort((a, b) =>
            string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase));

        _filteredVoices = voiceEntries.Select(v => v.Voice).ToList();

        VoiceButton.Clear();
        var selectIdx = 0;

        for (var i = 0; i < voiceEntries.Count; i++)
        {
            var voice = voiceEntries[i].Voice;
            VoiceButton.AddItem(voiceEntries[i].DisplayName, i);

            if (voice.ID == _draftState.Voice)
                selectIdx = i;
        }

        if (_filteredVoices.Count > 0)
        {
            VoiceButton.SelectId(selectIdx);
            _draftState.Voice = _filteredVoices[selectIdx].ID;
        }
    }

    private void RefreshSizeSliders()
    {
        if (_speciesProto != null)
        {
            HeightSlider.MinValue = _speciesProto.MinHeight;
            HeightSlider.MaxValue = _speciesProto.MaxHeight;
            WidthSlider.MinValue = _speciesProto.MinWidth;
            WidthSlider.MaxValue = _speciesProto.MaxWidth;
        }

        HeightSlider.Value = _draftState.Height;
        WidthSlider.Value = _draftState.Width;
        UpdateSizeLabels();
    }

    private void UpdateSizeLabels()
    {
        if (_speciesProto == null)
        {
            HeightLabel.Text = Loc.GetString("dynamic-appearance-height-label",
                ("value", (int)Math.Round(HeightSlider.Value * 100f)));
            WidthLabel.Text = Loc.GetString("dynamic-appearance-width-label",
                ("value", (int)Math.Round(WidthSlider.Value * 100f)));
            return;
        }

        var height = ConvertSliderToHeight(
            sliderValue: HeightSlider.Value,
            minSlider: _speciesProto.MinHeight,
            maxSlider: _speciesProto.MaxHeight,
            minHeightCm: _speciesProto.MinHeightCm,
            maxHeightCm: _speciesProto.MaxHeightCm);

        var weight = _speciesProto.StandardWeight
                     + _speciesProto.StandardDensity * (WidthSlider.Value * HeightSlider.Value - 1f);

        HeightLabel.Text = Loc.GetString("humanoid-profile-editor-height-label",
            ("height", Math.Round(height)));
        WidthLabel.Text = Loc.GetString("humanoid-profile-editor-width-label",
            ("weight", Math.Round(weight)));
    }

    private static float ConvertSliderToHeight(float sliderValue, float minSlider, float maxSlider, float minHeightCm, float maxHeightCm)
    {
        var denom = maxSlider - minSlider;
        if (MathF.Abs(denom) < 0.0001f)
            return minHeightCm;

        var normalized = (sliderValue - minSlider) / denom;
        return minHeightCm + normalized * (maxHeightCm - minHeightCm);
    }

    private void RefreshSkinColor()
    {
        if (_speciesProto == null)
        {
            _skinToneSlider.Visible = false;
            _skinColorSelector.Visible = true;
            _skinColorSelector.Color = _draftState.SkinColor;
            return;
        }

        var strategy = _protoMan.Index(_speciesProto.SkinColoration).Strategy;

        switch (strategy.InputType)
        {
            case SkinColorationStrategyInput.Unary:
                _skinToneSlider.Visible = true;
                _skinColorSelector.Visible = false;
                _skinToneSlider.Value = strategy.ToUnary(_draftState.SkinColor);
                break;

            default:
                _skinToneSlider.Visible = false;
                _skinColorSelector.Visible = true;
                _skinColorSelector.Color = _draftState.SkinColor;
                break;
        }
    }

    private void RefreshEyeColor()
    {
        EyeColorPicker.SetData(_draftState.EyeColor);
    }

    private void RefreshHairPickers()
    {
        var hairList = _draftState.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var h)
            ? h.ToList()
            : new List<Marking>();

        var facialList = _draftState.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var fh)
            ? fh.ToList()
            : new List<Marking>();

        HairPicker.UpdateData(hairList, _draftState.Species, 1);
        FacialHairPicker.UpdateData(facialList, _draftState.Species, 1);
    }

    private void RefreshBodyMarkings()
    {
        // Use the flat-list overload so MarkingPicker always reconstructs its internal set
        // from speciesPrototype.MarkingPoints, giving proper per-category limits.
        var markingsList = _draftState.MarkingSet.GetForwardEnumerator().ToList();
        Markings.SetData(
            markingsList,
            _draftState.Species,
            _draftState.Sex,
            _draftState.SkinColor,
            _draftState.EyeColor);
    }

    private void RefreshAdminOverride()
    {
        _updatingAdminOverrideButton = true;
        AdminOverrideButton.Visible = _canOverrideRestrictions;
        AdminOverrideButton.Pressed = _overrideRestrictions;
        _updatingAdminOverrideButton = false;
    }

    private DynamicAppearanceFields GetEffectiveAllowedFields()
    {
        return _overrideRestrictions ? DynamicAppearanceFields.All : _baseAllowedFields;
    }

    private List<BodyTypePrototype> GetValidBodyTypes(SpeciesPrototype speciesProto, Sex sex)
    {
        return speciesProto.BodyTypes
            .Select(id => _protoMan.Index<BodyTypePrototype>(id))
            .Where(proto => !proto.SexRestrictions.Contains(sex.ToString()))
            .ToList();
    }

    private string ResolveValidBodyType(SpeciesPrototype speciesProto, Sex sex, string? preferredBodyType)
    {
        var validBodyTypes = GetValidBodyTypes(speciesProto, sex);

        if (validBodyTypes.Count > 0)
        {
            if (!string.IsNullOrEmpty(preferredBodyType)
                && validBodyTypes.Any(proto => proto.ID == preferredBodyType))
            {
                return preferredBodyType;
            }

            return validBodyTypes[0].ID;
        }

        if (!string.IsNullOrEmpty(preferredBodyType)
            && speciesProto.BodyTypes.Contains(preferredBodyType))
        {
            return preferredBodyType;
        }

        return speciesProto.BodyTypes.FirstOrDefault() ?? SharedHumanoidAppearanceSystem.DefaultBodyType;
    }

    private void ApplySpeciesChange(string newSpecies)
    {
        if (!_protoMan.TryIndex<SpeciesPrototype>(newSpecies, out var speciesProto))
            return;

        _draftState.Species = newSpecies;
        _speciesProto = speciesProto;

        var strategy = _protoMan.Index(speciesProto.SkinColoration).Strategy;
        _draftState.SkinColor = strategy.EnsureVerified(_draftState.SkinColor);

        if (!speciesProto.Sexes.Contains(_draftState.Sex))
            _draftState.Sex = speciesProto.Sexes[0];

        _draftState.BodyType = ResolveValidBodyType(speciesProto, _draftState.Sex, _draftState.BodyType);

        _draftState.Age = Math.Clamp(_draftState.Age, speciesProto.MinAge, speciesProto.MaxAge);
        _draftState.Width = speciesProto.DefaultWidth;
        _draftState.Height = speciesProto.DefaultHeight;

        var markings = new MarkingSet(_draftState.MarkingSet);
        markings.EnsureSpecies(newSpecies, _draftState.SkinColor, _markingManager, _protoMan);

        var rebuiltMarkings = new MarkingSet(markings.GetForwardEnumerator().ToList(), speciesProto.MarkingPoints, _markingManager, _protoMan);
        rebuiltMarkings.EnsureSexes(_draftState.Sex, _markingManager);
        _draftState.MarkingSet = rebuiltMarkings;

        RefreshAge();
        RefreshSex();
        RefreshBodyTypes();
        RefreshVoice();
        RefreshSizeSliders();
        RefreshSkinColor();
        RefreshHairPickers();
        RefreshBodyMarkings();
        RefreshPreview();
    }
}
