using Content.Client._Sunrise.MarkingEffectsClient;
using Content.Shared._Sunrise.MarkingEffects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Sunrise.UserInterface.Controls;

public sealed class MarkingEffectSelectorSliders : Control
{
    private MarkingEffect Effect { get; set; }

    private static readonly Dictionary<MarkingEffectType, IMarkingEffectUiBuilder> UiBuilders = new()
    {
        { MarkingEffectType.Color, new ColorMarkingEffectUiBuilder() },
        { MarkingEffectType.Gradient, new GradientMarkingEffectUiBuilder() },
    };

    private readonly Dictionary<string, ColorSelectorSliders> _colorSelectors = new();

    private readonly OptionButton _typeSelector;
    private readonly List<MarkingEffectType> _types = new();

    private MarkingEffectType _currentType;

    private readonly BoxContainer _selectorContainer;
    private readonly BoxContainer _sliderContainer;
    private readonly BoxContainer _toggleContainer;

    public Action<MarkingEffect>? OnColorChanged;

    public MarkingEffectType CurrentType
    {
        get => _currentType;
        set
        {
            if (_currentType == value)
                return;

            _currentType = value;
            Populate(_currentType);
        }
    }

    public MarkingEffectSelectorSliders(MarkingEffect? defaultEffect = null)
    {
        defaultEffect ??= ColorMarkingEffect.White;

        _typeSelector = new OptionButton();
        foreach (var type in Enum.GetValues<MarkingEffectType>())
        {
            // TODO: локализация
            _typeSelector.AddItem(type.ToString());
            _types.Add(type);
        }

        _typeSelector.OnItemSelected += args =>
        {
            CurrentType = _types[args.Id];
            _typeSelector.Select(args.Id);
        };

        var rootBox = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        AddChild(rootBox);

        var headerBox = new BoxContainer();
        rootBox.AddChild(headerBox);
        headerBox.AddChild(_typeSelector);

        var bodyBox = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        rootBox.AddChild(bodyBox);

        _selectorContainer = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        bodyBox.AddChild(_selectorContainer);

        _sliderContainer = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        bodyBox.AddChild(_sliderContainer);

        _toggleContainer = new BoxContainer();
        bodyBox.AddChild(_toggleContainer);


        _currentType = defaultEffect.Type;
        _typeSelector.TrySelect(_types.IndexOf(_currentType));
        _typeSelector.OnItemSelected += _ => OnColorsChanged();
        Effect = defaultEffect;
        Populate(_currentType, defaultEffect);
    }

    public ColorSelectorSliders CreateSelector(string key = "base")
    {
        var colorSelector = new ColorSelectorSliders();

        if (Effect.Colors.TryGetValue(key, out var defaultColor))
            colorSelector.Color = defaultColor;

        colorSelector.OnColorChanged += _ => OnColorsChanged();

        _colorSelectors.Add(key, colorSelector);
        _selectorContainer.AddChild(colorSelector);

        return colorSelector;
    }

    public void CreateSlider(string label,
        int defaultValue,
        int minValue,
        int maxValue,
        Action<float> onValueChanged)
    {
        var slider = new Slider
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };

        slider.Value = defaultValue;
        slider.MinValue = minValue;
        slider.MaxValue = maxValue;

        var sliderContainer = new BoxContainer();

        var sliderLabel = new Label();
        sliderLabel.Text = label;

        var spinBox = new SpinBox
        {
            IsValid = value => IsSpinBoxValid(value, minValue, maxValue)
        };
        spinBox.InitDefaultButtons();
        spinBox.ValueChanged += value =>
        {
            slider.SetValueWithoutEvent(value.Value);
            OnColorsChanged();
        };
        spinBox.Value = defaultValue;

        slider.OnValueChanged += value =>
        {
            OnColorsChanged();
            spinBox.Value = (int)(value.Value);
        };


        sliderContainer.AddChild(sliderLabel);
        sliderContainer.AddChild(slider);
        sliderContainer.AddChild(spinBox);
        _sliderContainer.AddChild(sliderContainer);

        BindSlider(slider, spinBox, onValueChanged);
    }

    private void BindSlider(Slider slider, SpinBox spinBox, Action<float> setValue)
    {
        slider.OnValueChanged += val =>
        {
            setValue(val.Value);
            spinBox.Value = (int)(val.Value);

            OnColorsChanged();
        };

        spinBox.ValueChanged += val =>
        {
            setValue(val.Value);
            slider.SetValueWithoutEvent(val.Value);
            OnColorsChanged();
        };
    }

    public void CreateToggle(string label, bool defaultValue, Action<bool> onValueChanged)
    {
        var button = new Button
        {
            Text = label,
            ToggleMode = true,
            Pressed = defaultValue,
        };

        button.OnToggled += _ => OnColorsChanged();

        _toggleContainer.AddChild(button);

        BindToggle(button, onValueChanged);
    }

    private void BindToggle(Button toggle, Action<bool> setValue)
    {
        toggle.OnToggled += val =>
        {
            setValue(val.Pressed);
            OnColorsChanged();
        };
    }

    private bool IsSpinBoxValid(int value, float min, float max)
    {
        if (value > max)
            return false;

        return !(value < min);
    }

    private void OnColorsChanged()
    {
        foreach (var (key, selector) in _colorSelectors)
        {
            Effect.Colors[key] = selector.Color;
        }

        OnColorChanged?.Invoke(Effect);
    }

    private void Populate(MarkingEffectType type, MarkingEffect? defaultEffect = null)
    {
        _colorSelectors.Clear();
        _selectorContainer.DisposeAllChildren();
        _sliderContainer.DisposeAllChildren();
        _toggleContainer.DisposeAllChildren();

        defaultEffect ??= type switch
        {
            MarkingEffectType.Color => ColorMarkingEffect.White,
            MarkingEffectType.Gradient => new GradientMarkingEffect(),
            _ => ColorMarkingEffect.White,
        };

        Effect = defaultEffect;

        if (UiBuilders.TryGetValue(type, out var builder))
            builder.BuildUI(Effect, this);
        else
            Logger.Warning($"No UI builder for marking effect: {type}");
    }
}

