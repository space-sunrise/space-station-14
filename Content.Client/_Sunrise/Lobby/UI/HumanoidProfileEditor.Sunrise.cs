using Content.Shared._Sunrise;
using Content.Shared._Sunrise.Humanoid;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Humanoid.Prototypes;
using Content.Sunrise.Interfaces.Shared;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private ISharedSponsorsManager? _sponsorsMgr;
    private readonly List<BodyTypePrototype> _bodyTypes = new();
    private bool _updatingSunriseControls;

    private void InitializeSunriseProfileEditor()
    {
        IoCManager.Instance!.TryResolveType(out _sponsorsMgr);

        if (_cfgManager.GetCVar(SunriseCCVars.TTSEnabled))
        {
            TTSContainer.Visible = true;
            InitializeVoice();
        }

        CBodyTypesButton.OnItemSelected += args =>
        {
            CBodyTypesButton.SelectId(args.Id);
            SetBodyType(_bodyTypes[args.Id].ID);
        };

        HeightSlider.OnValueChanged += _ =>
        {
            if (_updatingSunriseControls)
                return;

            SetCharacterHeight(HeightSlider.Value);
        };

        WidthSlider.OnValueChanged += _ =>
        {
            if (_updatingSunriseControls)
                return;

            SetCharacterWidth(WidthSlider.Value);
        };

        HeightResetButton.OnPressed += _ => ResetHeight();
        WidthResetButton.OnPressed += _ => ResetWidth();

        // Sunrise added — вкладка досье персонажа
        InitializeRecordsTab();
    }

    private void UpdateSunriseControls()
    {
        RefreshBodyTypes();
        UpdateSizeControls();
        UpdateTtsVoicesControls();
        // Sunrise added — обновляем вкладку досье
        UpdateRecordsTab();
    }

    private void RefreshBodyTypes()
    {
        CBodyTypesButton.Clear();
        _bodyTypes.Clear();

        if (Profile is null ||
            !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var species))
        {
            return;
        }

        foreach (var bodyTypeId in species.BodyTypes)
        {
            if (!_prototypeManager.TryIndex<BodyTypePrototype>(bodyTypeId, out var bodyType))
                continue;

            if (bodyType.SexRestrictions.Contains(Profile.Sex))
            {
                continue;
            }

            var index = _bodyTypes.Count;
            _bodyTypes.Add(bodyType);
            CBodyTypesButton.AddItem(Loc.GetString(bodyType.Name), index);

            if (Profile.BodyType == bodyType.ID)
                CBodyTypesButton.SelectId(index);
        }

        if (_bodyTypes.Count == 0)
            return;

        if (!CBodyTypesButton.TrySelectId(_bodyTypes.FindIndex(type => type.ID == Profile.BodyType)))
            SetBodyType(_bodyTypes[0].ID);
    }

    private void UpdateSizeControls()
    {
        if (Profile is null ||
            !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var species))
        {
            return;
        }

        _updatingSunriseControls = true;

        var width = Math.Clamp(Profile.Width, species.MinWidth, species.MaxWidth);
        var height = Math.Clamp(Profile.Height, species.MinHeight, species.MaxHeight);
        Profile = Profile.WithSize(width, height);

        HeightSlider.MinValue = species.MinHeight;
        HeightSlider.MaxValue = species.MaxHeight;
        HeightSlider.Value = height;

        WidthSlider.MinValue = species.MinWidth;
        WidthSlider.MaxValue = species.MaxWidth;
        WidthSlider.Value = width;

        HeightDescribeLabel.Text = Loc.GetString(
            "humanoid-profile-editor-height-label",
            ("height", HumanoidBodyMetrics.GetHeightCm(species, height)));

        WidthDescribeLabel.Text = Loc.GetString(
            "humanoid-profile-editor-width-label",
            ("weight", HumanoidBodyMetrics.GetWeightKg(species, width, height)));

        _updatingSunriseControls = false;
    }

    private void SetVoice(string voice)
    {
        Profile = Profile?.WithVoice(voice);
        SetDirty();
    }

    private void SetBodyType(string bodyType)
    {
        Profile = Profile?.WithBodyType(bodyType);
        ReloadPreview();
    }

    private void SetCharacterHeight(float height)
    {
        Profile = Profile?.WithHeight(height);
        UpdateSizeControls();
        ReloadProfilePreview();
    }

    private void SetCharacterWidth(float width)
    {
        Profile = Profile?.WithWidth(width);
        UpdateSizeControls();
        ReloadProfilePreview();
    }

    private void ResetHeight()
    {
        if (Profile is null ||
            !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var species))
        {
            return;
        }

        SetCharacterHeight(species.DefaultHeight);
    }

    private void ResetWidth()
    {
        if (Profile is null ||
            !_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var species))
        {
            return;
        }

        SetCharacterWidth(species.DefaultWidth);
    }
}
