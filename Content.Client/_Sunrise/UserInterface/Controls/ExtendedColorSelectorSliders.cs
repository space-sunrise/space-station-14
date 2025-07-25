using Content.Shared._Sunrise.ExtendedColor;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Sunrise.UserInterface.Controls;

public sealed class ExtendedColorSelectorSliders : Control
{
    public ExtendedColor Color { get; set; }

    private readonly Dictionary<string, ColorSelectorSliders> _colorSelectors = new();
    private readonly List<Slider> _sliders = new();

    private readonly OptionButton _typeSelector;
    private readonly List<ColorType> _types = new();

    private ColorType _currentType;

    private readonly BoxContainer _bodyBox;

    private readonly BoxContainer _selectorContainer;
    private readonly BoxContainer _sliderContainer;
    private readonly BoxContainer _toggleContainer;

    public Action<ExtendedColor>? OnColorChanged;

    public ColorType CurrentType
    {
        get => _currentType;
        set
        {
            if (_currentType == value)
                return;

            _currentType = value;
            Populate();
        }
    }

    public ExtendedColorSelectorSliders(ExtendedColor? defaultColor = null)
    {
        defaultColor ??= ExtendedColor.White;

        _typeSelector = new OptionButton();
        foreach (var type in Enum.GetValues<ColorType>())
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

        _bodyBox = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        rootBox.AddChild(_bodyBox);

        _selectorContainer = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        _bodyBox.AddChild(_selectorContainer);

        _sliderContainer = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        _bodyBox.AddChild(_sliderContainer);

        _toggleContainer = new BoxContainer();
        _bodyBox.AddChild(_toggleContainer);

        Color = defaultColor;
        _currentType = defaultColor.Type;
        _typeSelector.TrySelect(_types.IndexOf(_currentType));
        _typeSelector.OnItemSelected += _ => OnColorsChanged();
        Populate();
    }

    private ColorSelectorSliders CreateSelector(string key = "base")
    {
        var colorSelector = new ColorSelectorSliders();

        if (Color.Colors.TryGetValue(key, out var defaultColor))
            colorSelector.Color = defaultColor;

        colorSelector.OnColorChanged += _ => OnColorsChanged();

        _colorSelectors.Add(key, colorSelector);
        _selectorContainer.AddChild(colorSelector);

        return colorSelector;
    }

    private void CreateSlider(string label,
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

        _sliders.Add(slider);

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

    private void CreateToggle(string label, bool defaultValue, Action<bool> onValueChanged)
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
            Color.Colors[key] = selector.Color;
        }

        Color.Type = CurrentType;

        OnColorChanged?.Invoke(Color);
    }

    private void Populate()
    {
        _colorSelectors.Clear();
        _selectorContainer.DisposeAllChildren();
        _sliderContainer.DisposeAllChildren();
        _toggleContainer.DisposeAllChildren();

        // Самый обычный тип, просто добавляем селектор
        if (CurrentType == ColorType.Color)
        {
            CreateSelector();
            return;
        }

        switch (CurrentType)
        {
            case ColorType.Gradient:
                CreateSelector();
                CreateSelector("gradient");

                // TODO: локализация слайдерам
                CreateSlider(
                    "offsetY",
                    (int)MathF.Round(Color.Offset.Y * 100),
                    -100,
                    100,
                    val => Color.Offset.Y = val / 100f
                );

                CreateSlider(
                    "sizeY",
                    (int)MathF.Round(Color.Size.Y * 100),
                    30,
                    500,
                    val => Color.Size.Y = val / 100f
                );

                CreateSlider(
                    "rotation",
                    (int)MathF.Round(Color.Rotation),
                    0,
                    360,
                    val => Color.Rotation = val
                );

                CreateToggle(
                    "pixelation",
                    Color.Pixelated,
                    toggle => Color.Pixelated = toggle
                );

                CreateToggle(
                    "mirror",
                    Color.Mirrored,
                    toggle => Color.Mirrored = toggle
                );

                break;
        }
    }
}

