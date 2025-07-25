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
        _bodyBox.AddChild(colorSelector);

        return colorSelector;
    }

    private Slider CreateSlider(string label, float defaultValue, float minValue, float maxValue)
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
        slider.OnValueChanged += _ => OnColorsChanged();

        var sliderContainer = new BoxContainer();

        var sliderLabel = new Label();
        sliderLabel.Text = label;

        sliderContainer.AddChild(sliderLabel);
        sliderContainer.AddChild(slider);
        _bodyBox.AddChild(sliderContainer);

        return slider;
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
        _bodyBox.DisposeAllChildren();
        switch (CurrentType)
        {
            case ColorType.Color:
                CreateSelector();
                break;
            case ColorType.Gradient:
                CreateSelector();
                CreateSelector("gradient");

                // TODO: локализация слайдерам
                var offsetYSlider = CreateSlider("offsetY", 0, -1, 1f);
                offsetYSlider.OnValueChanged += val => Color.Offset.Y = val.Value;

                var scaleYSlider = CreateSlider("scaleY", 1, 0.2f, 3f);
                scaleYSlider.OnValueChanged += val => Color.Size.Y = val.Value;

                var rotationSlider = CreateSlider("rotation", 0, 0f, 360f);
                rotationSlider.OnValueChanged += val => Color.Rotation = val.Value;

                break;
        }
    }
}

