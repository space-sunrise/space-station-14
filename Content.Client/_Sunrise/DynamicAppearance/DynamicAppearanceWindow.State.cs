using System.Linq;
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
    }

    // ═══════════ Allowed-fields visibility ═══════════

    /// <summary>
    /// Shows or hides UI sections based on the server-provided <see cref="_allowedFields"/> whitelist.
    /// </summary>
    private void RefreshAllowedFields()
    {
        var allowedFields = GetEffectiveAllowedFields();

        NameSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Name);
        SpeciesSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Species);
        AgeSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Age);
        SexSection.Visible = allowedFields.HasFlag(DynamicAppearanceFields.Sex);
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
            .Where(species => species.RoundStart)
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

    private void RefreshVoice()
    {
        if (_ttsEnabled)
            RebuildVoiceList();
    }

    private void RebuildVoiceList()
    {
        // Collect sponsor-owned prototype IDs so we can show/hide sponsor-only voices.
        // Sunrise-Sponsors-Start
        var clientSponsorProtos = _sponsorsMgr?.GetClientPrototypes()?.ToHashSet()
                                  ?? new HashSet<string>();
        // Sunrise-Sponsors-End

        _filteredVoices = _protoMan.EnumeratePrototypes<TTSVoicePrototype>()
            .Where(v => v.RoundStart
                        && HumanoidCharacterProfile.CanHaveVoice(v, _draftState.Sex)
                        && (!v.SponsorOnly || clientSponsorProtos.Contains(v.ID))) // Sunrise-Sponsors
            .OrderBy(v => v.Name)
            .ToList();

        VoiceButton.Clear();
        var selectIdx = 0;

        for (var i = 0; i < _filteredVoices.Count; i++)
        {
            var voice = _filteredVoices[i];
            VoiceButton.AddItem(voice.Name, i);

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
        HeightLabel.Text = Loc.GetString("dynamic-appearance-height-label",
            ("value", (int)Math.Round(HeightSlider.Value * 100f)));
        WidthLabel.Text = Loc.GetString("dynamic-appearance-width-label",
            ("value", (int)Math.Round(WidthSlider.Value * 100f)));
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
        RefreshVoice();
        RefreshSizeSliders();
        RefreshSkinColor();
        RefreshHairPickers();
        RefreshBodyMarkings();
        RefreshPreview();
    }
}
